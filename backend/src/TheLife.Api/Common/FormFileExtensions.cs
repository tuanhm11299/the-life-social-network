using TheLife.Application.Common.Files;

namespace TheLife.Api.Common;

internal static class FormFileExtensions
{
    /// <summary>Converts ASP.NET Core's IFormFile into the framework-independent FileUpload used by the handlers.</summary>
    public static FileUpload ToFileUpload(this IFormFile file) =>
        new(file.OpenReadStream(), file.FileName, file.ContentType ?? "", file.Length);
}
