namespace ContosoDashboard.Models;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "AppData/uploads";
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}
