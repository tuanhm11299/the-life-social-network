using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Stories;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

public sealed record MarkStoryViewedCommand(Guid StoryId) : ICommand<Unit>;

/// <summary>Records that I have seen a story. Viewing it again does nothing (idempotent).</summary>
public sealed class MarkStoryViewedHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<MarkStoryViewedCommand, Unit>
{
    public async Task<Result<Unit>> Handle(MarkStoryViewedCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var now = clock.GetUtcNow().UtcDateTime;

        var story = await db.Stories
            .AsNoTracking()
            .Where(s => s.Id == command.StoryId && s.ExpiresAt > now)
            .Select(s => new { s.AuthorId })
            .SingleOrDefaultAsync(cancellationToken);

        if (story is null) return StoryErrors.NotFound;

        // Authors looking at their own story are not counted as viewers.
        if (story.AuthorId == me) return Unit.Value;

        var alreadyViewed = await db.StoryViews.AnyAsync(v => v.StoryId == command.StoryId && v.ViewerId == me, cancellationToken);
        if (!alreadyViewed)
        {
            db.StoryViews.Add(StoryView.Create(command.StoryId, me, now));
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
