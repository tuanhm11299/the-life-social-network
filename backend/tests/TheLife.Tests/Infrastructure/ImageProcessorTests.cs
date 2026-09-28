using TheLife.Application.Common.Files;
using TheLife.Infrastructure.Images;
using TheLife.Tests.Api;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace TheLife.Tests.Infrastructure;

// Unit tests for the image cleaning done before every upload is stored. No database or Docker needed.

public class ImageProcessorTests
{
    public static TheoryData<IImageFormat> PhotoFormats =>
        [JpegFormat.Instance, PngFormat.Instance, WebpFormat.Instance];

    [Theory]
    [MemberData(nameof(PhotoFormats))]
    public async Task Location_and_other_metadata_are_removed(IImageFormat format)
    {
        var photo = TestImages.PhotoWithLocation(format);
        Assert.NotNull(Image.Identify(photo).Metadata.ExifProfile); // the test photo really has EXIF to remove

        var result = await ProcessAsync(photo);

        var info = Image.Identify(result.Bytes);
        Assert.Null(info.Metadata.ExifProfile);
        Assert.Null(info.Metadata.XmpProfile);
        Assert.Same(format, info.Metadata.DecodedImageFormat);
    }

    [Fact]
    public async Task Sideways_phone_photos_are_turned_upright_before_the_orientation_tag_is_removed()
    {
        var photo = TestImages.PhotoWithLocation(JpegFormat.Instance, orientation: 6); // stored 64×48, shown 48×64

        var result = await ProcessAsync(photo);

        var info = Image.Identify(result.Bytes);
        Assert.Equal(48, info.Width);
        Assert.Equal(64, info.Height);
    }

    [Fact]
    public async Task Animated_gifs_keep_their_frames_but_lose_their_comments()
    {
        var result = await ProcessAsync(TestImages.AnimatedGif());

        using var image = Image.Load(result.Bytes);
        Assert.Equal(3, image.Frames.Count);
        Assert.Empty(image.Metadata.GetGifMetadata().Comments);
    }

    [Fact]
    public async Task The_extension_comes_from_the_real_format()
    {
        var result = await ProcessAsync(TestImages.PhotoWithLocation(WebpFormat.Instance));

        Assert.Equal(".webp", result.Extension);
    }

    [Fact]
    public async Task Broken_image_content_is_rejected()
    {
        // Starts like a JPEG, so it passes the "first bytes" check in ImageRules, but the rest is garbage.
        byte[] broken = [0xFF, 0xD8, 0xFF, 0xE0, .. Enumerable.Repeat((byte)0x42, 200)];

        await Assert.ThrowsAsync<InvalidImageException>(() => ProcessAsync(broken));
    }

    [Fact]
    public async Task Images_with_too_many_pixels_are_rejected_before_decoding()
    {
        // A GIF header states the canvas size in bytes 6-9 (little-endian). Claim 65535×65535 (~4.3 billion pixels)
        // while the file stays tiny: that is exactly what a "decompression bomb" looks like.
        var gif = TestImages.AnimatedGif();
        gif[6] = gif[7] = gif[8] = gif[9] = 0xFF;

        var exception = await Assert.ThrowsAsync<InvalidImageException>(() => ProcessAsync(gif));
        Assert.Contains("too many pixels", exception.Message);
    }

    [Fact]
    public async Task Large_photos_are_shrunk_to_fit_keeping_their_shape()
    {
        var result = await ProcessAsync(TestImages.Plain(3000, 2000), ImageSize.Photo);

        var info = Image.Identify(result.Bytes);
        Assert.Equal(1080, info.Width);
        Assert.Equal(720, info.Height);
    }

    [Fact]
    public async Task Small_photos_are_never_enlarged()
    {
        var result = await ProcessAsync(TestImages.Plain(200, 100), ImageSize.Photo);

        var info = Image.Identify(result.Bytes);
        Assert.Equal(200, info.Width);
        Assert.Equal(100, info.Height);
    }

    [Theory]
    [InlineData(3000, 2000, 320)] // landscape: the middle square, shrunk
    [InlineData(1000, 4000, 320)] // tall: same
    [InlineData(200, 100, 100)]   // smaller than a thumbnail: a square as big as the short side, not enlarged
    public async Task Thumbnails_are_squares(int width, int height, int expectedSide)
    {
        var result = await ProcessAsync(TestImages.Plain(width, height), ImageSize.Thumbnail);

        var info = Image.Identify(result.Bytes);
        Assert.Equal(expectedSide, info.Width);
        Assert.Equal(expectedSide, info.Height);
    }

    private static async Task<(byte[] Bytes, string Extension)> ProcessAsync(byte[] input, ImageSize? size = null)
    {
        var result = await ImageProcessor.ProcessAsync(new MemoryStream(input), size ?? ImageSize.Photo, CancellationToken.None);
        using var output = new MemoryStream();
        await result.Content.CopyToAsync(output);
        return (output.ToArray(), result.Extension);
    }
}
