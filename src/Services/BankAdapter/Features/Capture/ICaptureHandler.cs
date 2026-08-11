using BankAdapter.Shared;

namespace BankAdapter.Features.Capture;

/// <summary>
/// Interface for capture handler
/// </summary>
public interface ICaptureHandler : IHandler
{
    Task<Result<CaptureResponse>> HandleAsync(CaptureRequest request, CancellationToken cancellationToken);
}
