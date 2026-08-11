using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

internal sealed class ListRulesHandler(
    PaymentRouterDbContext context,
    ILogger<ListRulesHandler> logger) : IListRulesHandler
{
    public async Task<Result<ListRulesResponse>> HandleAsync(
        ListRulesRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.RoutingRules.AsQueryable();

        if (!string.IsNullOrEmpty(request.MerchantId))
        {
            query = query.Where(r => r.MerchantId == request.MerchantId);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        var rules = await query
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        var ruleDtos = rules.Select(r => new RoutingRuleDto(
            r.RuleId,
            r.MerchantId,
            r.Name,
            r.Priority,
            r.IsActive,
            MapToConditionDto(r.GetConditions()),
            r.PreferredProviderIds,
            r.Strategy,
            r.CreatedAt,
            r.UpdatedAt
        )).ToArray();

        logger.LogInformation("Retrieved {Count} routing rules", ruleDtos.Length);

        return Result<ListRulesResponse>.Success(new ListRulesResponse(ruleDtos));
    }

    private static RoutingRuleConditionDto MapToConditionDto(RoutingRuleCondition condition) =>
        new(
            condition.Currencies,
            condition.PaymentMethods,
            condition.MinAmount,
            condition.MaxAmount,
            condition.Countries
        );
}
