using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Unit>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).ValidPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("The new password must be different from the current one.");
    }
}

/// <summary>
/// Changes the password and signs the user out everywhere else
/// by revoking all of their refresh tokens.
/// </summary>
public sealed class ChangePasswordHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
    : ICommandHandler<ChangePasswordCommand, Unit>
{
    public async Task<Result<Unit>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken);
        if (user is null) return AuthErrors.UserNotFound;

        if (!passwordHasher.Verify(user.PasswordHash, command.CurrentPassword))
            return AuthErrors.WrongCurrentPassword;

        user.ChangePasswordHash(passwordHasher.Hash(command.NewPassword));

        var now = clock.GetUtcNow().UtcDateTime;
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        activeTokens.ForEach(token => token.Revoke(now));

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
