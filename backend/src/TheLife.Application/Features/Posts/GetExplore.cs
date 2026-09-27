using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Posts;

public sealed record GetExploreQuery(string? Cursor, int? PageSize) : IQuery<CursorPage<PostDto>>;

/// <summary>
/// The explore page: popular posts from people I do not follow yet, to discover new accounts.
/// Sorted by number of likes, then newest. Because the order is not by date, the cursor is an offset.
/// </summary>
public sealed class GetExploreHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetExploreQuery, CursorPage<PostDto>>
{
    public async Task<Result<CursorPage<PostDto>>> Handle(GetExploreQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var pageSize = Cursor.ClampPageSize(query.PageSize);
        var offset = Cursor.ToOffset(query.Cursor);

        var followedUserIds = db.Follows.Where(f => f.FollowerId == me).Select(f => f.FolloweeId);

        var items = await db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId != me && !followedUserIds.Contains(p.AuthorId))
            .OrderByDescending(p => p.Likes.Count)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(pageSize + 1)
            .Select(PostProjections.ToPostDto(me))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        return new CursorPage<PostDto>(
            items.Take(pageSize).ToList(),
            hasMore ? Cursor.FromOffset(offset + pageSize) : null);
    }
}
