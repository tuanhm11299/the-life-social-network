using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Auth;

/// <param name="Login">Username or email address.</param>
public sealed record LoginCommand(string Login, string Password) : ICommand<AuthResult>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Login).NotEmpty().WithMessage("Enter your username or email.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter your password.");
    }
}

public sealed class LoginHandler(IAppDbContext db, IPasswordHasher passwordHasher, SessionIssuer sessionIssuer)
    : ICommandHandler<LoginCommand, AuthResult>
{
    public async Task<Result<AuthResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Usernames and emails are stored normalized (trimmed + lower-case), so normalize the input the same way.
        var login = User.NormalizeUsername(command.Login);

        var user = await db.Users.SingleOrDefaultAsync(
            u => u.Username == login || u.Email == login, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, command.Password))
            return AuthErrors.InvalidCredentials;

        var session = sessionIssuer.Issue(user);
        await db.SaveChangesAsync(cancellationToken);

        return session;
    }
}
