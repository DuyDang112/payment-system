using PaymentRouter.Shared;

namespace PaymentRouter.Features.RoutingRules;

internal interface IListRulesHandler : IHandler
{
    Task<Result<ListRulesResponse>> HandleAsync(ListRulesRequest request, CancellationToken cancellationToken);
}
