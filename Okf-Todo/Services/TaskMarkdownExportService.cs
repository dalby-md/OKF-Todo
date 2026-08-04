using System.Globalization;
using System.Net;
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
        var content = await PrepareExportAsync(request, cancellationToken);
        var scope = content.Scope;
        var rows = content.Rows;
        var exportedAt = content.ExportedAt;
        var viewName = content.ViewName;
        var sortDescription = content.SortDescription;
        var selectedColumns = content.SelectedColumns;
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
            var markdown = RenderMarkdown(
                scope,
                viewName,
                sortDescription,
                selectedColumns,
                rows,
                exportedAt);
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

    public async Task<TaskHtmlClipboardResult> CreateHtmlClipboardAsync(
        TaskMarkdownExportRequest request,
        CancellationToken cancellationToken)
    {
        var content = await PrepareExportAsync(request, cancellationToken);
        var plainText = RenderMarkdown(
            content.Scope,
            content.ViewName,
            content.SortDescription,
            content.SelectedColumns,
            content.Rows,
            content.ExportedAt);
        var html = RenderHtml(
            content.Scope,
            content.ViewName,
            content.SortDescription,
            content.SelectedColumns,
            content.Rows,
            content.ExportedAt);

        logger.LogInformation(
            "Prepared {TaskCount} tasks from {ExportScope} as HTML clipboard content.",
            content.Rows.Count,
            content.Scope.Name);

        return new TaskHtmlClipboardResult(
            html,
            plainText,
            content.Rows.Count,
            TaskMarkdownExportKinds.CurrentResults,
            content.Scope.Name);
    }

    public async Task<IReadOnlyCollection<TaskExportChecklistPreviewDto>> GetChecklistPreviewAsync(
        TaskExportChecklistPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var taskIds = NormalizePreviewTaskIds(request.TaskIds);
        if (taskIds.Count == 0)
        {
            return [];
        }

        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.DeletedAt == null && taskIds.Contains(task.Id))
            .OrderBy(task => task.Id)
            .Select(task => new TaskExportChecklistPreviewDto(
                task.Id,
                task.ChecklistItems
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Id)
                    .Select(item => new TaskExportChecklistItemDto(
                        item.Text,
                        item.IsCompleted))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<PreparedTaskExport> PrepareExportAsync(
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

        var selectedColumns = TaskMarkdownExportColumns.Normalize(request.Columns)
            .Where(column => column != TaskMarkdownExportColumns.List || scope.IncludeListColumn)
            .ToList();
        if (selectedColumns.Count == 0)
        {
            throw new ValidationException(
                "Select at least one column available in the current list scope.",
                "columns");
        }

        var sortMode = TaskMarkdownExportSortModes.Normalize(request.SortMode);
        var sortDirections = TaskMarkdownExportSortDirections.Normalize(request.SortDirections);
        if (sortMode == TaskMarkdownExportSortModes.Recipe)
        {
            rows = SortRowsByRecipe(rows, selectedColumns, sortDirections);
        }

        var sortDescription = sortMode == TaskMarkdownExportSortModes.Recipe
            ? BuildRecipeSortDescription(selectedColumns, sortDirections)
            : NormalizeDisplayValue(request.SortDescription, "Current task queue order", 160);

        return new PreparedTaskExport(
            scope,
            NormalizeDisplayValue(request.ViewName, "Current view", 80),
            sortDescription,
            selectedColumns,
            rows,
            DateTime.UtcNow);
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
                task.TaskList.SortOrder,
                task.TaskType!.Name,
                task.TaskType.SortOrder,
                task.TaskStatus!.Name,
                task.TaskStatus.SortOrder,
                task.TaskPriority == null ? null : task.TaskPriority.Name,
                task.TaskPriority == null ? null : task.TaskPriority.SortOrder,
                task.Deadline,
                task.WaitingTargets
                    .Where(waitingFor => waitingFor.ResolvedAt == null)
                    .Select(waitingFor => waitingFor.Label)
                    .SingleOrDefault(),
                task.Owner,
                task.Responsible,
                task.TaskSource == null ? null : task.TaskSource.Name,
                task.TaskSource == null ? null : task.TaskSource.SortOrder,
                task.SourceReference,
                task.Tags
                    .Where(taskTag => taskTag.TaskTag != null)
                    .Select(taskTag => taskTag.TaskTag!.Value)
                    .OrderBy(value => value)
                    .ToList(),
                task.ChecklistItems.Count(item => item.IsCompleted),
                task.ChecklistItems.Count,
                task.ChecklistItems
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Id)
                    .Select(item => new TaskExportChecklistItemDto(
                        item.Text,
                        item.IsCompleted))
                    .ToList(),
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

    private static IReadOnlyList<TaskMarkdownExportRow> SortRowsByRecipe(
        IReadOnlyList<TaskMarkdownExportRow> rows,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyDictionary<string, string> sortDirections)
    {
        return rows
            .OrderBy(row => row, Comparer<TaskMarkdownExportRow>.Create((left, right) =>
                CompareRecipeRows(left, right, selectedColumns, sortDirections)))
            .ToList();
    }

    private static int CompareRecipeRows(
        TaskMarkdownExportRow left,
        TaskMarkdownExportRow right,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyDictionary<string, string> sortDirections)
    {
        foreach (var column in selectedColumns)
        {
            var direction = sortDirections.GetValueOrDefault(column)
                == TaskMarkdownExportSortDirections.Descending
                    ? -1
                    : 1;
            var comparison = CompareRecipeColumn(left, right, column, direction);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.Id.CompareTo(right.Id);
    }

    private static int CompareRecipeColumn(
        TaskMarkdownExportRow left,
        TaskMarkdownExportRow right,
        string column,
        int direction)
    {
        return column switch
        {
            TaskMarkdownExportColumns.Id => left.Id.CompareTo(right.Id) * direction,
            TaskMarkdownExportColumns.Title => CompareText(left.Title, right.Title, direction),
            TaskMarkdownExportColumns.List => CompareLookup(
                left.TaskListSortOrder,
                left.TaskListName,
                right.TaskListSortOrder,
                right.TaskListName,
                direction),
            TaskMarkdownExportColumns.Type => CompareLookup(
                left.TaskTypeSortOrder,
                left.TaskTypeName,
                right.TaskTypeSortOrder,
                right.TaskTypeName,
                direction),
            TaskMarkdownExportColumns.Status => CompareLookup(
                left.TaskStatusSortOrder,
                left.TaskStatusName,
                right.TaskStatusSortOrder,
                right.TaskStatusName,
                direction),
            TaskMarkdownExportColumns.Priority => CompareNullableLookup(
                left.TaskPrioritySortOrder,
                left.TaskPriorityName,
                right.TaskPrioritySortOrder,
                right.TaskPriorityName,
                direction),
            TaskMarkdownExportColumns.Deadline => CompareNullable(left.Deadline, right.Deadline, direction),
            TaskMarkdownExportColumns.WaitingFor => CompareText(left.WaitingFor, right.WaitingFor, direction),
            TaskMarkdownExportColumns.Owner => CompareText(left.Owner, right.Owner, direction),
            TaskMarkdownExportColumns.Responsible => CompareText(left.Responsible, right.Responsible, direction),
            TaskMarkdownExportColumns.Source => CompareNullableLookup(
                left.TaskSourceSortOrder,
                FormatSource(left.TaskSourceName, left.SourceReference),
                right.TaskSourceSortOrder,
                FormatSource(right.TaskSourceName, right.SourceReference),
                direction),
            TaskMarkdownExportColumns.Tags => CompareText(
                string.Join(", ", left.Tags),
                string.Join(", ", right.Tags),
                direction),
            TaskMarkdownExportColumns.Checklist => CompareChecklist(left, right, direction),
            TaskMarkdownExportColumns.ChecklistItems => CompareChecklist(left, right, direction),
            TaskMarkdownExportColumns.Updated => left.UpdatedAt.CompareTo(right.UpdatedAt) * direction,
            _ => 0
        };
    }

    private static int CompareLookup(
        int leftOrder,
        string leftName,
        int rightOrder,
        string rightName,
        int direction)
    {
        var comparison = leftOrder.CompareTo(rightOrder) * direction;
        return comparison != 0 ? comparison : CompareText(leftName, rightName, direction);
    }

    private static int CompareNullableLookup(
        int? leftOrder,
        string? leftName,
        int? rightOrder,
        string? rightName,
        int direction)
    {
        var nullComparison = CompareNulls(leftName, rightName);
        if (nullComparison != 0)
        {
            return nullComparison;
        }

        if (string.IsNullOrWhiteSpace(leftName))
        {
            return 0;
        }

        var orderComparison = leftOrder.GetValueOrDefault().CompareTo(rightOrder.GetValueOrDefault()) * direction;
        return orderComparison != 0 ? orderComparison : CompareText(leftName, rightName, direction);
    }

    private static int CompareText(string? left, string? right, int direction)
    {
        var nullComparison = CompareNulls(left, right);
        if (nullComparison != 0)
        {
            return nullComparison;
        }

        return string.IsNullOrWhiteSpace(left)
            ? 0
            : StringComparer.OrdinalIgnoreCase.Compare(left, right) * direction;
    }

    private static int CompareNullable<T>(T? left, T? right, int direction)
        where T : struct, IComparable<T>
    {
        if (!left.HasValue || !right.HasValue)
        {
            return left.HasValue ? -1 : right.HasValue ? 1 : 0;
        }

        return left.Value.CompareTo(right.Value) * direction;
    }

    private static int CompareChecklist(
        TaskMarkdownExportRow left,
        TaskMarkdownExportRow right,
        int direction)
    {
        if (left.ChecklistCount == 0 || right.ChecklistCount == 0)
        {
            return left.ChecklistCount == 0
                ? right.ChecklistCount == 0 ? 0 : 1
                : -1;
        }

        var leftRatio = (decimal)left.CompletedChecklistCount / left.ChecklistCount;
        var rightRatio = (decimal)right.CompletedChecklistCount / right.ChecklistCount;
        var comparison = leftRatio.CompareTo(rightRatio) * direction;
        return comparison != 0
            ? comparison
            : left.ChecklistCount.CompareTo(right.ChecklistCount) * direction;
    }

    private static int CompareNulls(string? left, string? right)
    {
        var leftMissing = string.IsNullOrWhiteSpace(left);
        var rightMissing = string.IsNullOrWhiteSpace(right);
        return leftMissing == rightMissing ? 0 : leftMissing ? 1 : -1;
    }

    private static string BuildRecipeSortDescription(
        IReadOnlyList<string> columns,
        IReadOnlyDictionary<string, string> sortDirections)
    {
        return "Export recipe: " + string.Join(", then ", columns.Select(column =>
            $"{GetColumnHeader(column)} {(sortDirections.GetValueOrDefault(column) == TaskMarkdownExportSortDirections.Descending ? "descending" : "ascending")}"));
    }

    private static string RenderMarkdown(
        TaskExportScope scope,
        string viewName,
        string sortDescription,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<TaskMarkdownExportRow> rows,
        DateTime exportedAt)
    {
        var scopeDescription = $"{viewName} results in {scope.Name}";
        var columns = selectedColumns.Select(GetColumnHeader).ToList();

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
            var values = selectedColumns
                .Select(column => GetColumnValue(column, row))
                .ToList();
            AppendTableRow(builder, values);
        }

        return builder.ToString();
    }

    private static string RenderHtml(
        TaskExportScope scope,
        string viewName,
        string sortDescription,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<TaskMarkdownExportRow> rows,
        DateTime exportedAt)
    {
        var scopeDescription = $"{viewName} results in {scope.Name}";
        var builder = new StringBuilder();
        builder.Append("<div>");
        builder.Append("<h1>OKF-Todo task export</h1>");
        builder.Append("<ul>");
        AppendHtmlMetadata(builder, "Scope", scopeDescription);
        AppendHtmlMetadata(builder, "Exported", $"{exportedAt:yyyy-MM-dd HH:mm} UTC");
        AppendHtmlMetadata(builder, "Tasks", rows.Count.ToString(CultureInfo.InvariantCulture));
        AppendHtmlMetadata(builder, "Ordering", sortDescription);
        builder.Append("</ul>");
        builder.Append("<table style=\"border-collapse:collapse\"><thead><tr>");
        foreach (var column in selectedColumns)
        {
            builder.Append("<th style=\"border:1px solid #9ca3af;padding:6px 8px;text-align:left;background:#f3f4f6\">");
            builder.Append(WebUtility.HtmlEncode(GetColumnHeader(column)));
            builder.Append("</th>");
        }
        builder.Append("</tr></thead><tbody>");

        foreach (var row in rows)
        {
            builder.Append("<tr>");
            foreach (var column in selectedColumns)
            {
                builder.Append("<td style=\"border:1px solid #d1d5db;padding:6px 8px;vertical-align:top\">");
                if (column == TaskMarkdownExportColumns.ChecklistItems)
                {
                    AppendHtmlChecklistItems(builder, row.ChecklistItems);
                }
                else
                {
                    builder.Append(EncodeHtmlCell(GetColumnValue(column, row)));
                }
                builder.Append("</td>");
            }
            builder.Append("</tr>");
        }

        builder.Append("</tbody></table></div>");
        return builder.ToString();
    }

    private static void AppendHtmlMetadata(StringBuilder builder, string label, string value)
    {
        builder.Append("<li><strong>");
        builder.Append(WebUtility.HtmlEncode(label));
        builder.Append(":</strong> ");
        builder.Append(WebUtility.HtmlEncode(value));
        builder.Append("</li>");
    }

    private static string EncodeHtmlCell(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WebUtility.HtmlEncode(value.Trim())
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);
    }

    private static void AppendHtmlChecklistItems(
        StringBuilder builder,
        IReadOnlyCollection<TaskExportChecklistItemDto> checklistItems)
    {
        if (checklistItems.Count == 0)
        {
            return;
        }

        builder.Append("<ul style=\"margin:0;padding-left:18px\">");
        foreach (var item in checklistItems)
        {
            builder.Append("<li><strong>");
            builder.Append(item.IsCompleted ? "Done" : "Open");
            builder.Append("</strong> — ");
            builder.Append(WebUtility.HtmlEncode(item.Text));
            builder.Append("</li>");
        }
        builder.Append("</ul>");
    }

    private static string GetColumnHeader(string column)
    {
        return column switch
        {
            TaskMarkdownExportColumns.Id => "ID",
            TaskMarkdownExportColumns.Title => "Title",
            TaskMarkdownExportColumns.List => "List",
            TaskMarkdownExportColumns.Type => "Type",
            TaskMarkdownExportColumns.Status => "Status",
            TaskMarkdownExportColumns.Priority => "Priority",
            TaskMarkdownExportColumns.Deadline => "Deadline",
            TaskMarkdownExportColumns.WaitingFor => "Waiting for",
            TaskMarkdownExportColumns.Owner => "Owner",
            TaskMarkdownExportColumns.Responsible => "Responsible",
            TaskMarkdownExportColumns.Source => "Source",
            TaskMarkdownExportColumns.Tags => "Tags",
            TaskMarkdownExportColumns.Checklist => "Checklist progress",
            TaskMarkdownExportColumns.ChecklistItems => "Checklist items",
            TaskMarkdownExportColumns.Updated => "Updated",
            _ => throw new InvalidOperationException($"Unsupported task export column '{column}'.")
        };
    }

    private static string GetColumnValue(string column, TaskMarkdownExportRow row)
    {
        return column switch
        {
            TaskMarkdownExportColumns.Id => $"#{row.Id}",
            TaskMarkdownExportColumns.Title => row.Title,
            TaskMarkdownExportColumns.List => row.TaskListName,
            TaskMarkdownExportColumns.Type => row.TaskTypeName,
            TaskMarkdownExportColumns.Status => row.TaskStatusName,
            TaskMarkdownExportColumns.Priority => row.TaskPriorityName ?? string.Empty,
            TaskMarkdownExportColumns.Deadline =>
                row.Deadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            TaskMarkdownExportColumns.WaitingFor => row.WaitingFor ?? string.Empty,
            TaskMarkdownExportColumns.Owner => row.Owner ?? string.Empty,
            TaskMarkdownExportColumns.Responsible => row.Responsible ?? string.Empty,
            TaskMarkdownExportColumns.Source => FormatSource(row.TaskSourceName, row.SourceReference),
            TaskMarkdownExportColumns.Tags => string.Join(", ", row.Tags),
            TaskMarkdownExportColumns.Checklist => row.ChecklistCount == 0
                ? string.Empty
                : $"{row.CompletedChecklistCount}/{row.ChecklistCount}",
            TaskMarkdownExportColumns.ChecklistItems => FormatChecklistItems(row.ChecklistItems),
            TaskMarkdownExportColumns.Updated => $"{row.UpdatedAt:yyyy-MM-dd HH:mm} UTC",
            _ => throw new InvalidOperationException($"Unsupported task export column '{column}'.")
        };
    }

    private static void AppendTableRow(StringBuilder builder, IEnumerable<string> values)
    {
        builder.Append("| ");
        builder.Append(string.Join(" | ", values.Select(EscapeCell)));
        builder.AppendLine(" |");
    }

    private static string FormatChecklistItems(
        IReadOnlyCollection<TaskExportChecklistItemDto> checklistItems)
    {
        return string.Join(
            '\n',
            checklistItems.Select(item => $"{(item.IsCompleted ? "Done" : "Open")} — {item.Text}"));
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

    private static IReadOnlyList<int> NormalizePreviewTaskIds(IReadOnlyCollection<int>? values)
    {
        if (values is null)
        {
            return [];
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

public static class TaskMarkdownExportColumns
{
    public const string Id = "ID";
    public const string Title = "TITLE";
    public const string List = "LIST";
    public const string Type = "TYPE";
    public const string Status = "STATUS";
    public const string Priority = "PRIORITY";
    public const string Deadline = "DEADLINE";
    public const string WaitingFor = "WAITING_FOR";
    public const string Owner = "OWNER";
    public const string Responsible = "RESPONSIBLE";
    public const string Source = "SOURCE";
    public const string Tags = "TAGS";
    public const string Checklist = "CHECKLIST";
    public const string ChecklistItems = "CHECKLIST_ITEMS";
    public const string Updated = "UPDATED";

    public static readonly string[] All =
    [
        Id,
        Title,
        List,
        Type,
        Status,
        Priority,
        Deadline,
        WaitingFor,
        Owner,
        Responsible,
        Source,
        Tags,
        Checklist,
        ChecklistItems,
        Updated
    ];

    public static readonly string[] Default = All
        .Where(column => column != ChecklistItems)
        .ToArray();

    public static IReadOnlyList<string> Normalize(IReadOnlyCollection<string>? values)
    {
        if (values is null)
        {
            return Default;
        }

        var normalized = values
            .Select(value => value?.Trim().ToUpperInvariant())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();
        if (normalized.Count == 0
            || normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Count
            || normalized.Any(value => !All.Contains(value, StringComparer.Ordinal)))
        {
            throw new ValidationException("Task export columns are invalid.", "columns");
        }

        return normalized;
    }
}

public static class TaskMarkdownExportSortModes
{
    public const string CurrentTaskOrder = "CURRENT_TASK_ORDER";
    public const string Recipe = "RECIPE";

    public static string Normalize(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? CurrentTaskOrder
            : value.Trim().ToUpperInvariant();
        if (normalized is CurrentTaskOrder or Recipe)
        {
            return normalized;
        }

        throw new ValidationException("Task export row order is invalid.", "sortMode");
    }
}

public static class TaskMarkdownExportSortDirections
{
    public const string Ascending = "ASC";
    public const string Descending = "DESC";

    public static IReadOnlyDictionary<string, string> Normalize(
        IReadOnlyDictionary<string, string>? values)
    {
        var normalized = TaskMarkdownExportColumns.All.ToDictionary(
            column => column,
            column => column == TaskMarkdownExportColumns.Updated ? Descending : Ascending,
            StringComparer.Ordinal);

        if (values is null)
        {
            return normalized;
        }

        foreach (var pair in values)
        {
            var column = pair.Key?.Trim().ToUpperInvariant();
            var direction = pair.Value?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(column)
                || !TaskMarkdownExportColumns.All.Contains(column, StringComparer.Ordinal)
                || direction is not (Ascending or Descending))
            {
                throw new ValidationException("Task export sort directions are invalid.", "sortDirections");
            }

            normalized[column] = direction;
        }

        return normalized;
    }
}

public sealed record TaskMarkdownExportRequest(
    IReadOnlyCollection<int>? TaskIds,
    int? TaskListId,
    string? ViewName,
    string? SortDescription,
    IReadOnlyCollection<string>? Columns,
    string? SortMode = null,
    IReadOnlyDictionary<string, string>? SortDirections = null);

public sealed record TaskMarkdownExportResult(
    bool Cancelled,
    string? FilePath,
    int TaskCount,
    string ExportKind,
    string ScopeName);

public sealed record TaskHtmlClipboardResult(
    string Html,
    string PlainText,
    int TaskCount,
    string ExportKind,
    string ScopeName);

public sealed record TaskExportChecklistPreviewRequest(IReadOnlyCollection<int>? TaskIds);

public sealed record TaskExportChecklistPreviewDto(
    int TaskId,
    IReadOnlyCollection<TaskExportChecklistItemDto> Items);

public sealed record TaskExportChecklistItemDto(string Text, bool IsCompleted);

internal sealed record TaskExportScope(int? TaskListId, string Name, bool IncludeListColumn);

internal sealed record PreparedTaskExport(
    TaskExportScope Scope,
    string ViewName,
    string SortDescription,
    IReadOnlyList<string> SelectedColumns,
    IReadOnlyList<TaskMarkdownExportRow> Rows,
    DateTime ExportedAt);

internal sealed record TaskMarkdownExportRow(
    int Id,
    string Title,
    string TaskListName,
    int TaskListSortOrder,
    string TaskTypeName,
    int TaskTypeSortOrder,
    string TaskStatusName,
    int TaskStatusSortOrder,
    string? TaskPriorityName,
    int? TaskPrioritySortOrder,
    DateTime? Deadline,
    string? WaitingFor,
    string? Owner,
    string? Responsible,
    string? TaskSourceName,
    int? TaskSourceSortOrder,
    string? SourceReference,
    IReadOnlyCollection<string> Tags,
    int CompletedChecklistCount,
    int ChecklistCount,
    IReadOnlyCollection<TaskExportChecklistItemDto> ChecklistItems,
    DateTime UpdatedAt);
