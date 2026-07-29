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
        var scope = await ResolveScopeAsync(request.TaskListId, cancellationToken);
        var taskIds = NormalizeTaskIds(request.TaskIds);
        var rows = await LoadRowsAsync(taskIds, scope.TaskListId, cancellationToken);
        if (rows.Count == 0)
        {
            throw new ValidationException(
                "There are no tasks in the current results.",
                "taskIds");
        }

        var exportedAt = DateTime.UtcNow;
        var viewName = NormalizeDisplayValue(request.ViewName, "Current view", 80);
        var sortDescription = NormalizeDisplayValue(
            request.SortDescription,
            "Current view order",
            160);
        var suggestedFileName = BuildSuggestedFileName(scope.Name, exportedAt);
        var initialDirectory = await preferenceService.GetTaskExportDirectoryAsync(cancellationToken);
        var selectedPath = await destinationPicker.PickAsync(
            suggestedFileName,
            initialDirectory,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return new TaskMarkdownExportResult(
                true,
                null,
                rows.Count,
                TaskMarkdownExportKinds.CurrentResults,
                scope.Name);
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
            var markdown = RenderMarkdown(scope, viewName, sortDescription, rows, exportedAt);
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
                TaskMarkdownExportKinds.CurrentResults,
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
        IReadOnlyList<int> taskIds,
        int? taskListId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.DeletedAt == null && taskIds.Contains(task.Id));

        if (taskListId is not null)
        {
            query = query.Where(task => task.TaskListId == taskListId.Value);
        }

        var loadedRows = await query
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

        if (loadedRows.Count != taskIds.Count)
        {
            throw new ValidationException(
                "The current results changed before export. Refresh the task list and try again.",
                "taskIds");
        }

        var rowsById = loadedRows.ToDictionary(row => row.Id);
        return taskIds.Select(taskId => rowsById[taskId]).ToList();
    }

    private static string RenderMarkdown(
        TaskExportScope scope,
        string viewName,
        string sortDescription,
        IReadOnlyList<TaskMarkdownExportRow> rows,
        DateTime exportedAt)
    {
        var scopeDescription = $"{viewName} results in {scope.Name}";
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
        builder.AppendLine($"- Ordering: {EscapeInline(sortDescription)}");
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

    private static IReadOnlyList<int> NormalizeTaskIds(IReadOnlyCollection<int>? values)
    {
        if (values is null || values.Count == 0)
        {
            throw new ValidationException(
                "There are no tasks in the current results.",
                "taskIds");
        }

        if (values.Any(taskId => taskId <= 0) || values.Distinct().Count() != values.Count)
        {
            throw new ValidationException("Task IDs are invalid.", "taskIds");
        }

        return values.ToList();
    }

    private static string NormalizeDisplayValue(
        string? value,
        string fallback,
        int maximumLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? fallback
            : normalized[..Math.Min(normalized.Length, maximumLength)];
    }

    private static string BuildSuggestedFileName(string scopeName, DateTime exportedAt)
    {
        return $"okf-todo-results-{CreateFileNameSlug(scopeName)}-{exportedAt:yyyyMMdd-HHmm}.md";
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
    public const string CurrentResults = "currentResults";
}

public sealed record TaskMarkdownExportRequest(
    IReadOnlyCollection<int>? TaskIds,
    int? TaskListId,
    string? ViewName,
    string? SortDescription);

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
