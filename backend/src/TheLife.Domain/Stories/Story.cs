using TheLife.Domain.Common;
using TheLife.Domain.Users;

namespace TheLife.Domain.Stories;

/// <summary>A single full-screen image that disappears 24 hours after it is posted.</summary>
public sealed class Story : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private Story() { }

    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;

    public string ImageUrl { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public ICollection<StoryView> Views { get; private set; } = [];

    public static Story Create(Guid authorId, string imageUrl, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) throw new DomainException("A story needs an image.");

        return new Story
        {
            AuthorId = authorId,
            ImageUrl = imageUrl,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        };
    }

    public bool IsActive(DateTime now) => ExpiresAt > now;
}
