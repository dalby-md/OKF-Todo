using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "task_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List OKF-Todo tasks. The view can be active, ready, urgent, waiting, overdue, completed, or all.")]
    public static Task<IReadOnlyCollection<TaskListItemDto>> ListAsync(
        ApplicationCommandService commandService,
        [Description("Task view: active, ready, urgent, waiting, overdue, completed, or all. Ready contains active tasks without an unresolved waiting target. Defaults to active.")] string? view = null,
        [Description("Optional concrete task-list ID. Omit to search across all lists. Trash is always global.")] int? taskListId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyCollection<TaskListItemDto>>(
            commandService,
            "task.list",
            new TaskListRequest(view, taskListId),
            cancellationToken);

    [McpServerTool(Name = "task_list_lists", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Discover the concrete task lists available in OKF-Todo, including task counts and manual order.")]
    public static Task<IReadOnlyCollection<TaskListDto>> ListTaskListsAsync(
        ApplicationCommandService commandService,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyCollection<TaskListDto>>(
            commandService,
            "taskList.list",
            new { },
            cancellationToken);

    [McpServerTool(Name = "task_get", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get one OKF-Todo task by its numeric ID.")]
    public static Task<TaskDetailDto> GetAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<TaskDetailDto>(
            commandService,
            "task.get",
            new TaskGetRequest(id),
            cancellationToken);

    [McpServerTool(Name = "task_create", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Create an OKF-Todo task and return the saved task. Lookup inputs use stable codes, not display names.")]
    public static Task<TaskDetailDto> CreateAsync(
        ApplicationCommandService commandService,
        [Description("Task title.")] string title,
        [Description("Stable task type code, for example REQUEST or ERROR.")] string taskTypeCode,
        [Description("Optional HTML task body.")] string? body = null,
        [Description("Stable body format code. Defaults to HTML.")] string? bodyFormatCode = "HTML",
        [Description("Optional stable priority code.")] string? taskPriorityCode = null,
        [Description("Optional stable source code.")] string? taskSourceCode = null,
        [Description("Optional source reference.")] string? sourceReference = null,
        [Description("Optional source URL.")] string? sourceUrl = null,
        [Description("Optional deadline in ISO 8601 form.")] DateTime? deadline = null,
        [Description("Optional waiting-for label. Supplying it places the task in waiting state.")] string? activeWaitingForLabel = null,
        [Description("Optional plain-string tags.")] IReadOnlyCollection<string>? tags = null,
        [Description("Optional task owner.")] string? owner = null,
        [Description("Optional person responsible for the task.")] string? responsible = null,
        [Description("Optional explicit task-list ID. When omitted, OKF-Todo applies its documented list-resolution rule.")] int? taskListId = null,
        [Description("Optional existing/source/related/parent task ID used to infer list ownership when taskListId is omitted.")] int? contextTaskId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<TaskDetailDto>(
            commandService,
            "task.create",
            new TaskSaveRequest(
                null,
                title,
                taskTypeCode,
                body,
                bodyFormatCode,
                taskPriorityCode,
                taskSourceCode,
                sourceReference,
                sourceUrl,
                deadline,
                activeWaitingForLabel,
                tags,
                owner,
                responsible,
                taskListId,
                contextTaskId),
            cancellationToken);

    [McpServerTool(Name = "task_update", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Replace the editable fields of an existing OKF-Todo task. Call task_get first and pass every value that must be preserved; omitted optional fields are cleared.")]
    public static Task<TaskDetailDto> UpdateAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        [Description("Replacement task title.")] string title,
        [Description("Replacement stable task type code.")] string taskTypeCode,
        [Description("Replacement HTML body; null clears it.")] string? body = null,
        [Description("Replacement stable body format code; null clears it.")] string? bodyFormatCode = null,
        [Description("Replacement stable priority code; null clears it.")] string? taskPriorityCode = null,
        [Description("Replacement stable source code; null clears it.")] string? taskSourceCode = null,
        [Description("Replacement source reference; null clears it.")] string? sourceReference = null,
        [Description("Replacement source URL; null clears it.")] string? sourceUrl = null,
        [Description("Replacement deadline; null clears it.")] DateTime? deadline = null,
        [Description("Replacement waiting-for label; null clears active waiting.")] string? activeWaitingForLabel = null,
        [Description("Replacement plain-string tag set; null or empty removes all tags.")] IReadOnlyCollection<string>? tags = null,
        [Description("Replacement task owner; null clears it.")] string? owner = null,
        [Description("Replacement person responsible for the task; null clears it.")] string? responsible = null,
        [Description("Optional destination task-list ID. Omit to keep the task's current list.")] int? taskListId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<TaskDetailDto>(
            commandService,
            "task.update",
            new TaskSaveRequest(
                id,
                title,
                taskTypeCode,
                body,
                bodyFormatCode,
                taskPriorityCode,
                taskSourceCode,
                sourceReference,
                sourceUrl,
                deadline,
                activeWaitingForLabel,
                tags,
                owner,
                responsible,
                taskListId),
            cancellationToken);

    [McpServerTool(Name = "task_move_to_list", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Move one or more existing, non-Trash tasks to a concrete task list and record each move in the Timeline.")]
    public static Task<TaskListMoveResult> MoveToListAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task IDs to move.")] IReadOnlyCollection<int> taskIds,
        [Description("Destination task-list ID returned by task_list_lists.")] int destinationListId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<TaskListMoveResult>(
            commandService,
            "taskList.moveTasks",
            new TaskListMoveRequest(taskIds, destinationListId),
            cancellationToken);

    [McpServerTool(Name = "task_get_timeline", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get the comments and application-generated log entries for one task, newest first.")]
    public static Task<IReadOnlyCollection<TaskTimelineItemDto>> GetTimelineAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyCollection<TaskTimelineItemDto>>(
            commandService,
            "task.timeline.get",
            new TaskTimelineRequest(taskId),
            cancellationToken);

    private static async Task<TResult> ExecuteAsync<TResult>(
        ApplicationCommandService commandService,
        string commandType,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await commandService.ExecuteAsync(
                new ApplicationCommand(commandType, JsonSerializer.SerializeToElement(payload, JsonOptions)),
                cancellationToken);

            return result is TResult typedResult
                ? typedResult
                : throw new McpException($"Application command '{commandType}' returned an unexpected result.");
        }
        catch (ValidationException exception)
        {
            throw new McpException(exception.Message, exception);
        }
        catch (BridgeException exception)
        {
            throw new McpException(exception.Message, exception);
        }
    }
}
