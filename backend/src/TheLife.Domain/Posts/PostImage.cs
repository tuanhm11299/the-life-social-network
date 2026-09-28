using TheLife.Domain.Common;

namespace TheLife.Domain.Posts;

/// <summary>The stored files for one new image: the full photo and a small square thumbnail for grids.</summary>
public sealed record NewPostImage(string Url, string ThumbnailUrl);

/// <summary>One image inside a post. <see cref="Position"/> is the order in the carousel (0 = first).</summary>
public sealed class PostImage : Entity
{
    private PostImage() { }

    internal PostImage(Guid postId, NewPostImage image, int position)
    {
        PostId = postId;
        Url = image.Url;
        ThumbnailUrl = image.ThumbnailUrl;
        Position = position;
    }

    public Guid PostId { get; private set; }
    public string Url { get; private set; } = null!;
    public string ThumbnailUrl { get; private set; } = null!;
    public int Position { get; private set; }
}
