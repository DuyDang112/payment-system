using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RiskAssessment.Domain.Models;
using RiskAssessment.Features.EvaluateRisk;
using RiskAssessment.Infrastructure.Data;
using RiskAssessment.Infrastructure.RuleEngine;
using RiskAssessment.Infrastructure.VelocityChecking;
using RiskAssessment.Shared;
using Serilog;
using Shared;

// Configure Serilog using ObservabilityExtensions
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "RiskAssessment",
    serviceVersion: "1.0.0",
    minimumLevel: Serilog.Events.LogEventLevel.Information
);

try
{
    Log.Information("Starting Risk Assessment Service");

    var builder = WebApplication.CreateBuilder(args);

    // Configure service name for observability
    builder.Configuration["ServiceName"] = "RiskAssessment";

    // Use Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();

    // OpenAPI/Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Risk Assessment API",
            Version = "v1",
            Description = "Real-time risk evaluation service for payment transactions"
        });

        // Include XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // Database Configuration
    builder.Services.AddDbContext<RiskAssessmentDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.EnableRetryOnFailure(3);
        });
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

    // Add observability using ObservabilityExtensions
    builder.Services.AddObservability(builder.Configuration, serviceVersion: "1.0.0");

    // Activity Source for custom instrumentation
    var activitySource = new System.Diagnostics.ActivitySource("RiskAssessment");
    builder.Services.AddSingleton(activitySource);

    // Application Services
    builder.Services.AddScoped<IEvaluateRiskHandler, EvaluateRiskHandler>();
    builder.Services.AddScoped<IRuleEngine, RuleEngine>();
    builder.Services.AddScoped<IVelocityChecker, VelocityChecker>();

    // Rule Evaluators
    builder.Services.AddSingleton<IRuleEvaluator, VelocityRuleEvaluator>();
    builder.Services.AddSingleton<IRuleEvaluator, BlacklistRuleEvaluator>();
    builder.Services.AddSingleton<IRuleEvaluator, AmountRuleEvaluator>();
    builder.Services.AddSingleton<IRuleEvaluator, GeoRuleEvaluator>();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
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
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Risk Assessment API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    app.UseSerilogRequestLogging();

    // ===== PROMETHEUS METRICS ENDPOINT =====
    app.UsePrometheusMetrics(); 

    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    // Map API endpoints
    var apiEndpointTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => typeof(IApiEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    foreach (var endpointType in apiEndpointTypes)
    {
        var endpointInstance = (IApiEndpoint)Activator.CreateInstance(endpointType)!;
        endpointInstance.MapEndpoint(app);

        Log.Information("Mapped endpoint: {EndpointType}", endpointType.Name);
    }

    Log.Information("Risk Assessment Service started successfully");

    // Seed database in development
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RiskAssessmentDbContext>();
            await SeedData.SeedAsync(dbContext);
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Risk Assessment Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
