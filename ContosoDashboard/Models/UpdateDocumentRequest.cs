namespace ContosoDashboard.Models;

public class UpdateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = DocumentCategories.Other;
    public string? Tags { get; set; }
}
