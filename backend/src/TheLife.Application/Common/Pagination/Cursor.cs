using System.Globalization;

namespace TheLife.Application.Common.Pagination;

/// <summary>
/// Helpers to turn a position in a list into a cursor string and back.
/// Two kinds are used:
///  * a timestamp ("give me items older than this") for lists sorted by date, and
///  * an offset   ("skip this many items") for lists sorted by something else, like popularity.
/// </summary>
public static class Cursor
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 50;

    public static string FromDate(DateTime value) => value.Ticks.ToString(CultureInfo.InvariantCulture);

    /// <summary>Returns null for a missing or malformed cursor, which simply means "start from the top".</summary>
    public static DateTime? ToDate(string? cursor) =>
        long.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
        && ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? new DateTime(ticks, DateTimeKind.Utc)
            : null;

    public static string FromOffset(int offset) => offset.ToString(CultureInfo.InvariantCulture);

    public static int ToOffset(string? cursor) =>
        int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var offset) ? offset : 0;

    /// <summary>Keeps the page size the client asked for within sensible limits.</summary>
    public static int ClampPageSize(int? pageSize) => Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
}
