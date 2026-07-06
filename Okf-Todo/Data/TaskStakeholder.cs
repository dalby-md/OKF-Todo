namespace Photino.Okf_Todo.Data;

public sealed class TaskStakeholder
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public int? StakeholderTypeId { get; set; }

    public StakeholderType? StakeholderType { get; set; }

    public int? StakeholderRoleId { get; set; }

    public StakeholderRole? StakeholderRole { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Reference { get; set; }

    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TaskWaitingFor> WaitingTargets { get; set; } = [];
}
