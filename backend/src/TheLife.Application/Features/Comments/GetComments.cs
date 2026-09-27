using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Comments;

public sealed record GetCommentsQuery(Guid PostId, string? Cursor, int? PageSize) : IQuery<CursorPage<CommentDto>>;

/// <summary>Comments of a post, oldest first, so the conversation reads top to bottom.</summary>
public sealed class GetCommentsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetCommentsQuery, CursorPage<CommentDto>>
{
    public async Task<Result<CursorPage<CommentDto>>> Handle(GetCommentsQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Posts.AnyAsync(p => p.Id == query.PostId, cancellationToken))
            return PostErrors.NotFound;

        var pageSize = Cursor.ClampPageSize(query.PageSize);
        var comments = db.Comments.AsNoTracking().Where(c => c.PostId == query.PostId);

        // Oldest first, so the cursor means "comments written after this moment".
        var after = Cursor.ToDate(query.Cursor);
        if (after is not null) comments = comments.Where(c => c.CreatedAt > after);

        var items = await comments
            .OrderBy(c => c.CreatedAt)
            .Take(pageSize + 1)
            .Select(CommentProjections.ToCommentDto(currentUser.Id))
            .ToListAsync(cancellationToken);

        return CursorPage<CommentDto>.FromOverfetched(items, pageSize, comment => Cursor.FromDate(comment.CreatedAt));
    }
}
