using PaymentRouter.Shared;

namespace PaymentRouter.Features.Shared.Errors;

public static class RoutingErrors
{
    public static readonly Error ProviderNotFound = new(
        "Routing.ProviderNotFound",
        "Payment provider not found");

    public static readonly Error NoHealthyProviders = new(
        "Routing.NoHealthyProviders",
        "No healthy providers available for routing");

    public static readonly Error ProviderDisabled = new(
        "Routing.ProviderDisabled",
        "Payment provider is disabled");

    public static readonly Error UnsupportedCurrency = new(
        "Routing.UnsupportedCurrency",
        "Provider does not support the specified currency");

    public static readonly Error UnsupportedPaymentMethod = new(
        "Routing.UnsupportedPaymentMethod",
        "Provider does not support the specified payment method");

    public static readonly Error AmountOutOfRange = new(
        "Routing.AmountOutOfRange",
        "Amount is out of provider's supported range");

    public static readonly Error RuleNotFound = new(
        "Routing.RuleNotFound",
        "Routing rule not found");

    public static readonly Error InvalidRoutingParameters = new(
        "Routing.InvalidParameters",
        "Invalid routing parameters provided");
}
