using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    [MaxLength(32)]
    public string ActionType { get; set; } = DocumentActivityTypes.Upload;

    [Required]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(4000)]
    public string? DetailsJson { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}

public static class DocumentActivityTypes
{
    public const string Upload = "Upload";
    public const string Download = "Download";
    public const string Share = "Share";
    public const string UpdateMetadata = "UpdateMetadata";
    public const string ReplaceFile = "ReplaceFile";
    public const string Delete = "Delete";
}
