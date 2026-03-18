using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(64)]
    public string Category { get; set; } = DocumentCategories.Other;

    [MaxLength(500)]
    public string? Tags { get; set; }

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string StoredRelativePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(127)]
    public string MimeType { get; set; } = "application/octet-stream";

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public int? DeletedByUserId { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(DeletedByUserId))]
    public virtual User? DeletedByUser { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
    public virtual ICollection<TaskDocument> TaskDocuments { get; set; } = new List<TaskDocument>();

    [NotMapped]
    public IReadOnlyList<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? Array.Empty<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public static class DocumentCategories
{
    public const string ProjectDocuments = "Project Documents";
    public const string TeamResources = "Team Resources";
    public const string PersonalFiles = "Personal Files";
    public const string Reports = "Reports";
    public const string Presentations = "Presentations";
    public const string Other = "Other";

    public static readonly string[] All =
    [
        ProjectDocuments,
        TeamResources,
        PersonalFiles,
        Reports,
        Presentations,
        Other
    ];
}
