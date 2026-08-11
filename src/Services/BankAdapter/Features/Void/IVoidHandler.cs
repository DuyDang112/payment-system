using BankAdapter.Shared;

namespace BankAdapter.Features.Void;

/// <summary>
/// Interface for void handler
/// </summary>
public interface IVoidHandler : IHandler
{
    Task<Result<VoidResponse>> HandleAsync(VoidRequest request, CancellationToken cancellationToken);
}
