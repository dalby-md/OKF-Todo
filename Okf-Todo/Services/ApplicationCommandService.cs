using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Photino.Okf_Todo.Services;

public sealed class ApplicationCommandService(IServiceProvider services)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<object> ExecuteAsync(
        ApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;

        return command.Type switch
        {
            "issue.get" => await scopedServices.GetRequiredService<IssueService>()
                .GetOrCreateAsync(GetPayload<IssueGetRequest>(command).Id, cancellationToken),
            "issue.save" => await scopedServices.GetRequiredService<IssueService>()
                .SaveAsync(GetPayload<IssueSaveRequest>(command), cancellationToken),
            "image.create" => await scopedServices.GetRequiredService<ImageService>()
                .CreateAsync(GetPayload<ImageCreateRequest>(command), cancellationToken),
            "image.get" => await scopedServices.GetRequiredService<ImageService>()
                .GetAsync(GetPayload<ImageGetRequest>(command).Id, cancellationToken),
            "editor.preference.get" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .GetEditorPreferenceAsync(cancellationToken),
            "editor.preference.save" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .SaveEditorPreferenceAsync(GetPayload<EditorPreferenceSaveRequest>(command), cancellationToken),
            "layout.preference.get" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .GetLayoutPreferenceAsync(cancellationToken),
            "layout.preference.save" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .SaveLayoutPreferenceAsync(GetPayload<LayoutPreferenceSaveRequest>(command), cancellationToken),
            "help.runtimeContext.get" => scopedServices.GetRequiredService<HelpRuntimeContextService>()
                .GetContext(),
            "database.backup.create" => await scopedServices.GetRequiredService<DatabaseBackupService>()
                .CreateAsync(cancellationToken),
            "database.status.get" => await scopedServices.GetRequiredService<SampleDataService>()
                .GetStatusAsync(cancellationToken),
            "database.sample.create" => await scopedServices.GetRequiredService<SampleDataService>()
                .CreateAsync(cancellationToken),
            "database.sample.remove" => await scopedServices.GetRequiredService<SampleDataService>()
                .RemoveAsync(cancellationToken),
            "database.restore.prepare" => await scopedServices.GetRequiredService<DatabaseMaintenanceService>()
                .PrepareRestoreAsync(cancellationToken),
            "database.reset.prepare" => await scopedServices.GetRequiredService<DatabaseMaintenanceService>()
                .PrepareResetAsync(GetPayload<DatabaseResetRequest>(command), cancellationToken),
            "application.close" => scopedServices.GetRequiredService<ApplicationLifetimeService>()
                .RequestClose(),
            "task.export.markdown" => await scopedServices.GetRequiredService<TaskMarkdownExportService>()
                .ExportAsync(GetPayload<TaskMarkdownExportRequest>(command), cancellationToken),
            "task.export.html" => await scopedServices.GetRequiredService<TaskMarkdownExportService>()
                .CreateHtmlClipboardAsync(GetPayload<TaskMarkdownExportRequest>(command), cancellationToken),
            "task.export.columns.get" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .GetTaskExportColumnPreferenceAsync(cancellationToken),
            "task.export.columns.save" => await scopedServices.GetRequiredService<AppPreferenceService>()
                .SaveTaskExportColumnPreferenceAsync(
                    GetPayload<TaskExportColumnPreferenceSaveRequest>(command),
                    cancellationToken),
            "task.lookups.get" => await scopedServices.GetRequiredService<TaskService>()
                .GetLookupsAsync(cancellationToken),
            "lookup.settings.get" => await scopedServices.GetRequiredService<TaskService>()
                .GetLookupSettingsAsync(cancellationToken),
            "lookup.settings.update" => await scopedServices.GetRequiredService<TaskService>()
                .UpdateLookupAsync(GetPayload<LookupUpdateRequest>(command), cancellationToken),
            "lookup.settings.create" => await scopedServices.GetRequiredService<TaskService>()
                .CreateLookupAsync(GetPayload<LookupCreateRequest>(command), cancellationToken),
            "lookup.settings.delete" => await scopedServices.GetRequiredService<TaskService>()
                .DeleteLookupAsync(GetPayload<LookupDeleteRequest>(command), cancellationToken),
            "lookup.settings.reorder" => await scopedServices.GetRequiredService<TaskService>()
                .ReorderLookupAsync(GetPayload<LookupReorderRequest>(command), cancellationToken),
            "tag.settings.list" => await scopedServices.GetRequiredService<TaskService>()
                .GetTagSettingsAsync(cancellationToken),
            "tag.settings.rename" => await scopedServices.GetRequiredService<TaskService>()
                .RenameTagAsync(GetPayload<TagRenameRequest>(command), cancellationToken),
            "tag.settings.delete" => await scopedServices.GetRequiredService<TaskService>()
                .DeleteTagAsync(GetPayload<TagDeleteRequest>(command), cancellationToken),
            "tag.settings.merge" => await scopedServices.GetRequiredService<TaskService>()
                .MergeTagAsync(GetPayload<TagMergeRequest>(command), cancellationToken),
            "taskList.list" => await scopedServices.GetRequiredService<TaskListService>()
                .ListAsync(cancellationToken),
            "taskList.create" => await scopedServices.GetRequiredService<TaskListService>()
                .CreateAsync(GetPayload<TaskListCreateRequest>(command), cancellationToken),
            "taskList.rename" => await scopedServices.GetRequiredService<TaskListService>()
                .RenameAsync(GetPayload<TaskListRenameRequest>(command), cancellationToken),
            "taskList.reorder" => await scopedServices.GetRequiredService<TaskListService>()
                .ReorderAsync(GetPayload<TaskListReorderRequest>(command), cancellationToken),
            "taskList.delete" => await scopedServices.GetRequiredService<TaskListService>()
                .DeleteAsync(GetPayload<TaskListDeleteRequest>(command), cancellationToken),
            "taskList.moveTasks" => await scopedServices.GetRequiredService<TaskListService>()
                .MoveTasksAsync(GetPayload<TaskListMoveRequest>(command), cancellationToken),
            "taskList.undoMove" => await scopedServices.GetRequiredService<TaskListService>()
                .UndoMoveAsync(GetPayload<TaskListUndoMoveRequest>(command), cancellationToken),
            "task.list" => await scopedServices.GetRequiredService<TaskService>()
                .ListAsync(GetPayload<TaskListRequest>(command), cancellationToken),
            "task.get" => await scopedServices.GetRequiredService<TaskService>()
                .GetAsync(GetPayload<TaskGetRequest>(command).Id, cancellationToken),
            "task.create" => await scopedServices.GetRequiredService<TaskService>()
                .CreateAsync(GetPayload<TaskSaveRequest>(command), cancellationToken),
            "task.update" => await scopedServices.GetRequiredService<TaskService>()
                .UpdateAsync(GetPayload<TaskSaveRequest>(command), cancellationToken),
            "task.start" => await scopedServices.GetRequiredService<TaskService>()
                .StartAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.undoStart" => await scopedServices.GetRequiredService<TaskService>()
                .UndoStartAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.complete" => await scopedServices.GetRequiredService<TaskService>()
                .CompleteAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.reopen" => await scopedServices.GetRequiredService<TaskService>()
                .ReopenAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.cancel" => await scopedServices.GetRequiredService<TaskService>()
                .CancelAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.star.set" => await scopedServices.GetRequiredService<TaskService>()
                .SetStarredAsync(GetPayload<TaskStarRequest>(command), cancellationToken),
            "task.star.setMany" => await scopedServices.GetRequiredService<TaskService>()
                .SetStarredManyAsync(GetPayload<TaskBulkStarRequest>(command), cancellationToken),
            "task.trash" => await scopedServices.GetRequiredService<TaskService>()
                .MoveToTrashAsync(GetPayload<TaskIdsRequest>(command), cancellationToken),
            "task.trash.restore" => await scopedServices.GetRequiredService<TaskService>()
                .RestoreFromTrashAsync(GetPayload<TaskIdsRequest>(command), cancellationToken),
            "task.trash.delete" => await scopedServices.GetRequiredService<TaskService>()
                .DeletePermanentlyAsync(GetPayload<TaskIdsRequest>(command), cancellationToken),
            "task.waiting.add" => await scopedServices.GetRequiredService<TaskService>()
                .AddWaitingForAsync(GetPayload<TaskWaitingForSaveRequest>(command), cancellationToken),
            "task.waiting.clear" => await scopedServices.GetRequiredService<TaskService>()
                .ClearWaitingForAsync(GetPayload<TaskIdRequest>(command).Id, cancellationToken),
            "task.timeline.get" => await scopedServices.GetRequiredService<TaskService>()
                .GetTimelineAsync(GetPayload<TaskTimelineRequest>(command), cancellationToken),
            "task.comment.create" => await scopedServices.GetRequiredService<TaskService>()
                .AddCommentAsync(GetPayload<TaskCommentCreateRequest>(command), cancellationToken),
            "task.comment.delete" => await scopedServices.GetRequiredService<TaskService>()
                .DeleteCommentAsync(GetPayload<TaskCommentDeleteRequest>(command), cancellationToken),
            "task.checklist.list" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .ListAsync(GetPayload<TaskChecklistListRequest>(command).TaskId, cancellationToken),
            "task.checklist.create" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .CreateAsync(GetPayload<TaskChecklistCreateRequest>(command), cancellationToken),
            "task.checklist.update" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .UpdateAsync(GetPayload<TaskChecklistUpdateRequest>(command), cancellationToken),
            "task.checklist.complete" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .SetCompletedAsync(GetPayload<TaskChecklistCompleteRequest>(command), cancellationToken),
            "task.checklist.reorder" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .ReorderAsync(GetPayload<TaskChecklistReorderRequest>(command), cancellationToken),
            "task.checklist.delete" => await scopedServices.GetRequiredService<TaskChecklistService>()
                .DeleteAsync(GetPayload<TaskChecklistDeleteRequest>(command), cancellationToken),
            "task.relation.options" => await scopedServices.GetRequiredService<TaskRelationService>()
                .GetOptionsAsync(GetPayload<TaskRelationOptionsRequest>(command).TaskId, cancellationToken),
            "task.relation.list" => await scopedServices.GetRequiredService<TaskRelationService>()
                .ListAsync(GetPayload<TaskRelationListRequest>(command).TaskId, cancellationToken),
            "task.relation.create" => await scopedServices.GetRequiredService<TaskRelationService>()
                .CreateAsync(GetPayload<TaskRelationCreateRequest>(command), cancellationToken),
            "task.relation.delete" => await scopedServices.GetRequiredService<TaskRelationService>()
                .DeleteAsync(GetPayload<TaskRelationDeleteRequest>(command), cancellationToken),
            "task.attachment.list" => await scopedServices.GetRequiredService<TaskAttachmentService>()
                .ListAsync(GetPayload<TaskAttachmentListRequest>(command).TaskId, cancellationToken),
            "task.attachment.create" => await scopedServices.GetRequiredService<TaskAttachmentService>()
                .CreateAsync(GetPayload<TaskAttachmentCreateRequest>(command), cancellationToken),
            "task.attachment.get" => await scopedServices.GetRequiredService<TaskAttachmentService>()
                .GetAsync(GetPayload<TaskAttachmentGetRequest>(command).AttachmentId, cancellationToken),
            "task.attachment.delete" => await scopedServices.GetRequiredService<TaskAttachmentService>()
                .DeleteAsync(GetPayload<TaskAttachmentDeleteRequest>(command), cancellationToken),
            _ => throw new BridgeException(
                "InvalidMessage",
                $"Unsupported application command type '{command.Type}'.")
        };
    }

    private static TPayload GetPayload<TPayload>(ApplicationCommand command)
    {
        if (command.Payload is null)
        {
            throw new BridgeException("InvalidMessage", "Application command payload is required.");
        }

        var payload = command.Payload.Value.Deserialize<TPayload>(JsonOptions);
        return payload ?? throw new BridgeException("InvalidMessage", "Application command payload is invalid.");
    }
}

public sealed record ApplicationCommand(string Type, JsonElement? Payload);

public sealed record ImageGetRequest(int Id);
