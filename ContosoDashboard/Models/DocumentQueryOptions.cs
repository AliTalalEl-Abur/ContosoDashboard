namespace ContosoDashboard.Models;

public class DocumentQueryOptions
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "uploadedAt";
    public string Direction { get; set; } = "desc";
}
