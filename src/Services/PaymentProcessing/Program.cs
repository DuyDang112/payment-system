using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PaymentProcessing.Features.CancelPayment;
using PaymentProcessing.Features.CreatePayment;
using PaymentProcessing.Features.GetPayment;
using PaymentProcessing.Features.Shared.Clients;
using PaymentProcessing.Infrastructure.Data;
using PaymentProcessing.Infrastructure.Events;
using PaymentProcessing.Shared;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Shared;

// Create web application builder

var builder = WebApplication.CreateBuilder(args);

// Configure service name for observability
builder.Configuration["ServiceName"] = "PaymentProcessing";

// Configure Serilog using ObservabilityExtensions
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "PaymentProcessing",
    environment: builder.Environment.EnvironmentName,
    configuration: builder.Configuration,
    minimumLevel: Serilog.Events.LogEventLevel.Information
);

builder.Host.UseSerilog();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Register handlers
builder.Services.AddScoped<ICreatePaymentHandler, CreatePaymentHandler>();
builder.Services.AddScoped<IGetPaymentHandler, GetPaymentHandler>();
builder.Services.AddScoped<ICancelPaymentHandler, CancelPaymentHandler>();

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// Add observability using ObservabilityExtensions
builder.Services.AddObservability(builder.Configuration, environment: builder.Environment.EnvironmentName);

// Configure HTTP clients for inter-service communication
builder.Services.AddHttpClient<RiskAssessmentClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:RiskAssessment:BaseUrl"] ?? "https://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<PaymentRouterClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:PaymentRouter:BaseUrl"] ?? "https://localhost:5032");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<BankAdapterClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BankAdapter:BaseUrl"] ?? "https://localhost:5125");
    client.Timeout = TimeSpan.FromSeconds(45);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Processing API",
        Version = "v1",
        Description = "API for orchestrating payment workflows",
        Contact = new OpenApiContact
        {
            Name = "Payment System",
            Url = new Uri("https://github.com/payment-system")
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

builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Processing API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// ===== PROMETHEUS METRICS ENDPOINT =====
app.UsePrometheusMetrics();

app.UseSerilogRequestLogging();

app.UseCors();

app.MapHealthChecks("/health");

// Register endpoints
var endpoints = app.Services.GetRequiredService<IServiceProvider>();
var endpointTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterface(nameof(IApiEndpoint)) != null);

foreach (var endpointType in endpointTypes)
{
    var endpointInstance = (IApiEndpoint)ActivatorUtilities.CreateInstance(endpoints, endpointType);
    endpointInstance.MapEndpoint(app);

    Log.Information("Registered endpoint: {EndpointName}", endpointType.Name);
}

Log.Information("Payment Processing API initialized");

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await SeedData.SeedAsync(dbContext);
    }
}

app.Run();

// Retry policy for HTTP clients
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message}");
            });
}

// Circuit breaker policy for HTTP clients
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
}
