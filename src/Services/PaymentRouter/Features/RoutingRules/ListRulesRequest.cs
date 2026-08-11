namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// Request to list routing rules
/// </summary>
public sealed record ListRulesRequest(
    string? MerchantId = null,
    bool? IsActive = null
);
