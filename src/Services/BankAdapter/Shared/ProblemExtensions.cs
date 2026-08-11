using Microsoft.AspNetCore.Mvc;

namespace BankAdapter.Shared;

/// <summary>
/// Extension methods for converting Results to Problem responses
/// </summary>
public static class ProblemExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Code switch
        {
            "Error.NotFound" => 404,
            "Error.Conflict" => 409,
            "Error.Validation" => 400,
            _ => 500
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message
        );
    }

    public static IResult ToProblem(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.Problem(statusCode: 500, title: "Unknown error");
        }

        return errors[0].ToProblem();
    }

    public static IResult ToProblem(this Result result)
    {
        return result.Errors.ToProblem();
    }

    public static IResult ToProblem(this Result result, int statusCode)
    {
        if (result.Errors.Count == 0)
        {
            return Results.Problem(statusCode: statusCode, title: "Unknown error");
        }

        return Results.Problem(
            statusCode: statusCode,
            title: result.Errors[0].Code,
            detail: result.Errors[0].Message
        );
    }
}
