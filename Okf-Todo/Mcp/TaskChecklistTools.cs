using System.ComponentModel;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskChecklistTools
{
    [McpServerTool(Name = "task_checklist_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List a task's ordered checklist with completion state.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> ListAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.list", new TaskChecklistListRequest(taskId), cancellationToken);

    [McpServerTool(Name = "task_checklist_add", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Append an approved checklist item to a task.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> AddAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Checklist item text.")] string text,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.create", new TaskChecklistCreateRequest(taskId, text), cancellationToken);

    [McpServerTool(Name = "task_checklist_update", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Replace the text of one checklist item.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> UpdateAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Checklist item ID.")] int checklistItemId,
        [Description("Replacement checklist text.")] string text,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.update", new TaskChecklistUpdateRequest(taskId, checklistItemId, text), cancellationToken);

    [McpServerTool(Name = "task_checklist_set_completed", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Complete or reopen one checklist item.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> SetCompletedAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Checklist item ID.")] int checklistItemId,
        [Description("True to complete; false to reopen.")] bool isCompleted,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.complete", new TaskChecklistCompleteRequest(taskId, checklistItemId, isCompleted), cancellationToken);

    [McpServerTool(Name = "task_checklist_reorder", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Replace checklist order. Supply every current checklist item ID exactly once.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> ReorderAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Every checklist item ID in desired order.")] IReadOnlyCollection<int> orderedChecklistItemIds,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.reorder", new TaskChecklistReorderRequest(taskId, orderedChecklistItemIds), cancellationToken);

    [McpServerTool(Name = "task_checklist_delete", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Permanently delete one checklist item.")]
    public static Task<IReadOnlyCollection<TaskChecklistItemDto>> DeleteAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Checklist item ID.")] int checklistItemId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(commandService, "task.checklist.delete", new TaskChecklistDeleteRequest(taskId, checklistItemId), cancellationToken);

    private static Task<IReadOnlyCollection<TaskChecklistItemDto>> ExecuteAsync(
        ApplicationCommandService commandService,
        string commandType,
        object payload,
        CancellationToken cancellationToken) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskChecklistItemDto>>(
            commandService, commandType, payload, cancellationToken);
}
