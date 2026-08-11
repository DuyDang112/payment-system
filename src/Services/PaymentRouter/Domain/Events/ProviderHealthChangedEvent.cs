using PaymentRouter.Domain.Models;

namespace PaymentRouter.Domain.Events;

/// <summary>
/// Event published when provider health status changes
/// </summary>
public sealed record ProviderHealthChangedEvent(
    string ProviderId,
    HealthStatus PreviousStatus,
    HealthStatus NewStatus,
    DateTime ChangedAt
) : IEvent;
