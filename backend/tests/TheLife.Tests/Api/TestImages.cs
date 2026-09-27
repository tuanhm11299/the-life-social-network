using System.Net.Http.Headers;

namespace TheLife.Tests.Api;

/// <summary>Helpers to build multipart uploads with a tiny but valid PNG.</summary>
public static class TestImages
{
    // A 1×1 pixel PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

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
}
