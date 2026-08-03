using System.ComponentModel;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

[McpServerToolType]
public static class TaskAttachmentTools
{
    [McpServerTool(Name = "task_attachment_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List attachment metadata for one task without returning file content.")]
    public static Task<IReadOnlyCollection<TaskAttachmentDto>> ListAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskAttachmentDto>>(
            commandService, "task.attachment.list", new TaskAttachmentListRequest(taskId), cancellationToken);

    [McpServerTool(Name = "task_attachment_get", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Read one attachment as base64 content. Inspect metadata first and avoid loading large files unless needed.")]
    public static Task<TaskAttachmentContentDto> GetAsync(
        ApplicationCommandService commandService,
        [Description("Attachment ID returned by task_attachment_list.")] int attachmentId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<TaskAttachmentContentDto>(
            commandService, "task.attachment.get", new TaskAttachmentGetRequest(attachmentId), cancellationToken);

    [McpServerTool(Name = "task_attachment_add", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Store an approved base64-encoded attachment in the task database. The existing 25 MB per-file limit is enforced.")]
    public static Task<IReadOnlyCollection<TaskAttachmentDto>> AddAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Safe file name without a path.")] string fileName,
        [Description("Base64-encoded file bytes.")] string base64Data,
        [Description("Optional MIME content type.")] string? contentType = null,
        [Description("Optional attachment description.")] string? description = null,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskAttachmentDto>>(
            commandService,
            "task.attachment.create",
            new TaskAttachmentCreateRequest(taskId, fileName, contentType, base64Data, description),
            cancellationToken);

    [McpServerTool(Name = "task_attachment_delete", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Permanently remove one attachment from a task.")]
    public static Task<IReadOnlyCollection<TaskAttachmentDto>> DeleteAsync(
        ApplicationCommandService commandService,
        [Description("Numeric task ID.")] int taskId,
        [Description("Attachment ID returned by task_attachment_list.")] int attachmentId,
        CancellationToken cancellationToken = default) =>
        McpToolExecutor.ExecuteAsync<IReadOnlyCollection<TaskAttachmentDto>>(
            commandService,
            "task.attachment.delete",
            new TaskAttachmentDeleteRequest(taskId, attachmentId),
            cancellationToken);
}
