using TheLife.Application.Common.Abstractions;
using TheLife.Domain.Users;

namespace TheLife.Application.Features.Auth;

/// <summary>The signed-in user's own account details (includes private fields like email).</summary>
public sealed record CurrentUserDto(Guid Id, string Username, string Email, string DisplayName, string Bio, string? AvatarUrl)
{
    public static CurrentUserDto From(User user) =>
        new(user.Id, user.Username, user.Email, user.DisplayName, user.Bio, user.AvatarUrl);
}

/// <summary>
/// Result of a successful register / login / refresh.
/// The API sends the access token in the response body and puts the refresh token in an http-only cookie.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    CurrentUserDto User);

/// <summary>Creates a new pair of tokens for a user. Shared by Register, Login and RefreshSession.</summary>
public sealed class SessionIssuer(IAppDbContext db, ITokenService tokens, TimeProvider clock)
{
    /// <summary>
    /// Issues tokens and stages the new refresh token in the database.
    /// The caller must still call SaveChangesAsync.
    /// </summary>
    public AuthResult Issue(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var accessToken = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateRefreshToken();

        var storedToken = RefreshToken.Create(user.Id, tokens.HashRefreshToken(refreshToken), now, tokens.RefreshTokenLifetime);
        db.RefreshTokens.Add(storedToken);

        return new AuthResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            storedToken.ExpiresAt,
            CurrentUserDto.From(user));
    }
}
