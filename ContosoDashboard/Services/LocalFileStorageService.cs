using Microsoft.Extensions.Options;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly DocumentStorageOptions _options;

    public LocalFileStorageService(IWebHostEnvironment environment, IOptions<DocumentStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<FileStorageSaveResult> SaveAsync(int userId, int? projectId, string originalFileName, Stream content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
        var relativePath = Path.Combine(
            userId.ToString(),
            projectId?.ToString() ?? "general",
            $"{Guid.NewGuid():N}{safeExtension}").Replace('\\', '/');

        var rootPath = GetRootPath();
        var absolutePath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(absolutePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        return new FileStorageSaveResult
        {
            RelativePath = relativePath
        };
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(GetRootPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(GetRootPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string GetRootPath()
    {
        var configured = _options.RootPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(_environment.ContentRootPath, configured);
    }
}
