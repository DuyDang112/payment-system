using Microsoft.EntityFrameworkCore;
using PaymentProcessing.Domain.Events;
using PaymentProcessing.Domain.Models;
using PaymentProcessing.Features.Shared.Errors;
using PaymentProcessing.Infrastructure.Data;
using PaymentProcessing.Infrastructure.Events;
using PaymentProcessing.Shared;
using Serilog;

namespace PaymentProcessing.Features.CancelPayment;

/// <summary>
/// Handler for cancelling payments
/// </summary>
internal sealed class CancelPaymentHandler(
    PaymentsDbContext context,
    IEventPublisher eventPublisher,
    ILogger<CancelPaymentHandler> logger) : ICancelPaymentHandler
{
    public async Task<Result<CancelPaymentResponse>> HandleAsync(
        string paymentId,
        CancelPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, cancellationToken);

        if (payment == null)
        {
            logger.LogInformation("Payment {PaymentId} not found", paymentId);
            return PaymentErrors.NotFound;
        }

        try
        {
            payment.Cancel();

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Payment {PaymentId} cancelled", paymentId);

            // Publish domain events
            await eventPublisher.PublishDomainEventsAsync(payment.DomainEvents, cancellationToken);
            payment.ClearDomainEvents();

            return new CancelPaymentResponse(
                payment.PaymentId,
                payment.Status.ToString().ToUpperInvariant(),
                DateTime.UtcNow
            );
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("completed"))
        {
            logger.LogInformation("Cannot cancel completed payment {PaymentId}", paymentId);
            return PaymentErrors.CannotCancelCompleted;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("failed"))
        {
            logger.LogInformation("Cannot cancel failed payment {PaymentId}", paymentId);
            return PaymentErrors.CannotCancelFailed;
        }
    }
}
