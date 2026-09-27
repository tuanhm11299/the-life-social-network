using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Auth;

public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<AuthResult>;

/// <summary>
/// Exchanges a valid refresh token for a new access token AND a new refresh token ("rotation").
/// The old refresh token stops working shortly after (see RefreshToken.RotationGracePeriod),
/// so a stolen, already-used token is soon worthless.
/// </summary>
public sealed class RefreshSessionHandler(
    IAppDbContext db,
    ITokenService tokens,
    SessionIssuer sessionIssuer,
    TimeProvider clock)
    : ICommandHandler<RefreshSessionCommand, AuthResult>
{
    public async Task<Result<AuthResult>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return AuthErrors.InvalidRefreshToken;

        var now = clock.GetUtcNow().UtcDateTime;
        var tokenHash = tokens.HashRefreshToken(command.RefreshToken);

        var storedToken = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        storedToken.MarkReplaced(now);
        var session = sessionIssuer.Issue(storedToken.User);
        await db.SaveChangesAsync(cancellationToken);

        return session;
    }
}
