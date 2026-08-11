using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal interface IListProvidersHandler : IHandler
{
    Task<Result<ListProvidersResponse>> HandleAsync(ListProvidersRequest request, CancellationToken cancellationToken);
}
