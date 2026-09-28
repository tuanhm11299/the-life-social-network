using System.Net.Http.Headers;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;
using SixLabors.ImageSharp.PixelFormats;

namespace TheLife.Tests.Api;

/// <summary>Helpers to build multipart uploads with a tiny but valid PNG, and realistic photos with metadata.</summary>
public static class TestImages
{
    // A 1×1 pixel PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private const string Xmp =
        """<x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"/></x:xmpmeta>""";

    public static ByteArrayContent PngFile() => File(Png, "image/png");

    public static ByteArrayContent File(byte[] bytes, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }

    /// <summary>Form for POST /api/posts.</summary>
    public static MultipartFormDataContent PostForm(string caption, int imageCount = 1)
    {
        var form = new MultipartFormDataContent { { new StringContent(caption), "caption" } };
        for (var i = 0; i < imageCount; i++) form.Add(PngFile(), "images", $"photo{i}.png");
        return form;
    }

    /// <summary>
    /// A 64×48 photo like one straight from a phone: EXIF with a GPS location and an orientation tag, plus XMP.
    /// Orientation 6 means "the camera was held upright, turn the stored pixels 90° clockwise to show them".
    /// </summary>
    public static byte[] PhotoWithLocation(IImageFormat format, ushort orientation = 1)
    {
        using var image = new Image<Rgba32>(64, 48, Color.CornflowerBlue);

        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, [new Rational(52, 1), new Rational(22, 1), new Rational(8, 1)]);
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLongitude, [new Rational(4, 1), new Rational(53, 1), new Rational(42, 1)]);
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, orientation);
        image.Metadata.XmpProfile = new XmpProfile(Encoding.UTF8.GetBytes(Xmp));

        return Encode(image, format);
    }

    /// <summary>A plain JPEG of the given size, without any metadata.</summary>
    public static byte[] Plain(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.Teal);
        return Encode(image, JpegFormat.Instance);
    }

    /// <summary>An animated GIF with three differently colored frames and a comment.</summary>
    public static byte[] AnimatedGif()
    {
        using var image = new Image<Rgba32>(16, 16, Color.Red);
        image.Frames.CreateFrame(Color.Green);
        image.Frames.CreateFrame(Color.Blue);
        image.Metadata.GetGifMetadata().Comments.Add("Taken at 12 Example Street");

        return Encode(image, GifFormat.Instance);
    }

    private static byte[] Encode(Image image, IImageFormat format)
    {
        using var output = new MemoryStream();
        image.Save(output, format);
        return output.ToArray();
    }
}
