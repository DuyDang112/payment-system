using Microsoft.AspNetCore.Http.HttpResults;

namespace PaymentRouter.Shared;

/// <summary>
/// Extension methods for converting Result<T> to HTTP responses
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        var firstError = result.Errors.First();
        return Results.Problem(
            statusCode: GetStatusCode(firstError),
            title: firstError.Code,
            detail: firstError.Message);
    }

    public static IResult ToProblem<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        var firstError = result.Errors.First();
        return Results.Problem(
            statusCode: GetStatusCode(firstError),
            title: firstError.Code,
            detail: firstError.Message);
    }

    private static int GetStatusCode(Error error) => error.Code switch
    {
        "Routing.ProviderNotFound" => StatusCodes.Status404NotFound,
        "Routing.NoHealthyProviders" => StatusCodes.Status503ServiceUnavailable,
        "Routing.ProviderDisabled" => StatusCodes.Status400BadRequest,
        "Routing.UnsupportedCurrency" => StatusCodes.Status400BadRequest,
        "Routing.UnsupportedPaymentMethod" => StatusCodes.Status400BadRequest,
        "Routing.AmountOutOfRange" => StatusCodes.Status400BadRequest,
        "Routing.RuleNotFound" => StatusCodes.Status404NotFound,
        "Error.Validation" => StatusCodes.Status400BadRequest,
        "Error.NotFound" => StatusCodes.Status404NotFound,
        "Error.Conflict" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
