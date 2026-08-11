namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// Response after creating a routing rule
/// </summary>
public sealed record CreateRuleResponse(
    string RuleId,
    string Name,
    bool IsActive
);
