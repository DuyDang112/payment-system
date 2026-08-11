using Microsoft.AspNetCore.Http.HttpResults;

namespace PaymentProcessing.Shared;

/// <summary>
/// Extension methods for converting Results to ASP.NET Core results
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        var firstError = result.Errors.First();
        return Results.Problem(
            statusCode: GetStatusCode(firstError),
            title: firstError.Code,
            detail: firstError.Message);
    }

    public static IResult ToProblem<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var firstError = result.Errors.First();
        return Results.Problem(
            statusCode: GetStatusCode(firstError),
            title: firstError.Code,
            detail: firstError.Message);
    }

    private static int GetStatusCode(Error error)
    {
        return error.Code switch
        {
            "Error.NotFound" => StatusCodes.Status404NotFound,
            "Error.Conflict" => StatusCodes.Status409Conflict,
            "Error.Validation" => StatusCodes.Status400BadRequest,
            "Payment.AlreadyExists" => StatusCodes.Status409Conflict,
            "Payment.NotFound" => StatusCodes.Status404NotFound,
            "Payment.InvalidState" => StatusCodes.Status400BadRequest,
            "Payment.InvalidAmount" => StatusCodes.Status400BadRequest,
            "Payment.InvalidCurrency" => StatusCodes.Status400BadRequest,
            "Payment.IdempotencyKeyExpired" => StatusCodes.Status410Gone,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
