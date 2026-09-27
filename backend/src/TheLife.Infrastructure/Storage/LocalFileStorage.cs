using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TheLife.Infrastructure.Storage;

/// <summary>Settings from the "Storage" section of appsettings.json.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder for uploaded files. Relative paths are relative to the API project folder.</summary>
    public string RootPath { get; init; } = "uploads";

    /// <summary>URL prefix the files are served under.</summary>
    public string PublicBasePath { get; init; } = "/uploads";
}

/// <summary>
/// Stores uploads on the local disk and serves them as static files.
/// To move to cloud storage (S3, Azure Blob...), write another IFileStorage and register it instead.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _publicBasePath;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        RootDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _publicBasePath = options.Value.PublicBasePath.TrimEnd('/');
        Directory.CreateDirectory(RootDirectory);
    }

    /// <summary>Absolute path of the uploads folder on disk.</summary>
    public string RootDirectory { get; }

    public async Task<string> SaveImageAsync(FileUpload file, string folder, CancellationToken cancellationToken)
    {
        // Never trust the client's file name: generate our own and derive the extension from the (validated) type.
        var extension = ImageRules.ExtensionByContentType[file.ContentType.ToLowerInvariant()];
        var fileName = $"{Guid.CreateVersion7():N}{extension}";

        var directory = Path.Combine(RootDirectory, folder);
        Directory.CreateDirectory(directory);

        file.Content.Position = 0;
        await using (var output = File.Create(Path.Combine(directory, fileName)))
        {
            await file.Content.CopyToAsync(output, cancellationToken);
        }

        return $"{_publicBasePath}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        // Ignore URLs that are not ours, e.g. external demo images from the seed data.
        if (!url.StartsWith(_publicBasePath + "/", StringComparison.Ordinal)) return Task.CompletedTask;

        var relativePath = url[(_publicBasePath.Length + 1)..];
        var fullPath = Path.GetFullPath(Path.Combine(RootDirectory, relativePath));

        // Safety net against "../" tricks: only ever delete inside the uploads folder.
        if (fullPath.StartsWith(RootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
