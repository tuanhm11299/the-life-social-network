using TheLife.Domain.Users;

namespace TheLife.Domain.Stories;

/// <summary>Remembers that a user has seen a story, so the story ring can turn grey.</summary>
public sealed class StoryView
{
    private StoryView() { }

    public Guid StoryId { get; private set; }
    public Story Story { get; private set; } = null!;

    public Guid ViewerId { get; private set; }
    public User Viewer { get; private set; } = null!;

    public DateTime ViewedAt { get; private set; }

    public static StoryView Create(Guid storyId, Guid viewerId, DateTime now) =>
        new() { StoryId = storyId, ViewerId = viewerId, ViewedAt = now };
}
