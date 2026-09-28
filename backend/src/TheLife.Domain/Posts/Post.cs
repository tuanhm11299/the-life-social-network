using TheLife.Domain.Common;
using TheLife.Domain.Users;

namespace TheLife.Domain.Posts;

/// <summary>A photo post. It has 1 to 10 images (shown as a swipeable carousel) and an optional caption.</summary>
public sealed class Post : Entity
{
    public const int MaxImages = 10;
    public const int CaptionMaxLength = 2200;

    private Post() { }

    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;

    public string Caption { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    public List<PostImage> Images { get; private set; } = [];
    public ICollection<Like> Likes { get; private set; } = [];
    public ICollection<Comment> Comments { get; private set; } = [];
    public ICollection<SavedPost> Saves { get; private set; } = [];

    public static Post Create(Guid authorId, string? caption, IReadOnlyList<NewPostImage> images, DateTime now)
    {
        if (images.Count == 0) throw new DomainException("A post needs at least one image.");
        if (images.Count > MaxImages) throw new DomainException($"A post can have at most {MaxImages} images.");

        var post = new Post { AuthorId = authorId, CreatedAt = now };
        post.EditCaption(caption);

        for (var position = 0; position < images.Count; position++)
        {
            post.Images.Add(new PostImage(post.Id, images[position], position));
        }

        return post;
    }

    public void EditCaption(string? caption)
    {
        var trimmed = caption?.Trim() ?? "";
        if (trimmed.Length > CaptionMaxLength)
            throw new DomainException($"Caption cannot be longer than {CaptionMaxLength} characters.");

        Caption = trimmed;
    }

    public bool IsAuthoredBy(Guid userId) => AuthorId == userId;
}
