namespace PaymentRouter.Domain.Events;

/// <summary>
/// Event published when provider circuit breaker opens
/// </summary>
public sealed record ProviderCircuitOpenedEvent(
    string ProviderId,
    int FailureCount,
    DateTime OpenedAt
) : IEvent;
