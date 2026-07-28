using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class TaskMarkdownExportService(
    AppDbContext dbContext,
    ITaskMarkdownExportDestinationPicker destinationPicker,
    AppPreferenceService preferenceService,
    ILogger<TaskMarkdownExportService> logger)
{
    public async Task<TaskMarkdownExportResult> ExportAsync(
        TaskMarkdownExportRequest request,
        CancellationToken cancellationToken)
    {
        var exportKind = NormalizeExportKind(request.ExportKind);
        var scope = await ResolveScopeAsync(request.TaskListId, cancellationToken);
        var rows = await LoadRowsAsync(exportKind, scope.TaskListId, cancellationToken);
        if (rows.Count == 0)
        {
            throw new ValidationException(
                "There are no tasks in the selected export scope.",
                "exportKind");
        }

        var exportedAt = DateTime.UtcNow;
        var suggestedFileName = BuildSuggestedFileName(exportKind, scope.Name, exportedAt);
        var initialDirectory = await preferenceService.GetTaskExportDirectoryAsync(cancellationToken);
        var selectedPath = await destinationPicker.PickAsync(
            suggestedFileName,
            initialDirectory,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return new TaskMarkdownExportResult(true, null, rows.Count, exportKind, scope.Name);
        }

        var destinationPath = EnsureMarkdownExtension(Path.GetFullPath(selectedPath));
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new ValidationException("Export destination is invalid.", "destinationPath");
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var markdown = RenderMarkdown(exportKind, scope, rows, exportedAt);
            await File.WriteAllTextAsync(
                temporaryPath,
                markdown,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken);
            File.Move(temporaryPath, destinationPath, overwrite: true);

            try
            {
                await preferenceService.SaveTaskExportDirectoryAsync(
                    destinationDirectory,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Task export succeeded, but the export directory preference could not be saved.");
            }

            logger.LogInformation(
                "Exported {TaskCount} tasks from {ExportScope} to Markdown at {ExportPath}.",
                rows.Count,
                scope.Name,
                destinationPath);

            return new TaskMarkdownExportResult(
                false,
                destinationPath,
                rows.Count,
                exportKind,
                scope.Name);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not export tasks from {ExportScope} to {ExportPath}.",
                scope.Name,
                destinationPath);
            throw new BridgeException("TaskExportFailed", "Could not create the Markdown task export.");
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private async Task<TaskExportScope> ResolveScopeAsync(
        int? taskListId,
        CancellationToken cancellationToken)
    {
        if (taskListId is null)
        {
            return new TaskExportScope(null, "All lists", IncludeListColumn: true);
        }

        if (taskListId <= 0)
        {
            throw new ValidationException("Task list is invalid.", "taskListId");
        }

        var listName = await dbContext.TaskLists
            .AsNoTracking()
            .Where(taskList => taskList.Id == taskListId.Value)
            .Select(taskList => taskList.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (listName is null)
        {
            throw new BridgeException("NotFound", "Task list was not found.");
        }

        return new TaskExportScope(taskListId, listName, IncludeListColumn: false);
    }

    private async Task<IReadOnlyList<TaskMarkdownExportRow>> LoadRowsAsync(
        string exportKind,
        int? taskListId,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var query = dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.DeletedAt == null);

        if (taskListId is not null)
        {
            query = query.Where(task => task.TaskListId == taskListId.Value);
        }

        if (exportKind == TaskMarkdownExportKinds.Starred)
        {
            query = query.Where(task => task.IsStarred);
        }

        return await query
            .OrderBy(task => task.TaskStatus!.Code == TaskStatusCodes.Active
                    && task.Deadline != null
                    && task.Deadline < today
                ? 0
                : task.TaskStatus.Code == TaskStatusCodes.Active
                    && task.TaskPriority != null
                    && task.TaskPriority.Code == TaskPriorityCodes.Urgent
                ? 1
                : task.TaskStatus.Code == TaskStatusCodes.Active
                    && !task.WaitingTargets.Any(waitingFor => waitingFor.ResolvedAt == null)
                    && (task.TaskPriority == null || task.TaskPriority.Code != TaskPriorityCodes.CanWait)
                ? 2
                : task.TaskStatus.Code == TaskStatusCodes.Active
                    && task.WaitingTargets.Any(waitingFor => waitingFor.ResolvedAt == null)
                ? 3
                : task.TaskStatus.Code == TaskStatusCodes.Active
                    && task.TaskPriority != null
                    && task.TaskPriority.Code == TaskPriorityCodes.CanWait
                ? 4
                : 5)
            .ThenBy(task => task.Deadline == null)
            .ThenBy(task => task.Deadline)
            .ThenByDescending(task => task.UpdatedAt)
            .Select(task => new TaskMarkdownExportRow(
                task.Id,
                task.Title,
                task.TaskList!.Name,
                task.TaskType!.Name,
                task.TaskStatus!.Name,
                task.TaskPriority == null ? null : task.TaskPriority.Name,
                task.Deadline,
                task.WaitingTargets
                    .Where(waitingFor => waitingFor.ResolvedAt == null)
                    .Select(waitingFor => waitingFor.Label)
                    .SingleOrDefault(),
                task.Owner,
                task.Responsible,
                task.TaskSource == null ? null : task.TaskSource.Name,
                task.SourceReference,
                task.Tags
                    .Where(taskTag => taskTag.TaskTag != null)
                    .Select(taskTag => taskTag.TaskTag!.Value)
                    .OrderBy(value => value)
                    .ToList(),
                task.ChecklistItems.Count(item => item.IsCompleted),
                task.ChecklistItems.Count,
                task.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    private static string RenderMarkdown(
        string exportKind,
        TaskExportScope scope,
        IReadOnlyList<TaskMarkdownExportRow> rows,
        DateTime exportedAt)
    {
        var scopeDescription = exportKind == TaskMarkdownExportKinds.Starred
            ? $"Starred non-Trash tasks in {scope.Name}"
            : $"All non-Trash tasks in {scope.Name}";
        var columns = new List<string>
        {
            "ID",
            "Title"
        };
        if (scope.IncludeListColumn)
        {
            columns.Add("List");
        }
        columns.AddRange(
        [
            "Type",
            "Status",
            "Priority",
            "Deadline",
            "Waiting for",
            "Owner",
            "Responsible",
            "Source",
            "Tags",
            "Checklist",
            "Updated"
        ]);

        var builder = new StringBuilder();
        builder.AppendLine("# OKF-Todo task export");
        builder.AppendLine();
        builder.AppendLine($"- Scope: {EscapeInline(scopeDescription)}");
        builder.AppendLine($"- Exported: {exportedAt:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine($"- Tasks: {rows.Count}");
        builder.AppendLine("- Ordering: Smart priority, then earliest deadline, then most recently updated");
        builder.AppendLine();
        AppendTableRow(builder, columns);
        AppendTableRow(builder, columns.Select(_ => "---"));

        foreach (var row in rows)
        {
            var values = new List<string>
            {
                $"#{row.Id}",
                row.Title
            };
            if (scope.IncludeListColumn)
            {
                values.Add(row.TaskListName);
            }
            values.AddRange(
            [
                row.TaskTypeName,
                row.TaskStatusName,
                row.TaskPriorityName ?? string.Empty,
                row.Deadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                row.WaitingFor ?? string.Empty,
                row.Owner ?? string.Empty,
                row.Responsible ?? string.Empty,
                FormatSource(row.TaskSourceName, row.SourceReference),
                string.Join(", ", row.Tags),
                row.ChecklistCount == 0
                    ? string.Empty
                    : $"{row.CompletedChecklistCount}/{row.ChecklistCount}",
                $"{row.UpdatedAt:yyyy-MM-dd HH:mm} UTC"
            ]);
            AppendTableRow(builder, values);
        }

        return builder.ToString();
    }

    private static void AppendTableRow(StringBuilder builder, IEnumerable<string> values)
    {
        builder.Append("| ");
        builder.Append(string.Join(" | ", values.Select(EscapeCell)));
        builder.AppendLine(" |");
    }

    private static string EscapeCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);
    }

    private static string EscapeInline(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
    }

    private static string FormatSource(string? sourceName, string? sourceReference)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return sourceReference?.Trim() ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(sourceReference)
            ? sourceName
            : $"{sourceName}: {sourceReference.Trim()}";
    }

    private static string NormalizeExportKind(string? value)
    {
        var normalized = value?.Trim();
        return normalized switch
        {
            TaskMarkdownExportKinds.CurrentList => TaskMarkdownExportKinds.CurrentList,
            TaskMarkdownExportKinds.Starred => TaskMarkdownExportKinds.Starred,
            _ => throw new ValidationException("Export kind is invalid.", "exportKind")
        };
    }

    private static string BuildSuggestedFileName(string exportKind, string scopeName, DateTime exportedAt)
    {
        var kind = exportKind == TaskMarkdownExportKinds.Starred ? "starred" : "tasks";
        return $"okf-todo-{kind}-{CreateFileNameSlug(scopeName)}-{exportedAt:yyyyMMdd-HHmm}.md";
    }

    private static string CreateFileNameSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "tasks" : slug;
    }

    private static string EnsureMarkdownExtension(string path)
    {
        return string.IsNullOrEmpty(Path.GetExtension(path))
            ? $"{path}.md"
            : path;
    }
}

public static class TaskMarkdownExportKinds
{
    public const string CurrentList = "currentList";
    public const string Starred = "starred";
}

public sealed record TaskMarkdownExportRequest(string? ExportKind, int? TaskListId);

public sealed record TaskMarkdownExportResult(
    bool Cancelled,
    string? FilePath,
    int TaskCount,
    string ExportKind,
    string ScopeName);

internal sealed record TaskExportScope(int? TaskListId, string Name, bool IncludeListColumn);

internal sealed record TaskMarkdownExportRow(
    int Id,
    string Title,
    string TaskListName,
    string TaskTypeName,
    string TaskStatusName,
    string? TaskPriorityName,
    DateTime? Deadline,
    string? WaitingFor,
    string? Owner,
    string? Responsible,
    string? TaskSourceName,
    string? SourceReference,
    IReadOnlyCollection<string> Tags,
    int CompletedChecklistCount,
    int ChecklistCount,
    DateTime UpdatedAt);
