namespace PaymentRouter.Domain.Models;

/// <summary>
/// Supported payment methods
/// </summary>
public enum PaymentMethod
{
    CREDIT_CARD,
    DEBIT_CARD,
    BANK_TRANSFER,
    DIGITAL_WALLET,
    CRYPTO,
    BNPL
}
