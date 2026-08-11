namespace PaymentRouter.Domain.Models;

/// <summary>
/// Payment provider entity
/// </summary>
public sealed class PaymentProvider
{
    public string ProviderId { get; private set; } = string.Empty;
    public string ProviderName { get; private set; } = string.Empty;
    public ProviderType ProviderType { get; private set; }
    public string[] SupportedCurrencies { get; private set; } = Array.Empty<string>();
    public PaymentMethod[] SupportedMethods { get; private set; } = Array.Empty<PaymentMethod>();
    public int Priority { get; private set; }
    public bool IsEnabled { get; private set; }
    public decimal MinAmount { get; private set; }
    public decimal MaxAmount { get; private set; }
    public string CostConfigJson { get; private set; } = string.Empty;
    public string MetricsJson { get; private set; } = string.Empty;
    public HealthStatus HealthStatus { get; private set; }
    public CircuitState CircuitBreakerState { get; private set; }
    public DateTime LastHealthCheck { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PaymentProvider() { }

    public static PaymentProvider Create(
        string providerId,
        string providerName,
        ProviderType providerType,
        string[] supportedCurrencies,
        PaymentMethod[] supportedMethods,
        int priority,
        CostConfiguration costConfig)
    {
        return new PaymentProvider
        {
            ProviderId = providerId,
            ProviderName = providerName,
            ProviderType = providerType,
            SupportedCurrencies = supportedCurrencies,
            SupportedMethods = supportedMethods,
            Priority = priority,
            IsEnabled = true,
            MinAmount = 0.01m,
            MaxAmount = 1000000m,
            CostConfigJson = System.Text.Json.JsonSerializer.Serialize(costConfig),
            MetricsJson = System.Text.Json.JsonSerializer.Serialize(new PerformanceMetrics()),
            HealthStatus = HealthStatus.HEALTHY,
            CircuitBreakerState = CircuitState.CLOSED,
            LastHealthCheck = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public CostConfiguration GetCostConfiguration()
    {
        return System.Text.Json.JsonSerializer.Deserialize<CostConfiguration>(CostConfigJson)
            ?? new CostConfiguration(new Money(0, "USD"), 0);
    }

    public PerformanceMetrics GetPerformanceMetrics()
    {
        return System.Text.Json.JsonSerializer.Deserialize<PerformanceMetrics>(MetricsJson)
            ?? new PerformanceMetrics();
    }

    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        MetricsJson = System.Text.Json.JsonSerializer.Serialize(metrics);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateHealthStatus(HealthStatus status)
    {
        HealthStatus = status;
        LastHealthCheck = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCircuitBreakerState(CircuitState state)
    {
        CircuitBreakerState = state;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool SupportsCurrency(string currency) =>
        SupportedCurrencies.Contains(currency);

    public bool SupportsMethod(PaymentMethod method) =>
        SupportedMethods.Contains(method);

    public bool IsWithinLimits(decimal amount) =>
        amount >= MinAmount && amount <= MaxAmount;

    public bool IsHealthy() =>
        IsEnabled && HealthStatus != HealthStatus.UNHEALTHY;

    public bool IsAvailable() =>
        IsEnabled && CircuitBreakerState != CircuitState.OPEN;
}
