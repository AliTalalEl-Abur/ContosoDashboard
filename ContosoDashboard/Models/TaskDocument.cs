using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class TaskDocument
{
    public int TaskId { get; set; }

    public int DocumentId { get; set; }

    public DateTime AttachedAtUtc { get; set; } = DateTime.UtcNow;

    public int AttachedByUserId { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem Task { get; set; } = null!;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(AttachedByUserId))]
    public virtual User AttachedByUser { get; set; } = null!;
}
