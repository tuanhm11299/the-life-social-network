using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Stories;

/// <summary>Housekeeping: removes expired stories and their image files. Run periodically by a background job.</summary>
public sealed record DeleteExpiredStoriesCommand : ICommand<int>;

public sealed class DeleteExpiredStoriesHandler(IAppDbContext db, IFileStorage fileStorage, TimeProvider clock)
    : ICommandHandler<DeleteExpiredStoriesCommand, int>
{
    private const int BatchSize = 500;

    /// <returns>How many stories were deleted.</returns>
    public async Task<Result<int>> Handle(DeleteExpiredStoriesCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var expired = await db.Stories
            .Where(s => s.ExpiresAt <= now)
            .OrderBy(s => s.ExpiresAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0) return 0;

        db.Stories.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var story in expired) await fileStorage.DeleteAsync(story.ImageUrl, cancellationToken);

        return expired.Count;
    }
}
