using TheLife.Application.Common.Results;

namespace TheLife.Application.Features.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "This user does not exist.");

    public static readonly Error CannotFollowSelf =
        Error.Validation("User.CannotFollowSelf", "You cannot follow yourself.");
}
