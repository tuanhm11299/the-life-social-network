using TheLife.Domain.Common;
using TheLife.Domain.Posts;
using TheLife.Domain.Users;

namespace TheLife.Domain.Notifications;

public enum NotificationType
{
    Like = 1,
    Comment = 2,
    Follow = 3,
}

/// <summary>
/// "Actor did something that concerns Recipient", e.g. "jane liked your post".
/// Use the factory methods (<see cref="ForLike"/> etc.) so every type gets the right data.
/// </summary>
public sealed class Notification : Entity
{
    private Notification() { }

    public Guid RecipientId { get; private set; }
    public User Recipient { get; private set; } = null!;

    public Guid ActorId { get; private set; }
    public User Actor { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    /// <summary>Set for Like and Comment notifications.</summary>
    public Guid? PostId { get; private set; }
    public Post? Post { get; private set; }

    /// <summary>Short preview of the comment text, only for Comment notifications.</summary>
    public string? CommentText { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    public static Notification ForLike(Guid recipientId, Guid actorId, Guid postId, DateTime now) =>
        Create(recipientId, actorId, NotificationType.Like, postId, commentText: null, now);

    public static Notification ForComment(Guid recipientId, Guid actorId, Guid postId, string commentText, DateTime now) =>
        Create(recipientId, actorId, NotificationType.Comment, postId, Truncate(commentText, 100), now);

    public static Notification ForFollow(Guid recipientId, Guid actorId, DateTime now) =>
        Create(recipientId, actorId, NotificationType.Follow, postId: null, commentText: null, now);

    public void MarkAsRead() => IsRead = true;

    private static Notification Create(
        Guid recipientId, Guid actorId, NotificationType type, Guid? postId, string? commentText, DateTime now)
    {
        if (recipientId == actorId) throw new DomainException("Users are not notified about their own actions.");

        return new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId,
            CommentText = commentText,
            CreatedAt = now,
        };
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
