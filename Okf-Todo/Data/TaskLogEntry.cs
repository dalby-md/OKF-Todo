namespace Photino.Okf_Todo.Data;

public sealed class TaskLogEntry
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public int TaskLogTypeId { get; set; }

    public TaskLogType? TaskLogType { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }
}
