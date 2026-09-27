using System.Linq.Expressions;
using TheLife.Domain.Users;

namespace TheLife.Application.Features.Users;

/// <summary>The few fields needed to show who wrote something (avatar + username).</summary>
public sealed record UserSummaryDto(Guid Id, string Username, string DisplayName, string? AvatarUrl);

/// <summary>A row in a list of people (search results, followers, likes...) with a Follow button.</summary>
public sealed record UserListItemDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    bool IsFollowedByMe);

/// <summary>Everything shown in a profile header.</summary>
public sealed record ProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Bio,
    string? AvatarUrl,
    int PostCount,
    int FollowerCount,
    int FollowingCount,
    bool IsMe,
    bool IsFollowedByMe,
    bool HasActiveStory,
    DateTime JoinedAt);

public static class UserProjections
{
    /// <summary>
    /// LINQ projection used by every "list of users" query.
    /// Because it is an expression, Entity Framework turns it into SQL (only the needed columns are loaded).
    /// </summary>
    public static Expression<Func<User, UserListItemDto>> ToListItem(Guid viewerId) => user => new UserListItemDto(
        user.Id,
        user.Username,
        user.DisplayName,
        user.AvatarUrl,
        user.Followers.Any(follow => follow.FollowerId == viewerId));
}
