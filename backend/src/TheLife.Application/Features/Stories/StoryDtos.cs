using TheLife.Application.Common.Results;
using TheLife.Application.Features.Users;

namespace TheLife.Application.Features.Stories;

public sealed record StoryDto(Guid Id, string ImageUrl, DateTime CreatedAt, DateTime ExpiresAt, bool IsViewedByMe);

/// <summary>
/// One circle in the story bar at the top of the feed: a user and their active stories (oldest first).
/// <see cref="HasUnseen"/> decides whether the ring around the avatar is colored or grey.
/// </summary>
public sealed record StoryTrayItemDto(UserSummaryDto User, IReadOnlyList<StoryDto> Stories, bool HasUnseen);

public static class StoryErrors
{
    public static readonly Error NotFound = Error.NotFound("Story.NotFound", "This story does not exist or has expired.");

    public static readonly Error NotAuthor = Error.Forbidden("Story.NotAuthor", "Only the author can do this.");
}
