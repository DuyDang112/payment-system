using PaymentRouter.Shared;

namespace PaymentRouter.Features.ProviderManagement;

internal interface IAddProviderHandler : IHandler
{
    Task<Result<AddProviderResponse>> HandleAsync(AddProviderRequest request, CancellationToken cancellationToken);
}
