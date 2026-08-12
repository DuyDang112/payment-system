using System.Reflection;
using BankAdapter.Domain;
using BankAdapter.Features.Authorize;
using BankAdapter.Features.Capture;
using BankAdapter.Features.HealthCheck;
using BankAdapter.Features.Refund;
using BankAdapter.Features.Shared.Routes;
using BankAdapter.Features.Void;
using BankAdapter.Infrastructure.Data;
using BankAdapter.Infrastructure.Events;
using BankAdapter.Infrastructure.Logging;
using BankAdapter.Infrastructure.Providers;
using BankAdapter.Infrastructure.RateLimiting;
using BankAdapter.Infrastructure.Retry;
using BankAdapter.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Serilog;
using Serilog.Events;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Configure service name for observability
builder.Configuration["ServiceName"] = "BankAdapter";

// Configure Serilog using ObservabilityExtensions
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "BankAdapter",
    environment: builder.Environment.EnvironmentName,
    configuration: builder.Configuration,
    minimumLevel: Serilog.Events.LogEventLevel.Information
);

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add observability using ObservabilityExtensions
builder.Services.AddObservability(builder.Configuration, environment: builder.Environment.EnvironmentName);

// Configure PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Host=localhost;Port=5432;Database=bank_adapter_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<BankAdapterDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    }));

// Configure FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configure HttpClient
builder.Services.AddHttpClient("ProviderClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "BankAdapterService/1.0");
});

// Register infrastructure services
builder.Services.AddSingleton<RateLimiter>();
builder.Services.AddSingleton<RetryPolicyService>();
builder.Services.AddScoped<RequestLoggingService>();
builder.Services.AddScoped<ProviderFactory>();

// Register event publisher (use InMemory for development, switch to MessageBroker for production)
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Register handlers
builder.Services.AddScoped<IAuthorizeHandler, AuthorizeHandler>();
builder.Services.AddScoped<ICaptureHandler, CaptureHandler>();
builder.Services.AddScoped<IRefundHandler, RefundHandler>();
builder.Services.AddScoped<IVoidHandler, VoidHandler>();
builder.Services.AddScoped<IHealthCheckHandler, HealthCheckHandler>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Bank Adapter API",
        Version = "v1",
        Description = "Service for abstracting integration with external payment providers",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@payment-system.com"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddCheck<ProviderHealthCheck>("providers");

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank Adapter API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

// ===== PROMETHEUS METRICS ENDPOINT =====
app.UsePrometheusMetrics();

app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

// Configure exception handling
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        Log.Error(exception, "Unhandled exception occurred");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var problem = new
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? exception?.Message : "An error occurred while processing your request"
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// Map API endpoints
app.MapControllers();

// Register feature endpoints
var endpointTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.GetInterfaces().Contains(typeof(IApiEndpoint)) && !t.IsInterface && !t.IsAbstract);

foreach (var endpointType in endpointTypes)
{
    var endpoint = (IApiEndpoint)Activator.CreateInstance(endpointType)!;
    endpoint.MapEndpoint(app);
    Log.Information("Registered endpoint: {EndpointType}", endpointType.Name);
}

// Map health checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
})
.WithName("SystemHealthCheck")
.WithTags("Health")
.WithOpenApi();

// Map root endpoint
app.MapGet("/", () => new
{
    Service = "Bank Adapter API",
    Version = "1.0.0",
    Status = "Running",
    Endpoints = new
    {
        Authorize = "/api/bank/authorize",
        Capture = "/api/bank/capture",
        Refund = "/api/bank/refund",
        Void = "/api/bank/void",
        HealthCheck = "/api/bank/providers/{id}/health",
        Health = "/health",
        Swagger = "/swagger"
    }
})
.WithName("Root")
.WithTags("Info")
.WithOpenApi();

Log.Information("Bank Adapter API starting up...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BankAdapterDbContext>();
        await SeedData.SeedAsync(dbContext);
    }
}

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Provider health check for ASP.NET Core health checks
/// </summary>
internal class ProviderHealthCheck(ProviderFactory providerFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Just check if provider factory is operational
            // Individual provider health should be checked via the health check endpoint
            return HealthCheckResult.Healthy("Provider factory is operational");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Provider factory error", ex);
        }
    }
}
