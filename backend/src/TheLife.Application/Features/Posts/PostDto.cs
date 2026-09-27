using System.Linq.Expressions;
using TheLife.Application.Features.Users;
using TheLife.Domain.Posts;

namespace TheLife.Application.Features.Posts;

/// <summary>A post as shown in the feed, on the explore page, in a profile grid and on the post page.</summary>
public sealed record PostDto(
    Guid Id,
    UserSummaryDto Author,
    string Caption,
    IReadOnlyList<string> ImageUrls,
    DateTime CreatedAt,
    int LikeCount,
    int CommentCount,
    bool IsLikedByMe,
    bool IsSavedByMe);

public static class PostProjections
{
    /// <summary>
    /// Turns a Post into a PostDto inside the database query, from the point of view of <paramref name="viewerId"/>
    /// (so "IsLikedByMe" and "IsSavedByMe" are correct for the person looking at it).
    /// Every post query uses this, so all screens show posts the same way.
    /// </summary>
    public static Expression<Func<Post, PostDto>> ToPostDto(Guid viewerId) => post => new PostDto(
        post.Id,
        new UserSummaryDto(post.Author.Id, post.Author.Username, post.Author.DisplayName, post.Author.AvatarUrl),
        post.Caption,
        post.Images.OrderBy(image => image.Position).Select(image => image.Url).ToList(),
        post.CreatedAt,
        post.Likes.Count,
        post.Comments.Count,
        post.Likes.Any(like => like.UserId == viewerId),
        post.Saves.Any(save => save.UserId == viewerId));
}
