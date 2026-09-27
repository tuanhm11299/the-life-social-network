using TheLife.Domain.Common;

namespace TheLife.Domain.Posts;

/// <summary>One image inside a post. <see cref="Position"/> is the order in the carousel (0 = first).</summary>
public sealed class PostImage : Entity
{
    private PostImage() { }

    internal PostImage(Guid postId, string url, int position)
    {
        PostId = postId;
        Url = url;
        Position = position;
    }

    public Guid PostId { get; private set; }
    public string Url { get; private set; } = null!;
    public int Position { get; private set; }
}
