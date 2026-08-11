using BankAdapter.Shared;

namespace BankAdapter.Features.Refund;

/// <summary>
/// Interface for refund handler
/// </summary>
public interface IRefundHandler : IHandler
{
    Task<Result<RefundResponse>> HandleAsync(RefundRequest request, CancellationToken cancellationToken);
}
