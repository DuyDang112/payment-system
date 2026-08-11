using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentRouter.Domain.Events;
using PaymentRouter.Infrastructure.CircuitBreaking;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Infrastructure.Events;
using PaymentRouter.Infrastructure.RoutingEngine;
using PaymentRouter.Features.ProviderManagement;
using PaymentRouter.Features.RoutePayment;
using PaymentRouter.Features.RoutingRules;
using PaymentRouter.Shared;
using Serilog;
using Shared;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure service name for observability
builder.Configuration["ServiceName"] = "PaymentRouter";

// Configure Serilog using ObservabilityExtensions
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "PaymentRouter",
    serviceVersion: "1.0.0",
    minimumLevel: Serilog.Events.LogEventLevel.Information
);

builder.Host.UseSerilog();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaymentRouterDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Configure JSON serialization to handle string-to-enum conversion
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Register infrastructure services
builder.Services.AddScoped<ICircuitBreakerManager, CircuitBreakerManager>();
builder.Services.AddScoped<IProviderSelector, ProviderSelector>();
builder.Services.AddScoped<ICostCalculator, CostCalculator>();
builder.Services.AddScoped<IRoutingEngine, RoutingEngine>();

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// Add observability using ObservabilityExtensions
builder.Services.AddObservability(builder.Configuration, serviceVersion: "1.0.0");

// Register handlers
builder.Services.AddScoped<IRoutePaymentHandler, RoutePaymentHandler>();
builder.Services.AddScoped<IListProvidersHandler, ListProvidersHandler>();
builder.Services.AddScoped<IAddProviderHandler, AddProviderHandler>();
builder.Services.AddScoped<IGetProviderHealthHandler, GetProviderHealthHandler>();
builder.Services.AddScoped<IListRulesHandler, ListRulesHandler>();
builder.Services.AddScoped<ICreateRuleHandler, CreateRuleHandler>();

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Router API",
        Version = "v1",
        Description = "Intelligent payment routing service with circuit breaking, cost optimization, and provider selection",
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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Router API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// ===== PROMETHEUS METRICS ENDPOINT =====
app.UsePrometheusMetrics(); 

app.UseSerilogRequestLogging();

app.UseCors();

// Register endpoints
using (var scope = app.Services.CreateScope())
{
    var endpoints = scope.ServiceProvider.GetRequiredService<IServiceProvider>();
    var endpointTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterface(nameof(IApiEndpoint)) != null);

    foreach (var endpointType in endpointTypes)
    {
        var endpointInstance = (IApiEndpoint)ActivatorUtilities.CreateInstance(endpoints, endpointType);
        endpointInstance.MapEndpoint(app);

            Log.Information("Registered endpoint: {EndpointName}", endpointType.Name);
    }
}

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "PaymentRouter",
    Timestamp = DateTime.UtcNow,
    Endpoints = "Health check working"
}))
.WithName("HealthCheck")
.WithOpenApi()
.WithTags("Health");

Log.Information("Payment Router API initialized");
Log.Information("Swagger UI available at: http://localhost:5032 or https://localhost:7131");

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentRouterDbContext>();
        await SeedData.SeedAsync(dbContext);
    }
}

app.Run();
