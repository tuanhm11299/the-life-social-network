using System.ComponentModel.DataAnnotations;

namespace TheLife.Infrastructure.Authentication;

/// <summary>Settings from the "Jwt" section of appsettings.json.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = "";
    [Required] public string Audience { get; init; } = "";

    /// <summary>Secret used to sign tokens. At least 32 characters. Never commit a real one.</summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = "";

    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 365)] public int RefreshTokenDays { get; init; } = 30;
}
