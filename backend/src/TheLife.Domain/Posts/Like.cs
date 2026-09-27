using TheLife.Domain.Users;

namespace TheLife.Domain.Posts;

/// <summary>A user liking a post. (PostId, UserId) is the primary key, so a user can like a post only once.</summary>
public sealed class Like
{
    private Like() { }

    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public static Like Create(Guid postId, Guid userId, DateTime now) =>
        new() { PostId = postId, UserId = userId, CreatedAt = now };
}
