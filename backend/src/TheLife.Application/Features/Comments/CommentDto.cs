using System.Linq.Expressions;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Posts;

namespace TheLife.Application.Features.Comments;

/// <param name="CanDelete">True when the viewer wrote the comment or owns the post.</param>
public sealed record CommentDto(
    Guid Id,
    Guid PostId,
    UserSummaryDto Author,
    string Text,
    DateTime CreatedAt,
    bool CanDelete);

public static class CommentProjections
{
    public static Expression<Func<Comment, CommentDto>> ToCommentDto(Guid viewerId) => comment => new CommentDto(
        comment.Id,
        comment.PostId,
        new UserSummaryDto(comment.Author.Id, comment.Author.Username, comment.Author.DisplayName, comment.Author.AvatarUrl),
        comment.Text,
        comment.CreatedAt,
        comment.AuthorId == viewerId || comment.Post.AuthorId == viewerId);
}

public static class CommentErrors
{
    public static readonly Error NotFound = Error.NotFound("Comment.NotFound", "This comment does not exist.");

    public static readonly Error CannotDelete =
        Error.Forbidden("Comment.CannotDelete", "You can only delete your own comments or comments on your posts.");
}
