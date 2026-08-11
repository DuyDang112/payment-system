using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// Request to create a routing rule
/// </summary>
public sealed record CreateRuleRequest(
    string MerchantId,
    string Name,
    int Priority,
    RoutingRuleConditionDto Conditions,
    string[] PreferredProviderIds,
    RoutingStrategy Strategy
);
