using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;
using TheLife.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Follows;

public sealed record UnfollowUserCommand(string Username) : ICommand<FollowStatusDto>;

/// <summary>Stops following a user. Unfollowing someone you do not follow does nothing (idempotent).</summary>
public sealed class UnfollowUserHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UnfollowUserCommand, FollowStatusDto>
{
    public async Task<Result<FollowStatusDto>> Handle(UnfollowUserCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var username = User.NormalizeUsername(command.Username);

        var targetId = await db.Users
            .Where(u => u.Username == username)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (targetId is null) return UserErrors.NotFound;

        var follow = await db.Follows.SingleOrDefaultAsync(
            f => f.FollowerId == me && f.FolloweeId == targetId, cancellationToken);

        if (follow is not null)
        {
            db.Follows.Remove(follow);
            await db.SaveChangesAsync(cancellationToken);
        }

        var followerCount = await db.Follows.CountAsync(f => f.FolloweeId == targetId, cancellationToken);
        return new FollowStatusDto(IsFollowedByMe: false, followerCount);
    }
}
