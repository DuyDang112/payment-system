namespace PaymentRouter.Domain.Models;

/// <summary>
/// Health status of a payment provider
/// </summary>
public enum HealthStatus
{
    HEALTHY,
    DEGRADED,
    UNHEALTHY
}
