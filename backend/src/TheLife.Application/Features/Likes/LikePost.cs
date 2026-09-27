using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Posts;
using TheLife.Domain.Notifications;
using TheLife.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Likes;

/// <summary>Returned by like and unlike so the UI can update the heart and the counter.</summary>
public sealed record LikeStatusDto(bool IsLikedByMe, int LikeCount);

public sealed record LikePostCommand(Guid PostId) : ICommand<LikeStatusDto>;

/// <summary>Likes a post and notifies its author. Liking twice does nothing (idempotent).</summary>
public sealed class LikePostHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<LikePostCommand, LikeStatusDto>
{
    public async Task<Result<LikeStatusDto>> Handle(LikePostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;

        var authorId = await db.Posts
            .Where(p => p.Id == command.PostId)
            .Select(p => (Guid?)p.AuthorId)
            .SingleOrDefaultAsync(cancellationToken);

        if (authorId is null) return PostErrors.NotFound;

        var alreadyLiked = await db.Likes.AnyAsync(l => l.PostId == command.PostId && l.UserId == me, cancellationToken);
        if (!alreadyLiked)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            db.Likes.Add(Like.Create(command.PostId, me, now));

            if (authorId != me)
                db.Notifications.Add(Notification.ForLike(authorId.Value, me, command.PostId, now));

            await db.SaveChangesAsync(cancellationToken);
        }

        var likeCount = await db.Likes.CountAsync(l => l.PostId == command.PostId, cancellationToken);
        return new LikeStatusDto(IsLikedByMe: true, likeCount);
    }
}
