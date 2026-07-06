namespace Photino.Okf_Todo.Data;

public sealed class TaskRelationType : LookupEntity
{
    public string ReverseName { get; set; } = string.Empty;

    public List<TaskRelation> Relations { get; set; } = [];
}
