namespace ContosoDashboard.Models;

public class DocumentFilterState
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "uploadedAt";
    public string Direction { get; set; } = "desc";

    public DocumentQueryOptions ToQueryOptions() => new()
    {
        Category = Category,
        ProjectId = ProjectId,
        FromDate = FromDate,
        ToDate = ToDate,
        SortBy = SortBy,
        Direction = Direction
    };
}
