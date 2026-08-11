using BankAdapter.Shared;

namespace BankAdapter.Features.Authorize;

/// <summary>
/// Interface for authorize handler
/// </summary>
public interface IAuthorizeHandler : IHandler
{
    Task<Result<AuthorizeResponse>> HandleAsync(AuthorizeRequest request, CancellationToken cancellationToken);
}
