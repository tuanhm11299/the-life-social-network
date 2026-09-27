using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Notifications;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Follows;

/// <summary>Returned by follow and unfollow so the UI can update the button and the counter.</summary>
public sealed record FollowStatusDto(bool IsFollowedByMe, int FollowerCount);

public sealed record FollowUserCommand(string Username) : ICommand<FollowStatusDto>;

/// <summary>Follows a user. Following someone you already follow does nothing (idempotent).</summary>
public sealed class FollowUserHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<FollowUserCommand, FollowStatusDto>
{
    public async Task<Result<FollowStatusDto>> Handle(FollowUserCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var username = User.NormalizeUsername(command.Username);

        var targetId = await db.Users
            .Where(u => u.Username == username)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (targetId is null) return UserErrors.NotFound;
        if (targetId == me) return UserErrors.CannotFollowSelf;

        var alreadyFollowing = await db.Follows.AnyAsync(
            f => f.FollowerId == me && f.FolloweeId == targetId, cancellationToken);

        if (!alreadyFollowing)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            db.Follows.Add(Follow.Create(me, targetId.Value, now));
            db.Notifications.Add(Notification.ForFollow(targetId.Value, me, now));
            await db.SaveChangesAsync(cancellationToken);
        }

        var followerCount = await db.Follows.CountAsync(f => f.FolloweeId == targetId, cancellationToken);
        return new FollowStatusDto(IsFollowedByMe: true, followerCount);
    }
}
