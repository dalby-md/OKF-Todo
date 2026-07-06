namespace Photino.Okf_Todo.Data;

public sealed class TaskWaitingFor
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public int? WaitingForTypeId { get; set; }

    public WaitingForType? WaitingForType { get; set; }

    public string? Label { get; set; }

    public string? Reference { get; set; }

    public string? Url { get; set; }

    public int? StakeholderId { get; set; }

    public TaskStakeholder? Stakeholder { get; set; }

    public DateTime WaitingSince { get; set; }

    public DateTime? FollowUpAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
