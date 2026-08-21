using Serilog;
using Serilog.Events;

namespace Identity.Infrastructure.Logging;

/// <summary>
/// Extensions for configuring logging in the Identity service
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog for the Identity service
    /// </summary>
    public static void ConfigureSerilog(
        this IWebHostEnvironment environment,
        IConfiguration configuration,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Duende", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "Identity")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/identity-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Identity service logging configured");
        Log.Information("Environment: {Environment}", environment.EnvironmentName);
    }
}
