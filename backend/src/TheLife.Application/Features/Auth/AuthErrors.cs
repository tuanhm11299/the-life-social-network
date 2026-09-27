using TheLife.Application.Common.Results;

namespace TheLife.Application.Features.Auth;

public static class AuthErrors
{
    public static readonly Error UsernameTaken =
        Error.Conflict("Auth.UsernameTaken", "This username is already taken.");

    public static readonly Error EmailTaken =
        Error.Conflict("Auth.EmailTaken", "An account with this email already exists.");

    // Deliberately vague: do not reveal whether the username or the password was wrong.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Incorrect username or password.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Your session has expired. Please log in again.");

    public static readonly Error WrongCurrentPassword =
        Error.Validation("Auth.WrongCurrentPassword", "Your current password is incorrect.");

    public static readonly Error UserNotFound =
        Error.NotFound("Auth.UserNotFound", "The signed-in user no longer exists.");
}
