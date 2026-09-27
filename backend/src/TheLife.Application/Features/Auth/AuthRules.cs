using FluentValidation;
using TheLife.Domain.Users;

namespace TheLife.Application.Features.Auth;

/// <summary>Validation rules shared by registration and password change.</summary>
public static class AuthRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 100;

    /// <summary>3–30 characters: letters, numbers, dots and underscores (like Instagram).</summary>
    public static IRuleBuilderOptions<T, string> ValidUsername<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, User.UsernameMaxLength).WithMessage($"Username must be 3 to {User.UsernameMaxLength} characters.")
            .Matches("^[a-zA-Z0-9._]+$").WithMessage("Username can only contain letters, numbers, dots and underscores.");

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Password is required.")
            .Length(PasswordMinLength, PasswordMaxLength)
                .WithMessage($"Password must be {PasswordMinLength} to {PasswordMaxLength} characters.")
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
}
