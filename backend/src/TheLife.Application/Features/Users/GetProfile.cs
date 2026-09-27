using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Users;

public sealed record GetProfileQuery(string Username) : IQuery<ProfileDto>;

public sealed class GetProfileHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetProfileQuery, ProfileDto>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var viewerId = currentUser.Id;
        var username = User.NormalizeUsername(query.Username);
        var now = clock.GetUtcNow().UtcDateTime;

        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Username == username)
            .Select(u => new ProfileDto(
                u.Id,
                u.Username,
                u.DisplayName,
                u.Bio,
                u.AvatarUrl,
                u.Posts.Count,
                u.Followers.Count,
                u.Following.Count,
                u.Id == viewerId,
                u.Followers.Any(f => f.FollowerId == viewerId),
                u.Stories.Any(s => s.ExpiresAt > now),
                u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return profile is null ? UserErrors.NotFound : profile;
    }
}
