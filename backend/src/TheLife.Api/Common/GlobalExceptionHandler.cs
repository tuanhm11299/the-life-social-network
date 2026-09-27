using TheLife.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TheLife.Api.Common;

/// <summary>
/// Last-resort handler for exceptions nobody caught. Expected failures are returned as Result errors by the
/// handlers; this only deals with the unexpected ones and makes sure the client still gets problem details.
/// </summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Please log in to continue."),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request is invalid or too large."),

            // Two identical requests at the same moment (e.g. a double-click on "like") can both pass the
            // "already exists?" check; the database's unique key then rejects the second one.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (StatusCodes.Status409Conflict, "This was already done."),

            _ => (StatusCodes.Status500InternalServerError, "Something went wrong on our side. Please try again."),
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = statusCode, Detail = detail },
        });
    }
}
