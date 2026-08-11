namespace BankAdapter.Features.Shared.Routes;

/// <summary>
/// Constants for API routes
/// </summary>
public static class RouteConsts
{
    public const string BaseRoute = "/api/bank";
    public const string Authorize = "/api/bank/authorize";
    public const string Capture = "/api/bank/capture";
    public const string Refund = "/api/bank/refund";
    public const string Void = "/api/bank/void";
    public const string HealthCheck = "/api/bank/providers/{id}/health";
}
