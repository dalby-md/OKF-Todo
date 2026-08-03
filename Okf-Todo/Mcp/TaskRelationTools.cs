using System.ComponentModel;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskRelationTools
{
    [McpServerTool(Name = "task_relationship_options", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Discover active relationship type codes and eligible target tasks for one task.")]
    public static Task<TaskRelationOptionsDto> GetOptionsAsync(
        ApplicationCommandService commandService,
        [Description("Numeric source task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskRelationOptionsDto>(
            commandService, "task.relation.options", new TaskRelationOptionsRequest(taskId), cancellationToken);

    [McpServerTool(Name = "task_relationship_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List all forward and reverse relationships for one task.")]
    public static Task<IReadOnlyCollection<TaskRelationDto>> ListAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.relation.list", new TaskRelationListRequest(taskId), cancellationToken);

    [McpServerTool(Name = "task_relationship_add", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Create an approved typed relationship between two non-Trash tasks and log it on both tasks.")]
    public static Task<IReadOnlyCollection<TaskRelationDto>> AddAsync(
        ApplicationCommandService commandService,
        [Description("Numeric source task ID.")] int taskId,
        [Description("Numeric related target task ID.")] int targetTaskId,
        [Description("Stable relationship type code from task_relationship_options.")] string relationTypeCode,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            commandService,
            "task.relation.create",
            new TaskRelationCreateRequest(taskId, targetTaskId, relationTypeCode),
            cancellationToken);

    [McpServerTool(Name = "task_relationship_delete", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Permanently remove one task relationship and log its removal on both tasks.")]
    public static Task<IReadOnlyCollection<TaskRelationDto>> DeleteAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID shown in the relationship.")] int taskId,
        [Description("Relationship ID returned by task_relationship_list.")] int relationshipId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            commandService,
            "task.relation.delete",
            new TaskRelationDeleteRequest(taskId, relationshipId),
            cancellationToken);

    private static Task<IReadOnlyCollection<TaskRelationDto>> ExecuteAsync(
        ApplicationCommandService commandService,
        string commandType,
        object payload,
        CancellationToken cancellationToken) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskRelationDto>>(
            commandService, commandType, payload, cancellationToken);
}
