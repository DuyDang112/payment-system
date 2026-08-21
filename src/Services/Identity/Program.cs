using System.Reflection;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Identity.Features.Authorize;
using Identity.Features.HealthCheck;
using Identity.Features.Token;
using Identity.Features.UserInfo;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Events;
using Identity.Infrastructure.Logging;
using Identity.Shared;
using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AppIdentityUser = Identity.Domain.IdentityUser;
using AppIdentityRole = Identity.Domain.IdentityRole;

var builder = WebApplication.CreateBuilder(args);

// Configure service name for observability
builder.Configuration["ServiceName"] = "Identity";

// Configure Serilog
builder.Environment.ConfigureSerilog(builder.Configuration, minimumLevel: Serilog.Events.LogEventLevel.Information);

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Host=localhost;Port=5432;Database=identity_db;Username=postgres;Password=postgres";

// Add Identity and IdentityServer
builder.Services.AddIdentity<AppIdentityUser, AppIdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    // Disable automatic key management to avoid license requirement
    options.KeyManagement.Enabled = false;

    // Enable event logging
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddInMemoryIdentityResources(SeedData.GetIdentityResources())
    .AddInMemoryApiResources(SeedData.GetApiResources())
    .AddInMemoryApiScopes(SeedData.GetApiScopes())
    .AddInMemoryClients(SeedData.GetClients())
    .AddDeveloperSigningCredential();

// Add DbContexts
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    }));

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Identity Server API",
        Version = "v1",
        Description = "Duende IdentityServer for authentication and authorization in the Payment System",
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
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Server API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

// IdentityServer middleware
app.UseIdentityServer();

// Configure exception handling
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
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
app.MapHealthChecks("/health");

// Map root endpoint
app.MapGet("/", () => new
{
    Service = "Identity Server API",
    Version = "1.0.0",
    Status = "Running",
    Endpoints = new
    {
        Authorize = "/connect/authorize",
        Token = "/connect/token",
        UserInfo = "/connect/userinfo",
        Discovery = "/.well-known/openid-configuration",
        Health = "/health",
        Swagger = "/swagger"
    },
    SupportedFlows = new
    {
        AuthorizationCode = "User to App authentication",
        ClientCredentials = "App to App authentication",
        RefreshToken = "Token refresh"
    }
})
.WithName("Root")
.WithTags("Info");

// IdentityServer discovery endpoint
app.MapGet("/.well-known/openid-configuration", () => Results.Ok(new
{
    issuer = "https://localhost:5001",
    authorization_endpoint = "/connect/authorize",
    token_endpoint = "/connect/token",
    userinfo_endpoint = "/connect/userinfo",
    end_session_endpoint = "/connect/endsession",
    jwks_uri = "/.well-known/openid-configuration/jwks",
    response_types_supported = new[] { "code", "token", "id_token" },
    subject_types_supported = new[] { "public" },
    id_token_signing_alg_values_supported = new[] { "RS256" },
    scopes_supported = new[] { "openid", "profile", "email", "payment_api", "risk_assessment_api", "bank_adapter_api", "offline_access" },
    token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" },
    grant_types_supported = new[] { "authorization_code", "client_credentials", "refresh_token" },
    code_challenge_methods_supported = new[] { "plain", "S256" }
}))
.WithName("Discovery")
.WithTags("Discovery")
.AllowAnonymous();

Log.Information("Identity Server API starting up...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

// Initialize databases
using (var scope = app.Services.CreateScope())
{
    try
    {
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // Create database schema if it doesn't exist
        await identityDbContext.Database.EnsureCreatedAsync();

        await SeedData.SeedAsync(app.Services);

        Log.Information("Databases initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to initialize or seed databases");
        throw;
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
