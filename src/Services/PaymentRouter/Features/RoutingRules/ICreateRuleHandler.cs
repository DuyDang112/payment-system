using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

internal interface ICreateRuleHandler : IHandler
{
    Task<Result<CreateRuleResponse>> HandleAsync(CreateRuleRequest request, CancellationToken cancellationToken);
}
