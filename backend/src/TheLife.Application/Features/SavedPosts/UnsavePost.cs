using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.SavedPosts;

public sealed record UnsavePostCommand(Guid PostId) : ICommand<SaveStatusDto>;

/// <summary>Removes a bookmark. Works even if the post was never saved (idempotent).</summary>
public sealed class UnsavePostHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UnsavePostCommand, SaveStatusDto>
{
    public async Task<Result<SaveStatusDto>> Handle(UnsavePostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        var saved = await db.SavedPosts.SingleOrDefaultAsync(s => s.PostId == command.PostId && s.UserId == me, cancellationToken);
        if (saved is not null)
        {
            db.SavedPosts.Remove(saved);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new SaveStatusDto(IsSavedByMe: false);
    }
}
