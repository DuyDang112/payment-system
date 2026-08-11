using Microsoft.EntityFrameworkCore;
using PaymentProcessing.Features.Shared.Errors;
using PaymentProcessing.Infrastructure.Data;
using PaymentProcessing.Shared;
using Serilog;

namespace PaymentProcessing.Features.GetPayment;

/// <summary>
/// Handler for getting a payment by ID
/// </summary>
internal sealed class GetPaymentHandler(
    PaymentsDbContext context,
    ILogger<GetPaymentHandler> logger) : IGetPaymentHandler
{
    public async Task<Result<GetPaymentResponse>> HandleAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(p => p.Attempts)
            .Include(p => p.StateTransitions)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, cancellationToken);

        if (payment == null)
        {
            logger.LogInformation("Payment {PaymentId} not found", paymentId);
            return PaymentErrors.NotFound;
        }

        logger.LogDebug("Retrieved payment {PaymentId}", paymentId);
        return payment.MapToGetResponse();
    }
}
