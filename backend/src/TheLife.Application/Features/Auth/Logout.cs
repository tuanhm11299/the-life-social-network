using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Auth;

public sealed record LogoutCommand(string? RefreshToken) : ICommand<Unit>;

/// <summary>Revokes the refresh token so it cannot be used again. Always succeeds.</summary>
public sealed class LogoutHandler(IAppDbContext db, ITokenService tokens, TimeProvider clock)
    : ICommandHandler<LogoutCommand, Unit>
{
    public async Task<Result<Unit>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken)) return Unit.Value;

        var tokenHash = tokens.HashRefreshToken(command.RefreshToken);
        var storedToken = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is not null)
        {
            storedToken.Revoke(clock.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
