using Microsoft.AspNetCore.Http.HttpResults;

namespace RiskAssessment.Shared;

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
        "Risk.EvaluationNotFound" => StatusCodes.Status404NotFound,
        "Risk.RuleNotFound" => StatusCodes.Status404NotFound,
        "Risk.MerchantProfileNotFound" => StatusCodes.Status404NotFound,
        "Risk.InvalidScore" => StatusCodes.Status400BadRequest,
        "Risk.InvalidConditions" => StatusCodes.Status400BadRequest,
        "Risk.VelocityExceeded" => StatusCodes.Status429TooManyRequests,
        "Risk.EntityBlacklisted" => StatusCodes.Status403Forbidden,
        "Error.Validation" => StatusCodes.Status400BadRequest,
        "Error.NotFound" => StatusCodes.Status404NotFound,
        "Error.Conflict" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
