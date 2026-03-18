namespace ContosoDashboard.Models;

public class SharedDocumentListItem : DocumentListItem
{
    public string SharedByDisplayName { get; set; } = string.Empty;
    public DateTime SharedAtUtc { get; set; }
}
