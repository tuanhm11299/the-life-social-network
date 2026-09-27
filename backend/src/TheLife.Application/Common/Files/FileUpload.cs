namespace TheLife.Application.Common.Files;

/// <summary>
/// An uploaded file, independent of ASP.NET Core (the API converts IFormFile into this),
/// so the Application layer does not depend on the web framework.
/// </summary>
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
