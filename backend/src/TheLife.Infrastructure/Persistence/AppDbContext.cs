using TheLife.Application.Common.Abstractions;
using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using TheLife.Domain.Stories;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for PostgreSQL.
/// Table/column mapping lives in the Configurations folder (one class per entity).
/// Tables and columns use snake_case names (e.g. "post_images.post_id").
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
