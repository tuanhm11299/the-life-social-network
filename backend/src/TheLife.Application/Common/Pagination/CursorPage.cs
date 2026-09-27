namespace TheLife.Application.Common.Pagination;

/// <summary>
/// One page of a list that is loaded with infinite scrolling.
/// To load the next page, send <see cref="NextCursor"/> back as the <c>cursor</c> query parameter.
/// When <see cref="NextCursor"/> is null there are no more items.
/// The cursor is an opaque string: clients must not try to read or build it.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    /// <summary>
    /// Builds a page from a list that was loaded with <c>Take(pageSize + 1)</c>.
    /// The extra item only tells us that another page exists; it is not returned.
    /// </summary>
    public static CursorPage<T> FromOverfetched(List<T> items, int pageSize, Func<T, string> cursorOf)
    {
        if (items.Count <= pageSize) return new CursorPage<T>(items, NextCursor: null);

        var page = items.Take(pageSize).ToList();
        return new CursorPage<T>(page, cursorOf(page[^1]));
    }
}
