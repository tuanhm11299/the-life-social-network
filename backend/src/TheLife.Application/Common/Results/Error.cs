namespace TheLife.Application.Common.Results;

/// <summary>The kind of failure. The API layer turns each kind into an HTTP status code.</summary>
public enum ErrorType
{
    Validation,   // 400
    Unauthorized, // 401
    Forbidden,    // 403
    NotFound,     // 404
    Conflict,     // 409
}

/// <summary>
/// Describes why a command or query failed. Handlers return errors instead of throwing exceptions,
/// so the failure cases of a feature are visible in its code.
/// Each feature keeps its errors in a small catalog class, e.g. <c>PostErrors.NotFound</c>.
/// </summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    /// <summary>Field name → messages. Only set for validation errors.</summary>
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }

    public static Error Validation(string code, string message) => new(ErrorType.Validation, code, message);
    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);
    public static Error Forbidden(string code, string message) => new(ErrorType.Forbidden, code, message);
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error FromFieldErrors(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(ErrorType.Validation, "Validation.Failed", "One or more fields are invalid.") { FieldErrors = fieldErrors };
}
