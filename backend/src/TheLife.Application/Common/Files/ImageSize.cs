namespace TheLife.Application.Common.Files;

/// <summary>
/// The largest size an image is stored at. Bigger images are shrunk to fit (keeping their shape); smaller ones are
/// never enlarged. With <see cref="CropToSquare"/> the middle square is cut out, for square grid cells and avatars.
/// </summary>
public sealed record ImageSize(int MaxWidth, int MaxHeight, bool CropToSquare = false)
{
    /// <summary>Posts and stories: the full width of a phone screen, and sharp in the desktop feed.</summary>
    public static readonly ImageSize Photo = new(1080, 1920);

    /// <summary>Profile, explore and saved grids show small squares, so they don't need the full photo.</summary>
    public static readonly ImageSize Thumbnail = new(320, 320, CropToSquare: true);

    public static readonly ImageSize Avatar = new(320, 320, CropToSquare: true);
}
