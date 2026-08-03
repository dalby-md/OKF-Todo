using System.ComponentModel;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskListTools
{
    [McpServerTool(Name = "task_list_create", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Create a concrete task list with a case-insensitively unique name.")]
    public static Task<TaskListDto> CreateAsync(
        ApplicationCommandService commandService,
        [Description("New list name.")] string name,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskListDto>(
            commandService, "taskList.create", new TaskListCreateRequest(name), cancellationToken);

    [McpServerTool(Name = "task_list_rename", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Rename a concrete task list.")]
    public static Task<TaskListDto> RenameAsync(
        ApplicationCommandService commandService,
        [Description("Numeric list ID.")] int id,
        [Description("Replacement list name.")] string name,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskListDto>(
            commandService, "taskList.rename", new TaskListRenameRequest(id, name), cancellationToken);

    [McpServerTool(Name = "task_list_reorder", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Replace manual task-list order. Supply every current list ID exactly once.")]
    public static Task<IReadOnlyCollection<TaskListDto>> ReorderAsync(
        ApplicationCommandService commandService,
        [Description("Every concrete task-list ID in desired order.")] IReadOnlyCollection<int> orderedListIds,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskListDto>>(
            commandService, "taskList.reorder", new TaskListReorderRequest(orderedListIds), cancellationToken);

    [McpServerTool(Name = "task_list_delete", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Delete a concrete list. If it contains tasks, destinationListId is required and every normal and Trash task is moved transactionally. The final list cannot be deleted.")]
    public static Task<TaskListDeleteResult> DeleteAsync(
        ApplicationCommandService commandService,
        [Description("Numeric list ID to delete.")] int id,
        [Description("Destination list ID for every task owned by the deleted list; omit only when the list is empty.")] int? destinationListId = null,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskListDeleteResult>(
            commandService,
            "taskList.delete",
            new TaskListDeleteRequest(id, destinationListId),
            cancellationToken);
}
