using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using Serilog.Events;
using Shared;
using Shared.Observability;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ===== STRUCTURED LOGGING WITH SHARED CONFIGURATION =====
// Configure service name for observability
builder.Configuration["ServiceName"] = "ApiGateway";

// Configure Serilog using ObservabilityExtensions
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "ApiGateway",
    environment: builder.Environment.EnvironmentName,
    configuration: builder.Configuration,
    minimumLevel: LogEventLevel.Information
);

builder.Host.UseSerilog();

// ===== REVERSE PROXY CONFIGURATION =====
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ===== OPENTELEMETRY CONFIGURATION WITH SHARED EXTENSIONS =====
builder.Services.AddObservability(builder.Configuration, environment: builder.Environment.EnvironmentName);

// ===== PROMETHEUS METRICS ENDPOINT =====
builder.Services.AddMetrics();

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("https://localhost:5001/health"), "payment-processing", HealthStatus.Unhealthy)
    .AddUrlGroup(new Uri("https://localhost:5003/health"), "risk-assessment", HealthStatus.Unhealthy)
    .AddUrlGroup(new Uri("https://localhost:5002/health"), "payment-router", HealthStatus.Unhealthy)
    .AddUrlGroup(new Uri("https://localhost:5004/health"), "bank-adapter", HealthStatus.Unhealthy);

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Gateway",
        Version = "v1",
        Description = "API Gateway for Distributed Payment System with Production-Grade Observability"
    });
});

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseHttpsRedirection();

// ===== PROMETHEUS METRICS ENDPOINT =====
app.UsePrometheusMetrics();

// ===== REVERSE PROXY =====
app.MapReverseProxy();

// ===== HEALTH CHECK ENDPOINT =====
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Services = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration
            }),
            Timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsJsonAsync(response);
    }
})
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi();

// ===== ROOT ENDPOINT =====
app.MapGet("/", () => new
{
    Service = "API Gateway",
    Version = "1.0.0",
    Status = "Running",
    Observability = new
    {
        DistributedTracing = "OpenTelemetry OTLP with safe span attributes",
        Metrics = "Prometheus endpoint: /metrics (bounded labels only)",
        Logs = "Structured JSON with consistent field names and trace context"
    },
    Routes = new
    {
        PaymentProcessing = "/api/payments/*",
        RiskAssessment = "/api/risk/*",
        PaymentRouter = "/api/router/*",
        BankAdapter = "/api/bank/*"
    },
    Endpoints = new
    {
        Health = "/health",
        Metrics = "/metrics",
        Swagger = "/swagger"
    }
})
.WithName("Root")
.WithTags("Info")
.WithOpenApi();


try
{
    Log.Information("API Gateway starting with production-grade observability");
    Log.Information("Structured logging: Enabled with trace_id correlation");
    Log.Information("Prometheus metrics: /metrics (proper label cardinality)");
    Log.Information("Safe span attributes: No sensitive data in traces");
    Log.Information("Consistent field names: All services use same log structure");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
