using System.Reflection;
using System.Security.Claims;
using System.Text;
using Authentication.Domain;
using Authentication.Features.Login;
using Authentication.Features.Register;
using Authentication.Features.Token;
using Authentication.Infrastructure.Data;
using Authentication.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICE CONFIGURATION =====
builder.Configuration["ServiceName"] = "Authentication";

// ===== SERILOG CONFIGURATION =====
ObservabilityExtensions.ConfigureSerilog(
    serviceName: "Authentication",
    environment: builder.Environment.EnvironmentName,
    configuration: builder.Configuration,
    minimumLevel: LogEventLevel.Information
);

builder.Host.UseSerilog();

// ===== DATABASE CONFIGURATION =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Host=localhost;Port=5432;Database=authentication;Username=postgres;Password=Postgres123";

builder.Services.AddDbContext<AuthenticationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    }));

// ===== PASSWORD HASHER =====
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AuthServer:Authority"];
        // options.RequireHttpsMetadata = Convert.ToBoolean(builder.Configuration["AuthServer:RequireHttpsMetadata"]);
        options.Audience = builder.Configuration["AuthServer:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ===== FLUENT VALIDATION =====
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ===== OBSERVABILITY =====
builder.Services.AddObservability(builder.Configuration, environment: builder.Environment.EnvironmentName);

// ===== HTTP CLIENT =====
builder.Services.AddHttpClient("IdentityServerClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "AuthenticationService/1.0");
});

// ===== REGISTER HANDLERS =====
builder.Services.AddScoped<ILoginHandler, LoginHandler>();
builder.Services.AddScoped<IRegisterHandler, RegisterHandler>();
builder.Services.AddScoped<ITokenHandler, Authentication.Features.Token.TokenHandler>();

// ===== JWT TOKEN SERVICE =====
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ===== SWAGGER =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Authentication API",
        Version = "v1",
        Description = "Simple JWT-based authentication service"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// ===== PROMETHEUS METRICS =====
app.UsePrometheusMetrics();

app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

// ===== EXCEPTION HANDLING =====
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

// ===== REGISTER API ENDPOINTS =====
var endpointTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.GetInterfaces().Contains(typeof(IApiEndpoint)) && !t.IsInterface && !t.IsAbstract);

foreach (var endpointType in endpointTypes)
{
    var endpoint = (IApiEndpoint)Activator.CreateInstance(endpointType)!;
    endpoint.MapEndpoint(app);
    Log.Information("Registered endpoint: {EndpointType}", endpointType.Name);
}

// ===== HEALTH CHECK =====
app.MapHealthChecks("/health");

// ===== ROOT ENDPOINT =====
app.MapGet("/", () => new
{
    Service = "Authentication",
    Version = "1.0.0",
    Status = "Running",
    Type = "Simple JWT Authentication",
    Endpoints = new
    {
        Login = "/api/auth/login",
        Register = "/api/auth/register",
        Token = "/api/auth/token",
        UserInfo = "/api/auth/userinfo",
        Health = "/health",
        Swagger = "/swagger"
    }
})
.WithName("Root")
.WithTags("Info")
.WithOpenApi();

Log.Information("Authentication Service starting up...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

// ===== SEED DATABASE IN DEVELOPMENT =====
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            await dbContext.Database.MigrateAsync();
            await SeedData.SeedAsync(dbContext);
            Log.Information("Database seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database seeding failed (may already exist)");
        }
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
/// JWT Token Service
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
}

/// <summary>
/// JWT Token Service Implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var jwtKey = _configuration["AuthServer:Key"]!;
        var jwtIssuer = _configuration["AuthServer:Authority"] ?? "AuthenticationService";
        var jwtAudience = _configuration["AuthServer:Audience"] ?? "PaymentSystem";

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}