namespace PaymentProcessing.Features.Shared.Routes;

/// <summary>
/// Route constants for Payment API endpoints
/// </summary>
public static class RouteConsts
{
    public const string BaseRoute = "/api/payments";
    public const string GetByIdRoute = "/api/payments/{id}";
    public const string CancelRoute = "/api/payments/{id}/cancel";
}
