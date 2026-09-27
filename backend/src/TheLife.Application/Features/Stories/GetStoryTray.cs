using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

public sealed record GetStoryTrayQuery : IQuery<IReadOnlyList<StoryTrayItemDto>>;

/// <summary>
/// The story bar on top of the feed: active stories from me and from people I follow.
/// Order: me first, then people with stories I have not seen yet, then the rest; newest activity first.
/// </summary>
public sealed class GetStoryTrayHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetStoryTrayQuery, IReadOnlyList<StoryTrayItemDto>>
{
    public async Task<Result<IReadOnlyList<StoryTrayItemDto>>> Handle(GetStoryTrayQuery query, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var now = clock.GetUtcNow().UtcDateTime;
        var followedUserIds = db.Follows.Where(f => f.FollowerId == me).Select(f => f.FolloweeId);

        var stories = await db.Stories
            .AsNoTracking()
            .Where(s => s.ExpiresAt > now && (s.AuthorId == me || followedUserIds.Contains(s.AuthorId)))
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                Author = new UserSummaryDto(s.Author.Id, s.Author.Username, s.Author.DisplayName, s.Author.AvatarUrl),
                Story = new StoryDto(s.Id, s.ImageUrl, s.CreatedAt, s.ExpiresAt, s.Views.Any(v => v.ViewerId == me)),
            })
            .ToListAsync(cancellationToken);

        // Group the flat list of stories into one tray item per author (done in memory: the list is small).
        var tray = stories
            .GroupBy(row => row.Author.Id)
            .Select(group =>
            {
                var authorStories = group.Select(row => row.Story).ToList();
                var isMine = group.Key == me;
                return new StoryTrayItemDto(
                    group.First().Author,
                    authorStories,
                    HasUnseen: !isMine && authorStories.Any(story => !story.IsViewedByMe));
            })
            .OrderByDescending(item => item.User.Id == me)
            .ThenByDescending(item => item.HasUnseen)
            .ThenByDescending(item => item.Stories[^1].CreatedAt)
            .ToList();

        return tray;
    }
}
