using TheLife.Domain.Users;

namespace TheLife.Domain.Posts;

/// <summary>A post bookmarked by a user (the "Save" button). Only the user who saved it can see this.</summary>
public sealed class SavedPost
{
    private SavedPost() { }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public static SavedPost Create(Guid userId, Guid postId, DateTime now) =>
        new() { UserId = userId, PostId = postId, CreatedAt = now };
}
