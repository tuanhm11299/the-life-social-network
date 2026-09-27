using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using TheLife.Domain.Stories;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Common.Abstractions;

/// <summary>
/// The database, as seen by the feature handlers. Implemented by AppDbContext in Infrastructure.
/// Handlers use LINQ on these sets directly (no repository layer), which keeps each feature short
/// and lets queries project straight into response DTOs.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Follow> Follows { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostImage> PostImages { get; }
    DbSet<Like> Likes { get; }
    DbSet<Comment> Comments { get; }
    DbSet<SavedPost> SavedPosts { get; }
    DbSet<Story> Stories { get; }
    DbSet<StoryView> StoryViews { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
