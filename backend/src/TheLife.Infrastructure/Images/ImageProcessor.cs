using TheLife.Application.Common.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace TheLife.Infrastructure.Images;

/// <summary>An image that is ready to store: re-encoded, and with the file extension that matches its format.</summary>
public sealed record ProcessedImage(Stream Content, string Extension);

/// <summary>
/// Cleans uploaded images before they are stored. Photos taken on phones carry metadata (EXIF, XMP, IPTC)
/// such as the GPS location where they were taken, the camera and the time, so we decode the image and
/// write a fresh file without it, shrunk to the size it will be shown at.
/// Every storage implementation should call this before saving.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Refuse to decode images with more pixels than this (all frames together). A small, highly compressed
    /// file can decode to gigabytes of memory ("decompression bomb"). 60 megapixels still fits a 48 MP phone photo.
    /// </summary>
    public const long MaxPixels = 60_000_000;

    private const int JpegQuality = 85;

    // Only the formats ImageRules allows. ImageSharp could also decode BMP, TIFF and others,
    // but every extra decoder is more code an attacker could aim a malicious file at.
    private static readonly DecoderOptions Options = new() { Configuration = CreateConfiguration() };

    private static Configuration CreateConfiguration()
    {
        var configuration = new Configuration(
            new JpegConfigurationModule(),
            new PngConfigurationModule(),
            new WebpConfigurationModule(),
            new GifConfigurationModule());

        // The default JPEG quality (75) shows visible artifacts on photos.
        configuration.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder { Quality = JpegQuality });
        return configuration;
    }

    public static async Task<ProcessedImage> ProcessAsync(Stream input, ImageSize size, CancellationToken cancellationToken)
    {
        try
        {
            // Read only the header first, so we can refuse huge images before decoding the pixels.
            input.Position = 0;
            var info = await Image.IdentifyAsync(Options, input, cancellationToken);
            var pixels = (long)info.Width * info.Height * Math.Max(1, info.FrameMetadataCollection.Count);
            if (pixels > MaxPixels) throw new InvalidImageException("The image has too many pixels.");

            input.Position = 0;
            using var image = await Image.LoadAsync(Options, input, cancellationToken);
            var format = image.Metadata.DecodedImageFormat!;

            // Phones store portrait photos sideways plus an "orientation" tag in the EXIF data.
            // Turn the pixels the right way up *before* removing the tag, or the photo would show sideways.
            image.Mutate(x => x.AutoOrient());
            Shrink(image, size);
            RemoveMetadata(image);

            var output = new MemoryStream();
            await image.SaveAsync(output, format, cancellationToken);
            output.Position = 0;

            // Use the format we actually decoded, not the content type the browser claimed.
            return new ProcessedImage(output, ImageRules.ExtensionByContentType[format.DefaultMimeType]);
        }
        catch (ImageFormatException)
        {
            throw new InvalidImageException("The file content is not a valid image.");
        }
    }

    private static void Shrink(Image image, ImageSize size)
    {
        if (size.CropToSquare)
        {
            // Cut out the middle square. A picture smaller than the square keeps its size (no blurry enlarging).
            var side = Math.Min(Math.Min(size.MaxWidth, size.MaxHeight), Math.Min(image.Width, image.Height));
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(side, side), Mode = ResizeMode.Crop }));
        }
        else if (image.Width > size.MaxWidth || image.Height > size.MaxHeight)
        {
            // Shrink until both sides fit, keeping the shape.
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(size.MaxWidth, size.MaxHeight), Mode = ResizeMode.Max }));
        }
    }

    /// <summary>
    /// Removes everything that could describe the person, place or device. The ICC color profile is kept:
    /// it only says how to show the colors, and without it photos from newer phones would look washed out.
    /// </summary>
    private static void RemoveMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        // Animated GIF and WebP frames can carry their own copies.
        foreach (var frame in image.Frames)
        {
            frame.Metadata.ExifProfile = null;
            frame.Metadata.XmpProfile = null;
            frame.Metadata.IptcProfile = null;
        }

        // Free-text fields specific to one format.
        image.Metadata.GetPngMetadata().TextData.Clear();
        image.Metadata.GetGifMetadata().Comments.Clear();
    }
}
