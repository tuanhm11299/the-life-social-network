using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

public sealed record GetUserStoriesQuery(string Username) : IQuery<StoryTrayItemDto>;

/// <summary>The active stories of one user (opened by tapping their avatar on a profile page).</summary>
public sealed class GetUserStoriesHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetUserStoriesQuery, StoryTrayItemDto>
{
    public async Task<Result<StoryTrayItemDto>> Handle(GetUserStoriesQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var now = clock.GetUtcNow().UtcDateTime;
        var username = User.NormalizeUsername(query.Username);

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Username == username)
            .Select(u => new UserSummaryDto(u.Id, u.Username, u.DisplayName, u.AvatarUrl))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null) return UserErrors.NotFound;

        var stories = await db.Stories
            .AsNoTracking()
            .Where(s => s.AuthorId == user.Id && s.ExpiresAt > now)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new StoryDto(s.Id, s.ImageUrl, s.CreatedAt, s.ExpiresAt, s.Views.Any(v => v.ViewerId == me)))
            .ToListAsync(cancellationToken);

        var hasUnseen = user.Id != me && stories.Any(story => !story.IsViewedByMe);
        return new StoryTrayItemDto(user, stories, hasUnseen);
    }
}
