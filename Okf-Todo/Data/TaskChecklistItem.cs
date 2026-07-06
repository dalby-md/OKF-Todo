namespace Photino.Okf_Todo.Data;

public sealed class TaskChecklistItem
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
