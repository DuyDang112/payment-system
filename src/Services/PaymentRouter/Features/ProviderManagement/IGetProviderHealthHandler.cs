using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal interface IGetProviderHealthHandler : IHandler
{
    Task<Result<GetProviderHealthResponse>> HandleAsync(GetProviderHealthRequest request, CancellationToken cancellationToken);
}
