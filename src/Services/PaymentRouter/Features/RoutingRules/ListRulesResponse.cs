using PaymentRouter.Domain.Models;

namespace PaymentRouter.Features.RoutingRules;

/// <summary>
/// Response containing list of routing rules
/// </summary>
public sealed record ListRulesResponse(
    RoutingRuleDto[] Rules
);

public sealed record RoutingRuleDto(
    string RuleId,
    string MerchantId,
    string Name,
    int Priority,
    bool IsActive,
    RoutingRuleConditionDto Conditions,
    string[] PreferredProviderIds,
    RoutingStrategy Strategy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record RoutingRuleConditionDto(
    string[]? Currencies,
    PaymentMethod[]? PaymentMethods,
    decimal? MinAmount,
    decimal? MaxAmount,
    string[]? Countries
);
