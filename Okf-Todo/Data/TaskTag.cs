namespace Photino.Okf_Todo.Data;

public sealed class TaskTag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TaskTaskTag> TaskTags { get; set; } = [];
}

public sealed class TaskTaskTag
{
    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public int TaskTagId { get; set; }

    public TaskTag? TaskTag { get; set; }

    public DateTime CreatedAt { get; set; }
}
