namespace PaymentRouter.Domain.Events;

/// <summary>
/// Event published when provider circuit breaker closes
/// </summary>
public sealed record ProviderCircuitClosedEvent(
    string ProviderId,
    DateTime ClosedAt
) : IEvent;
