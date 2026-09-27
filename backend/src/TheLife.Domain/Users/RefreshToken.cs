using TheLife.Domain.Common;

namespace TheLife.Domain.Users;

/// <summary>
/// A long-lived token that lets the browser get a new short-lived access token without
/// asking for the password again. Only a hash of the token is stored, so a leaked database
/// does not leak usable tokens.
/// </summary>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// After a token is exchanged for a new one ("rotated") it keeps working for a few seconds.
    /// Without this, opening several tabs at once logs the user out: every tab sends the same
    /// cookie, the first tab rotates it, and all the others would be rejected.
    /// </summary>
    public static readonly TimeSpan RotationGracePeriod = TimeSpan.FromSeconds(30);

    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Set when the token was exchanged for a newer one. Still usable during the grace period.</summary>
    public DateTime? ReplacedAt { get; private set; }

    /// <summary>Set on logout or password change. A revoked token never works again.</summary>
    public DateTime? RevokedAt { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime now, TimeSpan lifetime) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now.Add(lifetime),
    };

    public bool IsActive(DateTime now) =>
        RevokedAt is null
        && ExpiresAt > now
        && (ReplacedAt is null || now - ReplacedAt < RotationGracePeriod);

    public void MarkReplaced(DateTime now) => ReplacedAt ??= now;

    public void Revoke(DateTime now) => RevokedAt ??= now;
}
