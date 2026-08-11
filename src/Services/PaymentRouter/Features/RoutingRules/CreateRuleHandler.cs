using Microsoft.EntityFrameworkCore;
using PaymentRouter.Domain.Models;
using PaymentRouter.Features.Shared.Errors;
using PaymentRouter.Infrastructure.Data;
using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

internal sealed class CreateRuleHandler(
    PaymentRouterDbContext context,
    ILogger<CreateRuleHandler> logger) : ICreateRuleHandler
{
    public async Task<Result<CreateRuleResponse>> HandleAsync(
        CreateRuleRequest request,
        CancellationToken cancellationToken)
    {
        // Check if rule already exists for this merchant
        var exists = await context.RoutingRules
            .AnyAsync(r => r.MerchantId == request.MerchantId && r.Name == request.Name, cancellationToken);

        if (exists)
        {
            logger.LogWarning("Routing rule {Name} already exists for merchant {MerchantId}", request.Name, request.MerchantId);
            return Result<CreateRuleResponse>.Failure(
                new Error("Routing.RuleExists", $"Rule {request.Name} already exists for merchant {request.MerchantId}"));
        }

        // Map DTO to domain condition
        var conditions = new RoutingRuleCondition
        {
            Currencies = request.Conditions.Currencies,
            PaymentMethods = request.Conditions.PaymentMethods,
            MinAmount = request.Conditions.MinAmount,
            MaxAmount = request.Conditions.MaxAmount,
            Countries = request.Conditions.Countries
        };

        // Create rule
        var rule = RoutingRule.Create(
            request.MerchantId,
            request.Name,
            request.Priority,
            conditions,
            request.PreferredProviderIds,
            request.Strategy
        );

        await context.RoutingRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created routing rule {RuleId} - {Name} for merchant {MerchantId}", rule.RuleId, rule.Name, rule.MerchantId);

        return Result<CreateRuleResponse>.Success(new CreateRuleResponse(
            rule.RuleId,
            rule.Name,
            rule.IsActive
        ));
    }
}
