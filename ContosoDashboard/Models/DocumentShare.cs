using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public int? SharedWithUserId { get; set; }

    public int? SharedWithProjectId { get; set; }

    [Required]
    public DateTime SharedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Message { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(SharedByUserId))]
    public virtual User SharedByUser { get; set; } = null!;

    [ForeignKey(nameof(SharedWithUserId))]
    public virtual User? SharedWithUser { get; set; }

    [ForeignKey(nameof(SharedWithProjectId))]
    public virtual Project? SharedWithProject { get; set; }
}
