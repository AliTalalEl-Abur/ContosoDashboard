namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<FileStorageSaveResult> SaveAsync(int userId, int? projectId, string originalFileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public class FileStorageSaveResult
{
    public string RelativePath { get; set; } = string.Empty;
}
