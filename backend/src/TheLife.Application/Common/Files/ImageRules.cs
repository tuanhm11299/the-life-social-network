using FluentValidation;

namespace TheLife.Application.Common.Files;

/// <summary>
/// Validation rules for uploaded images, shared by posts, stories and avatars.
/// Usage in a validator: <c>RuleFor(x => x.Image).MustBeAnImage();</c>
/// </summary>
public static class ImageRules
{
    public const long MaxSizeInBytes = 10 * 1024 * 1024; // 10 MB

    public static readonly IReadOnlyDictionary<string, string> ExtensionByContentType = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public static IRuleBuilderOptions<T, FileUpload?> MustBeAnImage<T>(this IRuleBuilder<T, FileUpload?> rule) =>
        rule
            .NotNull().WithMessage("An image is required.")
            .Must(file => file!.Length > 0).WithMessage("The image is empty.")
            .Must(file => file!.Length <= MaxSizeInBytes).WithMessage("The image must be 10 MB or smaller.")
            .Must(file => ExtensionByContentType.ContainsKey(file!.ContentType.ToLowerInvariant()))
                .WithMessage("Only JPEG, PNG, WebP and GIF images are allowed.")
            .Must(file => HasImageSignature(file!.Content))
                .WithMessage("The file content is not a valid image.");

    /// <summary>
    /// Checks the first bytes of the file ("magic numbers"), so a renamed .exe cannot
    /// pretend to be a .jpg just by lying about its content type.
    /// </summary>
    private static bool HasImageSignature(Stream stream)
    {
        if (!stream.CanSeek) return false;

        Span<byte> header = stackalloc byte[12];
        var originalPosition = stream.Position;
        var read = stream.Read(header);
        stream.Position = originalPosition;

        if (read < 4) return false;

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isGif = header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38;
        var isWebp = read >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46   // "RIFF"
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // "WEBP"

        return isJpeg || isPng || isGif || isWebp;
    }
}
