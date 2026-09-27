using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Users;

public sealed record GetSuggestedUsersQuery(int? Limit) : IQuery<IReadOnlyList<UserListItemDto>>;

/// <summary>"Suggested for you": popular people the current user does not follow yet.</summary>
public sealed class GetSuggestedUsersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetSuggestedUsersQuery, IReadOnlyList<UserListItemDto>>
{
    public async Task<Result<IReadOnlyList<UserListItemDto>>> Handle(GetSuggestedUsersQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var limit = Math.Clamp(query.Limit ?? 5, 1, 30);

        var users = await db.Users
            .AsNoTracking()
            .Where(u => u.Id != me && !u.Followers.Any(f => f.FollowerId == me))
            .OrderByDescending(u => u.Followers.Count)
            .ThenByDescending(u => u.CreatedAt)
            .Take(limit)
            .Select(UserProjections.ToListItem(me))
            .ToListAsync(cancellationToken);

        return users;
    }
}
