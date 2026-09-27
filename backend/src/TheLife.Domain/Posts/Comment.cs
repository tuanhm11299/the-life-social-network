using TheLife.Domain.Common;
using TheLife.Domain.Users;

namespace TheLife.Domain.Posts;

public sealed class Comment : Entity
{
    public const int TextMaxLength = 1000;

    private Comment() { }

    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = null!;

    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;

    public string Text { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static Comment Create(Guid postId, Guid authorId, string text, DateTime now)
    {
        var trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0) throw new DomainException("Comment cannot be empty.");
        if (trimmed.Length > TextMaxLength)
            throw new DomainException($"Comment cannot be longer than {TextMaxLength} characters.");

        return new Comment { PostId = postId, AuthorId = authorId, Text = trimmed, CreatedAt = now };
    }

    /// <summary>The comment author and the post author may both delete a comment.</summary>
    public bool CanBeDeletedBy(Guid userId, Guid postAuthorId) => userId == AuthorId || userId == postAuthorId;
}
