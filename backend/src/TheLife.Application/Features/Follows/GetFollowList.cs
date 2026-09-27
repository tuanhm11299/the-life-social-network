using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Pagination;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Follows;

public enum FollowListKind
{
    /// <summary>People who follow the user.</summary>
    Followers,

    /// <summary>People the user follows.</summary>
    Following,
}

public sealed record GetFollowListQuery(string Username, FollowListKind Kind, string? Cursor, int? PageSize)
    : IQuery<CursorPage<UserListItemDto>>;

/// <summary>Lists a user's followers or the people they follow, most recent first.</summary>
public sealed class GetFollowListHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetFollowListQuery, CursorPage<UserListItemDto>>
{
    public async Task<Result<CursorPage<UserListItemDto>>> Handle(GetFollowListQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var pageSize = Cursor.ClampPageSize(query.PageSize);
        var username = User.NormalizeUsername(query.Username);

        var userId = await db.Users
            .Where(u => u.Username == username)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (userId is null) return UserErrors.NotFound;

        // For "followers" we show the follower; for "following" we show the followee.
        var follows = query.Kind == FollowListKind.Followers
            ? db.Follows.Where(f => f.FolloweeId == userId).Select(f => new { f.CreatedAt, Person = f.Follower })
            : db.Follows.Where(f => f.FollowerId == userId).Select(f => new { f.CreatedAt, Person = f.Followee });

        var before = Cursor.ToDate(query.Cursor);
        if (before is not null) follows = follows.Where(row => row.CreatedAt < before);

        var rows = await follows
            .AsNoTracking()
            .OrderByDescending(row => row.CreatedAt)
            .Take(pageSize + 1)
            .Select(row => new FollowRow(
                row.CreatedAt,
                new UserListItemDto(
                    row.Person.Id,
                    row.Person.Username,
                    row.Person.DisplayName,
                    row.Person.AvatarUrl,
                    row.Person.Followers.Any(x => x.FollowerId == me))))
            .ToListAsync(cancellationToken);

        var page = CursorPage<FollowRow>.FromOverfetched(rows, pageSize, row => Cursor.FromDate(row.FollowedAt));
        return new CursorPage<UserListItemDto>(page.Items.Select(row => row.User).ToList(), page.NextCursor);
    }

    private sealed record FollowRow(DateTime FollowedAt, UserListItemDto User);
}
