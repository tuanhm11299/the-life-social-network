using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Likes;

public sealed record UnlikePostCommand(Guid PostId) : ICommand<LikeStatusDto>;

/// <summary>Removes my like from a post. Unliking a post I did not like does nothing (idempotent).</summary>
public sealed class UnlikePostHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UnlikePostCommand, LikeStatusDto>
{
    public async Task<Result<LikeStatusDto>> Handle(UnlikePostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        if (!await db.Posts.AnyAsync(p => p.Id == command.PostId, cancellationToken))
            return PostErrors.NotFound;

        var like = await db.Likes.SingleOrDefaultAsync(l => l.PostId == command.PostId && l.UserId == me, cancellationToken);
        if (like is not null)
        {
            db.Likes.Remove(like);
            await db.SaveChangesAsync(cancellationToken);
        }

        var likeCount = await db.Likes.CountAsync(l => l.PostId == command.PostId, cancellationToken);
        return new LikeStatusDto(IsLikedByMe: false, likeCount);
    }
}
