using TheLife.Domain.Common;
using TheLife.Domain.Posts;
using TheLife.Domain.Stories;

namespace TheLife.Domain.Users;

public sealed class User : Entity
{
    public const int UsernameMaxLength = 30;
    public const int DisplayNameMaxLength = 50;
    public const int BioMaxLength = 150;
    public const int EmailMaxLength = 256;

    // Private constructor for Entity Framework.
    private User() { }

    /// <summary>Unique, lower-case handle, e.g. "jane.doe". Used in profile URLs.</summary>
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string Bio { get; private set; } = "";
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<Post> Posts { get; private set; } = [];
    public ICollection<Story> Stories { get; private set; } = [];

    /// <summary>Rows where somebody else follows this user.</summary>
    public ICollection<Follow> Followers { get; private set; } = [];

    /// <summary>Rows where this user follows somebody else.</summary>
    public ICollection<Follow> Following { get; private set; } = [];

    public static User Create(string username, string email, string displayName, string passwordHash, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        return new User
        {
            Username = NormalizeUsername(username),
            Email = NormalizeEmail(email),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = now,
        };
    }

    public void UpdateProfile(string displayName, string? bio)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("Display name is required.");

        DisplayName = displayName.Trim();
        Bio = bio?.Trim() ?? "";
    }

    public void ChangeAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = passwordHash;
    }

    /// <summary>Usernames are stored lower-case so "Jane" and "jane" are the same account.</summary>
    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
