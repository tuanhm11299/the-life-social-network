using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace TheLife.Tests.Api;

/// <summary>
/// Starts the real API in memory against a throw-away PostgreSQL database in Docker (Testcontainers).
/// One database is shared by all API tests; every test creates its own users with unique names,
/// so tests don't interfere with each other.
/// Requires Docker to be running.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly string _uploadsFolder = Path.Combine(Path.GetTempPath(), "thelife-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync() => await _database.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        if (Directory.Exists(_uploadsFolder)) Directory.Delete(_uploadsFolder, recursive: true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", "false");
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-long-enough-0123456789");
        builder.UseSetting("Storage:RootPath", _uploadsFolder);
        builder.UseSetting("RateLimiting:AuthRequestsPerMinute", "10000");
    }

    /// <summary>Registers a brand-new user and returns an HttpClient that is signed in as them.</summary>
    public async Task<TestUser> CreateUserAsync(string? usernamePrefix = null)
    {
        var username = $"{usernamePrefix ?? "user"}_{Guid.NewGuid():N}"[..20];
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { username, email = $"{username}@test.local", password = TestUser.Password, displayName = username });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return new TestUser(auth.User.Id, username, client);
    }
}

public sealed record TestUser(Guid Id, string Username, HttpClient Client)
{
    public const string Password = "Password123!";
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "API";
}

// Minimal copies of the API's JSON responses, just the fields the tests look at.
public sealed record AuthResponseDto(string AccessToken, UserDto User);
public sealed record UserDto(Guid Id, string Username);
public sealed record PostDto(Guid Id, UserDto Author, string Caption, List<string> ImageUrls, int LikeCount, int CommentCount, bool IsLikedByMe, bool IsSavedByMe);
public sealed record PageDto<T>(List<T> Items, string? NextCursor);
public sealed record LikeStatusDto(bool IsLikedByMe, int LikeCount);
public sealed record FollowStatusDto(bool IsFollowedByMe, int FollowerCount);
public sealed record CommentDto(Guid Id, string Text, bool CanDelete);
public sealed record ProfileDto(string Username, int PostCount, int FollowerCount, int FollowingCount, bool IsMe, bool IsFollowedByMe);
public sealed record NotificationDto(string Type, UserDto Actor, Guid? PostId);
public sealed record StoryDto(Guid Id, string ImageUrl, bool IsViewedByMe);
public sealed record StoryTrayItemDto(UserDto User, List<StoryDto> Stories, bool HasUnseen);
public sealed record ProblemDto(int Status, string? Detail, string? Code, Dictionary<string, string[]>? Errors);
