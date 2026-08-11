using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Shared;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds comprehensive observability (traces, metrics, logs) to services
    /// Integrates with Tempo (traces), Prometheus (metrics), and Loki (logs) via OTEL Collector
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceVersion = "1.0.0")
    {
        var serviceName = configuration["ServiceName"] ?? "UnknownService";
        var otelEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
        var environment = configuration["Environment"] ?? "development";

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.namespace"] = "PaymentSystem",
                        ["deployment.environment"] = environment,
                        ["service.type"] = "microservice"
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddSource("PaymentProcessing")
                    .AddSource("RiskAssessment")
                    .AddSource("PaymentRouter")
                    .AddSource("BankAdapter")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            var path = request.Path.Value ?? "";
                            var route = GetRouteCategory(path);
                            activity.AddTag("http.route", route);
                            activity.AddTag("http.method", request.Method);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.AddTag("http.status_code", (int)response.StatusCode);
                        };
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.AddTag("error.type", exception.GetType().Name);
                            activity.AddTag("error.message", exception.Message);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    .AddMeter("PrometheusMetrics")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            })
            .WithLogging(logging =>
            {
                logging
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
            });

        return services;
    }

    /// <summary>
    /// Configures Serilog with structured logging to Loki via OTEL Collector
    /// Each service should call this in their Program.cs
    /// </summary>
    public static void ConfigureSerilog(
        string serviceName,
        string serviceVersion = "1.0.0",
        string otelEndpoint = "http://otel-collector:4317",
        Serilog.Events.LogEventLevel minimumLevel = Serilog.Events.LogEventLevel.Information)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("service", serviceName)
            .Enrich.WithProperty("service_version", serviceVersion)
            .Enrich.WithProperty("service_namespace", "PaymentSystem")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                                "trace_id={TraceId} span_id={SpanId} " +
                                "service={Service} message={Message:lj}{NewLine}{Exception}")
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otelEndpoint;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.version"] = serviceVersion,
                    ["service.namespace"] = "PaymentSystem",
                    ["deployment.environment"] = "production"
                };
            })
            .CreateLogger();
    }

    /// <summary>
    /// Adds Prometheus HTTP metrics middleware
    /// Note: Prometheus scraping should be configured via app.UseMetricServer("/metrics") and app.UseHttpMetrics() in Program.cs
    /// </summary>
    public static IApplicationBuilder UsePrometheusMetrics(this IApplicationBuilder app)
    {
        // This is a placeholder - actual Prometheus configuration is done in Program.cs
        // via app.UseMetricServer("/metrics") and app.UseHttpMetrics()
        return app;
    }

    private static string GetRouteCategory(string path)
    {
        return path switch
        {
            var p when p.StartsWith("/api/payments") => "/api/payments",
            var p when p.StartsWith("/api/risk") => "/api/risk",
            var p when p.StartsWith("/api/router") => "/api/router",
            var p when p.StartsWith("/api/bank") => "/api/bank",
            var p when p.StartsWith("/health") => "/health",
            var p when p.StartsWith("/metrics") => "/metrics",
            _ => path
        };
    }
}
