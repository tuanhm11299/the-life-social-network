using TheLife.Application.Common.Files;
using TheLife.Domain.Users;

namespace TheLife.Application.Common.Abstractions;

// Interfaces ("ports") for things the Application layer needs but does not implement itself.
// Implementations live in the Infrastructure and Api projects and are wired up with dependency injection.

/// <summary>The signed-in user making the current request.</summary>
public interface ICurrentUser
{
    /// <summary>Id of the signed-in user. Throws UnauthorizedAccessException when nobody is signed in.</summary>
    Guid Id { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    /// <summary>Creates a short-lived signed JWT for the user.</summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>Creates a random, unguessable refresh token (the plain value is only ever sent to the client).</summary>
    string CreateRefreshToken();

    /// <summary>Hashes a refresh token for storage and lookup.</summary>
    string HashRefreshToken(string refreshToken);

    TimeSpan RefreshTokenLifetime { get; }
}

public interface IFileStorage
{
    /// <summary>
    /// Stores a cleaned copy of an image (no metadata such as GPS location, at most <paramref name="size"/>)
    /// and returns the public URL to show it, e.g. "/uploads/posts/abc.jpg".
    /// </summary>
    Task<string> SaveImageAsync(FileUpload file, string folder, ImageSize size, CancellationToken cancellationToken);

    /// <summary>Deletes a stored file. URLs not owned by this storage (e.g. external demo images) are ignored.</summary>
    Task DeleteAsync(string url, CancellationToken cancellationToken);
}
