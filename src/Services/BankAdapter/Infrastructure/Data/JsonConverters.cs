using System.Text.Json;
using System.Text.Json.Serialization;
using BankAdapter.Domain;

namespace BankAdapter.Infrastructure.Data;

/// <summary>
/// JSON converters for complex types stored in database
/// </summary>
public static class JsonConverters
{
    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}

/// <summary>
/// Converter for AuthConfig
/// </summary>
public sealed class AuthConfigConverter : JsonConverter<AuthConfig>
{
    public override AuthConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var typeStr = root.GetProperty("type").GetString() ?? "api_key";
        var type = Enum.Parse<AuthType>(typeStr, ignoreCase: true);

        var credentials = new Dictionary<string, string>();
        if (root.TryGetProperty("credentials", out var credsElement))
        {
            foreach (var prop in credsElement.EnumerateObject())
            {
                credentials[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        return new AuthConfig(type, credentials);
    }

    public override void Write(Utf8JsonWriter writer, AuthConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type.ToString());
        writer.WritePropertyName("credentials");
        writer.WriteStartObject();
        foreach (var kvp in value.Credentials)
        {
            writer.WriteString(kvp.Key, kvp.Value);
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

/// <summary>
/// Converter for RateLimits
/// </summary>
public sealed class RateLimitsConverter : JsonConverter<RateLimits>
{
    public override RateLimits Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var maxRequestsPerSecond = root.GetProperty("max_requests_per_second").GetInt32();
        var maxConcurrentRequests = root.GetProperty("max_concurrent_requests").GetInt32();

        return new RateLimits(maxRequestsPerSecond, maxConcurrentRequests);
    }

    public override void Write(Utf8JsonWriter writer, RateLimits value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("max_requests_per_second", value.MaxRequestsPerSecond);
        writer.WriteNumber("max_concurrent_requests", value.MaxConcurrentRequests);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Converter for RetryConfig
/// </summary>
public sealed class RetryConfigConverter : JsonConverter<RetryConfig>
{
    public override RetryConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var maxRetries = root.GetProperty("max_retries").GetInt32();
        var backoffMs = root.GetProperty("backoff_ms").GetInt32();

        var retryableErrors = new HashSet<string>();
        if (root.TryGetProperty("retryable_errors", out var errorsElement))
        {
            foreach (var error in errorsElement.EnumerateArray())
            {
                retryableErrors.Add(error.GetString() ?? string.Empty);
            }
        }

        return new RetryConfig(maxRetries, backoffMs, retryableErrors);
    }

    public override void Write(Utf8JsonWriter writer, RetryConfig value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("max_retries", value.MaxRetries);
        writer.WriteNumber("backoff_ms", value.BackoffMs);
        writer.WritePropertyName("retryable_errors");
        writer.WriteStartArray();
        foreach (var error in value.RetryableErrors)
        {
            writer.WriteStringValue(error);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

/// <summary>
/// Converter for Timeouts
/// </summary>
public sealed class TimeoutsConverter : JsonConverter<Timeouts>
{
    public override Timeouts Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var connectTimeoutMs = root.GetProperty("connect_timeout_ms").GetInt32();
        var readTimeoutMs = root.GetProperty("read_timeout_ms").GetInt32();

        return new Timeouts(connectTimeoutMs, readTimeoutMs);
    }

    public override void Write(Utf8JsonWriter writer, Timeouts value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("connect_timeout_ms", value.ConnectTimeoutMs);
        writer.WriteNumber("read_timeout_ms", value.ReadTimeoutMs);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Converter for HashSet<Operation>
/// </summary>
public sealed class OperationSetConverter : JsonConverter<HashSet<Operation>>
{
    public override HashSet<Operation> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var operations = new HashSet<Operation>();
        foreach (var element in root.EnumerateArray())
        {
            var opStr = element.GetString();
            if (opStr != null && Enum.TryParse<Operation>(opStr, ignoreCase: true, out var op))
            {
                operations.Add(op);
            }
        }

        return operations;
    }

    public override void Write(Utf8JsonWriter writer, HashSet<Operation> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var op in value)
        {
            writer.WriteStringValue(op.ToString());
        }
        writer.WriteEndArray();
    }
}
