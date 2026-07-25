namespace Photino.Okf_Todo.Data;

public sealed class TaskList
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TaskItem> Tasks { get; set; } = [];
}
