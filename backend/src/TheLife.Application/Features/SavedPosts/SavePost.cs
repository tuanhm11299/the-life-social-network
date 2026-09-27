using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using TheLife.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.SavedPosts;

public sealed record SaveStatusDto(bool IsSavedByMe);

public sealed record SavePostCommand(Guid PostId) : ICommand<SaveStatusDto>;

/// <summary>Bookmarks a post. Saving twice does nothing (idempotent).</summary>
public sealed class SavePostHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<SavePostCommand, SaveStatusDto>
{
    public async Task<Result<SaveStatusDto>> Handle(SavePostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        if (!await db.Posts.AnyAsync(p => p.Id == command.PostId, cancellationToken))
            return PostErrors.NotFound;

        var alreadySaved = await db.SavedPosts.AnyAsync(s => s.PostId == command.PostId && s.UserId == me, cancellationToken);
        if (!alreadySaved)
        {
            db.SavedPosts.Add(SavedPost.Create(me, command.PostId, clock.GetUtcNow().UtcDateTime));
            await db.SaveChangesAsync(cancellationToken);
        }

        return new SaveStatusDto(IsSavedByMe: true);
    }
}
