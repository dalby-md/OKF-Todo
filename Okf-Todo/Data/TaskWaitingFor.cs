namespace Photino.Okf_Todo.Data;

public sealed class TaskWaitingFor
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public string Label { get; set; } = string.Empty;

    public DateTime WaitingSince { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
