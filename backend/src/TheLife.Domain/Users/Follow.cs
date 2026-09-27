using TheLife.Domain.Common;

namespace TheLife.Domain.Users;

/// <summary>
/// "Follower follows Followee". The pair (FollowerId, FolloweeId) is the primary key,
/// so the same follow can never be stored twice.
/// </summary>
public sealed class Follow
{
    private Follow() { }

    public Guid FollowerId { get; private set; }
    public User Follower { get; private set; } = null!;

    public Guid FolloweeId { get; private set; }
    public User Followee { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public static Follow Create(Guid followerId, Guid followeeId, DateTime now)
    {
        if (followerId == followeeId) throw new DomainException("You cannot follow yourself.");

        return new Follow { FollowerId = followerId, FolloweeId = followeeId, CreatedAt = now };
    }
}
