namespace PaymentRouter.Domain.Models;

/// <summary>
/// Circuit breaker state
/// </summary>
public enum CircuitState
{
    CLOSED,
    OPEN,
    HALF_OPEN
}
