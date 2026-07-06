namespace Photino.Okf_Todo.Data;

public sealed class TaskRelation
{
    public int Id { get; set; }

    public int SourceTaskId { get; set; }

    public TaskItem? SourceTask { get; set; }

    public int TargetTaskId { get; set; }

    public TaskItem? TargetTask { get; set; }

    public int TaskRelationTypeId { get; set; }

    public TaskRelationType? TaskRelationType { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
