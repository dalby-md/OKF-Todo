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
    [Description("Search and list OKF-Todo tasks by operational view, list, text, tags, types, statuses, and priorities.")]
    public static Task<IReadOnlyCollection<TaskListItemDto>> ListAsync(
        ApplicationCommandService commandService,
        [Description("Task view: active, ready, starred, attention, actnow, urgent, waiting, overdue, completed, all, or trash. Defaults to active.")] string? view = null,
        [Description("Optional concrete task-list ID. Omit to search across all lists. Trash is always global.")] int? taskListId = null,
        [Description("Optional text matched against title, body, source reference, owner, responsible, and tags.")] string? search = null,
        [Description("Optional tags with OR semantics: a task matches when it has any supplied tag.")] IReadOnlyCollection<string>? tags = null,
        [Description("Optional stable task-type codes with OR semantics.")] IReadOnlyCollection<string>? taskTypeCodes = null,
        [Description("Optional stable lifecycle-status codes with OR semantics.")] IReadOnlyCollection<string>? taskStatusCodes = null,
        [Description("Optional stable priority codes with OR semantics.")] IReadOnlyCollection<string>? taskPriorityCodes = null,
        [Description("Maximum results from 1 through 1000. Defaults to 200.")] int? limit = 200,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskListItemDto>>(
            commandService,
            "task.list",
            new TaskListRequest(
                view,
                taskListId,
                search,
                tags,
                taskTypeCodes,
                taskStatusCodes,
                taskPriorityCodes,
                limit),
            cancellationToken);

    [McpServerTool(Name = "task_get_lookups", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Discover valid task types, statuses, priorities, sources, body formats, and existing plain-string tags before proposing values.")]
    public static Task<TaskLookupsDto> GetLookupsAsync(
        ApplicationCommandService commandService,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskLookupsDto>(
            commandService,
            "task.lookups.get",
            new { },
            cancellationToken);

    [McpServerTool(Name = "task_list_lists", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Discover the concrete task lists available in OKF-Todo, including task counts and manual order.")]
    public static Task<IReadOnlyCollection<TaskListDto>> ListTaskListsAsync(
        ApplicationCommandService commandService,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskListDto>>(
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
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
            commandService,
            "task.get",
            new TaskGetRequest(id),
            cancellationToken);

    [McpServerTool(Name = "task_get_context", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get a complete working context for one task: fields, checklist, relationships, attachment metadata, and Timeline.")]
    public static async Task<TaskContextDto> GetContextAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        CancellationToken cancellationToken = default)
    {
        var task = await GetAsync(commandService, id, cancellationToken);
        var checklist = await McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskChecklistItemDto>>(
            commandService, "task.checklist.list", new TaskChecklistListRequest(id), cancellationToken);
        var relationships = await McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskRelationDto>>(
            commandService, "task.relation.list", new TaskRelationListRequest(id), cancellationToken);
        var attachments = await McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskAttachmentDto>>(
            commandService, "task.attachment.list", new TaskAttachmentListRequest(id), cancellationToken);
        var timeline = await GetTimelineAsync(commandService, id, cancellationToken);
        return new TaskContextDto(task, checklist, relationships, attachments, timeline);
    }

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
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
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
        McpToolExecutor.ExecuteAsync<TaskDetailDto>(
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

    [McpServerTool(Name = "task_patch", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Safely change only named editable task fields. The changes object may contain title, taskTypeCode, body, bodyFormatCode, taskPriorityCode, taskSourceCode, sourceReference, sourceUrl, deadline, activeWaitingForLabel, tags, owner, responsible, or taskListId. Explicit null clears a nullable field; omitted fields are preserved.")]
    public static async Task<TaskDetailDto> PatchAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int id,
        [Description("JSON object containing only approved fields. Example: {\"taskPriorityCode\":\"URGENT\",\"deadline\":\"2026-08-10\"}.")] JsonElement changes,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(commandService, id, cancellationToken);
        var request = CreatePatchRequest(current, changes);
        return await McpToolExecutor.ExecuteAsync<TaskDetailDto>(
            commandService,
            "task.update",
            request,
            cancellationToken);
    }

    [McpServerTool(Name = "task_move_to_list", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Move one or more existing, non-Trash tasks to a concrete task list and record each move in the Timeline.")]
    public static Task<TaskListMoveResult> MoveToListAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task IDs to move.")] IReadOnlyCollection<int> taskIds,
        [Description("Destination task-list ID returned by task_list_lists.")] int destinationListId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskListMoveResult>(
            commandService,
            "taskList.moveTasks",
            new TaskListMoveRequest(taskIds, destinationListId),
            cancellationToken);

    [McpServerTool(Name = "task_undo_list_move", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Undo a task_move_to_list result. Pass the complete items array returned by that move.")]
    public static Task<TaskListMoveResult> UndoListMoveAsync(
        ApplicationCommandService commandService,
        [Description("Complete move items returned by task_move_to_list.")] IReadOnlyCollection<TaskListMoveItemDto> items,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskListMoveResult>(
            commandService,
            "taskList.undoMove",
            new TaskListUndoMoveRequest(items),
            cancellationToken);

    [McpServerTool(Name = "task_get_timeline", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get the comments and application-generated log entries for one task, newest first.")]
    public static Task<IReadOnlyCollection<TaskTimelineItemDto>> GetTimelineAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskTimelineItemDto>>(
            commandService,
            "task.timeline.get",
            new TaskTimelineRequest(taskId),
            cancellationToken);

    [McpServerTool(Name = "task_add_comment", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Add an approved progress note or human-readable observation to a task Timeline.")]
    public static Task<IReadOnlyCollection<TaskTimelineItemDto>> AddCommentAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Comment text.")] string commentText,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskTimelineItemDto>>(
            commandService,
            "task.comment.create",
            new TaskCommentCreateRequest(taskId, commentText),
            cancellationToken);

    [McpServerTool(Name = "task_delete_comment", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Permanently delete one user comment from a task. Automatic Timeline entries cannot be deleted.")]
    public static Task<IReadOnlyCollection<TaskTimelineItemDto>> DeleteCommentAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Comment ID returned by task_get_timeline.")] int commentId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskTimelineItemDto>>(
            commandService,
            "task.comment.delete",
            new TaskCommentDeleteRequest(taskId, commentId),
            cancellationToken);

    private static TaskSaveRequest CreatePatchRequest(TaskDetailDto current, JsonElement changes)
    {
        if (changes.ValueKind != JsonValueKind.Object)
        {
            throw new McpException("Changes must be a JSON object.");
        }

        var title = current.Title;
        var taskTypeCode = current.TaskTypeCode;
        var body = current.Body;
        var bodyFormatCode = current.BodyFormatCode;
        var taskPriorityCode = current.TaskPriorityCode;
        var taskSourceCode = current.TaskSourceCode;
        var sourceReference = current.SourceReference;
        var sourceUrl = current.SourceUrl;
        var deadline = current.Deadline;
        var activeWaitingForLabel = current.ActiveWaitingFor?.Label;
        IReadOnlyCollection<string>? tags = current.Tags;
        var owner = current.Owner;
        var responsible = current.Responsible;
        int? taskListId = current.TaskListId;

        foreach (var property in changes.EnumerateObject())
        {
            switch (property.Name.ToLowerInvariant())
            {
                case "title":
                    title = ReadRequiredString(property);
                    break;
                case "tasktypecode":
                    taskTypeCode = ReadRequiredString(property);
                    break;
                case "body":
                    body = ReadNullableString(property);
                    break;
                case "bodyformatcode":
                    bodyFormatCode = ReadNullableString(property);
                    break;
                case "taskprioritycode":
                    taskPriorityCode = ReadNullableString(property);
                    break;
                case "tasksourcecode":
                    taskSourceCode = ReadNullableString(property);
                    break;
                case "sourcereference":
                    sourceReference = ReadNullableString(property);
                    break;
                case "sourceurl":
                    sourceUrl = ReadNullableString(property);
                    break;
                case "deadline":
                    deadline = property.Value.ValueKind == JsonValueKind.Null
                        ? null
                        : property.Value.Deserialize<DateTime>(JsonOptions);
                    break;
                case "activewaitingforlabel":
                    activeWaitingForLabel = ReadNullableString(property);
                    break;
                case "tags":
                    tags = property.Value.ValueKind == JsonValueKind.Null
                        ? []
                        : property.Value.Deserialize<IReadOnlyCollection<string>>(JsonOptions)
                            ?? throw new McpException("Patch field 'tags' must be an array of strings or null.");
                    break;
                case "owner":
                    owner = ReadNullableString(property);
                    break;
                case "responsible":
                    responsible = ReadNullableString(property);
                    break;
                case "tasklistid":
                    taskListId = property.Value.ValueKind == JsonValueKind.Number
                        ? property.Value.GetInt32()
                        : throw new McpException("Patch field 'taskListId' must be a numeric list ID.");
                    break;
                default:
                    throw new McpException($"Patch field '{property.Name}' is not supported.");
            }
        }

        return new TaskSaveRequest(
            current.Id,
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
            taskListId);
    }

    private static string ReadRequiredString(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.Value.GetString()))
        {
            throw new McpException($"Patch field '{property.Name}' must be a non-empty string.");
        }

        return property.Value.GetString()!;
    }

    private static string? ReadNullableString(JsonProperty property)
    {
        return property.Value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => property.Value.GetString(),
            _ => throw new McpException($"Patch field '{property.Name}' must be a string or null.")
        };
    }
}

public sealed record TaskContextDto(
    TaskDetailDto Task,
    IReadOnlyCollection<TaskChecklistItemDto> Checklist,
    IReadOnlyCollection<TaskRelationDto> Relationships,
    IReadOnlyCollection<TaskAttachmentDto> Attachments,
    IReadOnlyCollection<TaskTimelineItemDto> Timeline);
