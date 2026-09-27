namespace TheLife.Domain.Common;

/// <summary>
/// Base class for every entity that has its own identity.
/// Ids are version 7 GUIDs: they are unique like normal GUIDs but also sort by creation time,
/// which keeps database indexes compact.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
