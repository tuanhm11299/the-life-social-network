using TheLife.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace TheLife.Infrastructure.Authentication;

/// <summary>
/// Uses ASP.NET Core Identity's battle-tested hasher (PBKDF2 with a random salt per password).
/// We only borrow the hasher; the rest of ASP.NET Core Identity is not used.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    // Identity's hasher wants a "user" object, but its default implementation never looks at it.
    private static readonly object AnyUser = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(AnyUser, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(AnyUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
