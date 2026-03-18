namespace ContosoDashboard.Models;

public class DocumentShareRequest
{
    public int RequestingUserId { get; set; }
    public List<int> UserIds { get; set; } = new();
    public int? ProjectId { get; set; }
    public string? Message { get; set; }
}
