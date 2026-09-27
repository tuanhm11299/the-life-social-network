using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.SavedPosts;

public sealed record GetSavedPostsQuery(string? Cursor, int? PageSize) : IQuery<CursorPage<PostDto>>;

/// <summary>My bookmarked posts, most recently saved first. Only visible to me.</summary>
public sealed class GetSavedPostsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetSavedPostsQuery, CursorPage<PostDto>>
{
    public async Task<Result<CursorPage<PostDto>>> Handle(GetSavedPostsQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var pageSize = Cursor.ClampPageSize(query.PageSize);
        var offset = Cursor.ToOffset(query.Cursor);

        var items = await db.SavedPosts
            .AsNoTracking()
            .Where(s => s.UserId == me)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(pageSize + 1)
            .Select(s => s.Post)
            .Select(PostProjections.ToPostDto(me))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        return new CursorPage<PostDto>(
            items.Take(pageSize).ToList(),
            hasMore ? Cursor.FromOffset(offset + pageSize) : null);
    }
}
