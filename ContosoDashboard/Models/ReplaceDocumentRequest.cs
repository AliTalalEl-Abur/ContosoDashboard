namespace ContosoDashboard.Models;

public class ReplaceDocumentRequest
{
    public int RequestingUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
}
