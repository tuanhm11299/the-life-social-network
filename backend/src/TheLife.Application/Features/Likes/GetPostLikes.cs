using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using TheLife.Application.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Likes;

public sealed record GetPostLikesQuery(Guid PostId) : IQuery<IReadOnlyList<UserListItemDto>>;

/// <summary>People who liked a post (most recent first), for the "Liked by ..." dialog.</summary>
public sealed class GetPostLikesHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetPostLikesQuery, IReadOnlyList<UserListItemDto>>
{
    private const int MaxResults = 100;

    public async Task<Result<IReadOnlyList<UserListItemDto>>> Handle(GetPostLikesQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Posts.AnyAsync(p => p.Id == query.PostId, cancellationToken))
            return PostErrors.NotFound;

        var users = await db.Likes
            .AsNoTracking()
            .Where(l => l.PostId == query.PostId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(MaxResults)
            .Select(l => l.User)
            .Select(UserProjections.ToListItem(currentUser.Id))
            .ToListAsync(cancellationToken);

        return users;
    }
}
