using TheLife.Application.Common.Results;

namespace TheLife.Api.Common;

/// <summary>Turns handler results into HTTP responses, so every endpoint answers errors the same way.</summary>
internal static class ResultExtensions
{
    /// <summary>Success → 200 OK with the value (or 204 No Content for <see cref="Unit"/>). Failure → problem details.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.ToHttpResult(value => value is Unit ? Results.NoContent() : Results.Ok(value));

    /// <summary>Same as above, but lets the endpoint choose the success response (e.g. 201 Created).</summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error!.ToProblem();

    /// <summary>
    /// Builds an RFC 7807 "problem details" response. The frontend shows <c>detail</c> to the user
    /// and uses <c>errors</c> (field → messages) to highlight form fields.
    /// </summary>
    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.FieldErrors is not null)
        {
            return Results.ValidationProblem(
                error.FieldErrors.ToDictionary(pair => pair.Key, pair => pair.Value),
                detail: error.Message,
                extensions: extensions);
        }

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(statusCode: statusCode, detail: error.Message, extensions: extensions);
    }
}
