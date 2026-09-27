using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Posts;

public sealed record GetFeedQuery(string? Cursor, int? PageSize) : IQuery<CursorPage<PostDto>>;

/// <summary>The home feed: my posts and posts from people I follow, newest first.</summary>
public sealed class GetFeedHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetFeedQuery, CursorPage<PostDto>>
{
    public async Task<Result<CursorPage<PostDto>>> Handle(GetFeedQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var pageSize = Cursor.ClampPageSize(query.PageSize);

        var followedUserIds = db.Follows.Where(f => f.FollowerId == me).Select(f => f.FolloweeId);

        var posts = db.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == me || followedUserIds.Contains(p.AuthorId));

        var before = Cursor.ToDate(query.Cursor);
        if (before is not null) posts = posts.Where(p => p.CreatedAt < before);

        var items = await posts
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize + 1)
            .Select(PostProjections.ToPostDto(me))
            .ToListAsync(cancellationToken);

        return CursorPage<PostDto>.FromOverfetched(items, pageSize, post => Cursor.FromDate(post.CreatedAt));
    }
}
