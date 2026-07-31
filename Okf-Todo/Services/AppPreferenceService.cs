using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class AppPreferenceService(
    AppDbContext dbContext,
    IAppPreferencePathProvider pathProvider,
    ILogger<AppPreferenceService> logger)
{
    private const string DefaultBodyFormatCode = "HTML";
    private const string DefaultMarkdownEditType = MarkdownEditTypes.Markdown;
    private const string DefaultLayoutMode = LayoutPreferenceModes.Auto;
    private const string DefaultTaskFilterLayout = TaskFilterLayoutModes.Compact;
    private const string DefaultColorScheme = ColorSchemes.Light;
    private const string DefaultFontSize = FontSizeModes.Standard;
    private const bool DefaultShowSourceFields = false;
    private const bool DefaultShowOwner = false;
    private const bool DefaultShowResponsible = false;
    private const bool DefaultShowRelationships = false;
    private const bool DefaultAllowEditingCompletedTasks = false;
    private const bool DefaultAllowEditingCancelledTasks = false;
    private const bool DefaultTaskSelectionCoachmarkSeen = false;
    private const int DefaultEditorHeight = 360;
    private const int MinimumEditorHeight = 200;
    private const int MaximumEditorHeight = 1800;
    private const double MinimumTaskListWidth = 160;
    private const double MaximumTaskListWidth = 2400;
    private const double MinimumTaskListHeight = 120;
    private const double MaximumTaskListHeight = 1800;
    private const int MinimumWindowWidth = 640;
    private const int MaximumWindowWidth = 10000;
    private const int MinimumWindowHeight = 480;
    private const int MaximumWindowHeight = 10000;
    private const int MinimumWindowCoordinate = -20000;
    private const int MaximumWindowCoordinate = 20000;
    private const bool DefaultWindowIsMaximized = true;
    private static readonly string[] TaskViews = ["active", "ready", "starred", "attention", "waiting", "completed", "all", "trash"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<EditorPreferenceDto> GetEditorPreferenceAsync(CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        var code = await NormalizeOrDefaultBodyFormatCodeAsync(
            preferences.EditorBodyFormatCode,
            cancellationToken);
        var markdownEditType = NormalizeOrDefaultMarkdownEditType(preferences.MarkdownEditType);
        var editorHeight = NormalizeOrDefaultEditorHeight(preferences.EditorHeight);

        return new EditorPreferenceDto(code, markdownEditType, editorHeight);
    }

    public async Task<EditorPreferenceDto> SaveEditorPreferenceAsync(
        EditorPreferenceSaveRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        var code = request.BodyFormatCode is null
            ? await NormalizeOrDefaultBodyFormatCodeAsync(preferences.EditorBodyFormatCode, cancellationToken)
            : await NormalizeBodyFormatCodeAsync(request.BodyFormatCode, cancellationToken);
        var markdownEditType = request.MarkdownEditType is null
            ? NormalizeOrDefaultMarkdownEditType(preferences.MarkdownEditType)
            : NormalizeMarkdownEditType(request.MarkdownEditType);
        var editorHeight = request.EditorHeight is null
            ? NormalizeOrDefaultEditorHeight(preferences.EditorHeight)
            : NormalizeEditorHeight(request.EditorHeight, "editorHeight");

        preferences = preferences with
        {
            EditorBodyFormatCode = code,
            MarkdownEditType = markdownEditType,
            EditorHeight = editorHeight
        };
        await WritePreferencesAsync(preferences, cancellationToken);

        logger.LogInformation(
            "Saved editor preference with body format {BodyFormatCode}, markdown edit type {MarkdownEditType}, and editor height {EditorHeight}.",
            code,
            markdownEditType,
            editorHeight);

        return new EditorPreferenceDto(code, markdownEditType, editorHeight);
    }

    public async Task<LayoutPreferenceDto> GetLayoutPreferenceAsync(CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        var layoutMode = NormalizeOrDefaultLayoutMode(preferences.LayoutMode);
        var colorScheme = NormalizeOrDefaultColorScheme(preferences.ColorScheme);
        var fontSize = NormalizeOrDefaultFontSize(preferences.FontSize);
        var taskSortModes = NormalizeStoredTaskSortModes(preferences.TaskSortModes);
        var taskSortDirections = NormalizeStoredTaskSortDirections(preferences.TaskSortDirections, taskSortModes);

        return new LayoutPreferenceDto(
            preferences.TaskListWidth,
            preferences.TaskListHeight,
            layoutMode,
            preferences.ShowSourceFields ?? DefaultShowSourceFields,
            preferences.ShowOwner ?? DefaultShowOwner,
            preferences.ShowResponsible ?? DefaultShowResponsible,
            preferences.ShowRelationships ?? DefaultShowRelationships,
            preferences.AllowEditingCompletedTasks ?? DefaultAllowEditingCompletedTasks,
            preferences.AllowEditingCancelledTasks ?? DefaultAllowEditingCancelledTasks,
            preferences.TaskSelectionCoachmarkSeen ?? DefaultTaskSelectionCoachmarkSeen,
            colorScheme,
            taskSortModes,
            taskSortDirections,
            NormalizeTaskListScope(preferences.TaskListScope),
            NormalizeOrDefaultTaskFilterLayout(preferences.TaskFilterLayout),
            fontSize);
    }

    public async Task<LayoutPreferenceDto> SaveLayoutPreferenceAsync(
        LayoutPreferenceSaveRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        var taskListWidth = request.TaskListWidth is null
            ? preferences.TaskListWidth
            : NormalizeLayoutValue(
                request.TaskListWidth,
                MinimumTaskListWidth,
                MaximumTaskListWidth,
                "taskListWidth");
        var taskListHeight = request.TaskListHeight is null
            ? preferences.TaskListHeight
            : NormalizeLayoutValue(
                request.TaskListHeight,
                MinimumTaskListHeight,
                MaximumTaskListHeight,
                "taskListHeight");
        var layoutMode = request.LayoutMode is null
            ? NormalizeOrDefaultLayoutMode(preferences.LayoutMode)
            : NormalizeLayoutMode(request.LayoutMode);
        var showSourceFields = request.ShowSourceFields
            ?? preferences.ShowSourceFields
            ?? DefaultShowSourceFields;
        var showOwner = request.ShowOwner
            ?? preferences.ShowOwner
            ?? DefaultShowOwner;
        var showResponsible = request.ShowResponsible
            ?? preferences.ShowResponsible
            ?? DefaultShowResponsible;
        var showRelationships = request.ShowRelationships
            ?? preferences.ShowRelationships
            ?? DefaultShowRelationships;
        var allowEditingCompletedTasks = request.AllowEditingCompletedTasks
            ?? preferences.AllowEditingCompletedTasks
            ?? DefaultAllowEditingCompletedTasks;
        var allowEditingCancelledTasks = request.AllowEditingCancelledTasks
            ?? preferences.AllowEditingCancelledTasks
            ?? DefaultAllowEditingCancelledTasks;
        var taskSelectionCoachmarkSeen = request.TaskSelectionCoachmarkSeen
            ?? preferences.TaskSelectionCoachmarkSeen
            ?? DefaultTaskSelectionCoachmarkSeen;
        var colorScheme = request.ColorScheme is null
            ? NormalizeOrDefaultColorScheme(preferences.ColorScheme)
            : NormalizeColorScheme(request.ColorScheme);
        var fontSize = request.FontSize is null
            ? NormalizeOrDefaultFontSize(preferences.FontSize)
            : NormalizeFontSize(request.FontSize);
        var taskSortModes = MergeTaskSortModes(
            NormalizeStoredTaskSortModes(preferences.TaskSortModes),
            request.TaskSortModes);
        var taskSortDirections = MergeTaskSortDirections(
            NormalizeStoredTaskSortDirections(preferences.TaskSortDirections, taskSortModes),
            request.TaskSortDirections);
        var taskListScope = request.TaskListScope is null
            ? NormalizeTaskListScope(preferences.TaskListScope)
            : NormalizeTaskListScope(request.TaskListScope);
        var taskFilterLayout = request.TaskFilterLayout is null
            ? NormalizeOrDefaultTaskFilterLayout(preferences.TaskFilterLayout)
            : NormalizeTaskFilterLayout(request.TaskFilterLayout);

        preferences = preferences with
        {
            TaskListWidth = taskListWidth,
            TaskListHeight = taskListHeight,
            LayoutMode = layoutMode,
            ShowSourceFields = showSourceFields,
            ShowOwner = showOwner,
            ShowResponsible = showResponsible,
            ShowRelationships = showRelationships,
            AllowEditingCompletedTasks = allowEditingCompletedTasks,
            AllowEditingCancelledTasks = allowEditingCancelledTasks,
            TaskSelectionCoachmarkSeen = taskSelectionCoachmarkSeen,
            ColorScheme = colorScheme,
            TaskSortModes = taskSortModes,
            TaskSortDirections = taskSortDirections,
            TaskListScope = taskListScope,
            TaskFilterLayout = taskFilterLayout,
            FontSize = fontSize
        };
        await WritePreferencesAsync(preferences, cancellationToken);

        logger.LogInformation(
            "Saved layout preference with task list width {TaskListWidth}, height {TaskListHeight}, mode {LayoutMode}, task filter layout {TaskFilterLayout}, font size {FontSize}, source fields {ShowSourceFields}, owner {ShowOwner}, responsible {ShowResponsible}, relationships {ShowRelationships}, completed editing {AllowEditingCompletedTasks}, cancelled editing {AllowEditingCancelledTasks}, task selection coach mark seen {TaskSelectionCoachmarkSeen}, color scheme {ColorScheme}, task sort modes {TaskSortModes}, task sort directions {TaskSortDirections}, and task list scope {TaskListScope}.",
            taskListWidth,
            taskListHeight,
            layoutMode,
            taskFilterLayout,
            fontSize,
            showSourceFields,
            showOwner,
            showResponsible,
            showRelationships,
            allowEditingCompletedTasks,
            allowEditingCancelledTasks,
            taskSelectionCoachmarkSeen,
            colorScheme,
            taskSortModes,
            taskSortDirections,
            taskListScope);

        return new LayoutPreferenceDto(
            taskListWidth,
            taskListHeight,
            layoutMode,
            showSourceFields,
            showOwner,
            showResponsible,
            showRelationships,
            allowEditingCompletedTasks,
            allowEditingCancelledTasks,
            taskSelectionCoachmarkSeen,
            colorScheme,
            taskSortModes,
            taskSortDirections,
            taskListScope,
            taskFilterLayout,
            fontSize);
    }

    public async Task<WindowPreferenceDto> GetWindowPreferenceAsync(CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        return NormalizeOrDefaultWindowPreference(preferences);
    }

    public async Task<WindowPreferenceDto> SaveWindowPreferenceAsync(
        WindowPreferenceSaveRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        var isMaximized = request.IsMaximized;
        var left = request.Left is null
            ? preferences.WindowLeft
            : NormalizeWindowCoordinate(request.Left, "left");
        var top = request.Top is null
            ? preferences.WindowTop
            : NormalizeWindowCoordinate(request.Top, "top");
        var width = request.Width is null
            ? preferences.WindowWidth
            : NormalizeWindowSize(
                request.Width,
                MinimumWindowWidth,
                MaximumWindowWidth,
                "width");
        var height = request.Height is null
            ? preferences.WindowHeight
            : NormalizeWindowSize(
                request.Height,
                MinimumWindowHeight,
                MaximumWindowHeight,
                "height");

        if (!isMaximized && (left is null || top is null || width is null || height is null))
        {
            throw new ValidationException("Window preference is incomplete.", "window");
        }

        preferences = preferences with
        {
            WindowLeft = left,
            WindowTop = top,
            WindowWidth = width,
            WindowHeight = height,
            WindowIsMaximized = isMaximized
        };
        await WritePreferencesAsync(preferences, cancellationToken);

        logger.LogInformation(
            "Saved window preference with left {WindowLeft}, top {WindowTop}, width {WindowWidth}, height {WindowHeight}, maximized {WindowIsMaximized}.",
            left,
            top,
            width,
            height,
            isMaximized);

        return new WindowPreferenceDto(left, top, width, height, isMaximized);
    }

    public async Task<string?> GetBackupDirectoryAsync(CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(preferences.BackupDirectory)
            && Directory.Exists(preferences.BackupDirectory)
                ? Path.GetFullPath(preferences.BackupDirectory)
                : null;
    }

    public async Task SaveBackupDirectoryAsync(
        string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupDirectory) || !Directory.Exists(backupDirectory))
        {
            throw new ValidationException("Backup directory is invalid.", "backupDirectory");
        }

        var normalizedDirectory = Path.GetFullPath(backupDirectory);
        var preferences = await ReadPreferencesAsync(cancellationToken);
        await WritePreferencesAsync(
            preferences with { BackupDirectory = normalizedDirectory },
            cancellationToken);

        logger.LogInformation("Saved backup directory preference {BackupDirectory}.", normalizedDirectory);
    }

    public async Task<string?> GetTaskExportDirectoryAsync(CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(preferences.TaskExportDirectory)
            && Directory.Exists(preferences.TaskExportDirectory)
                ? Path.GetFullPath(preferences.TaskExportDirectory)
                : null;
    }

    public async Task SaveTaskExportDirectoryAsync(
        string taskExportDirectory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskExportDirectory) || !Directory.Exists(taskExportDirectory))
        {
            throw new ValidationException("Task export directory is invalid.", "taskExportDirectory");
        }

        var normalizedDirectory = Path.GetFullPath(taskExportDirectory);
        var preferences = await ReadPreferencesAsync(cancellationToken);
        await WritePreferencesAsync(
            preferences with { TaskExportDirectory = normalizedDirectory },
            cancellationToken);

        logger.LogInformation("Saved task export directory preference {TaskExportDirectory}.", normalizedDirectory);
    }

    public async Task<TaskExportColumnPreferenceDto> GetTaskExportColumnPreferenceAsync(
        CancellationToken cancellationToken)
    {
        var preferences = await ReadPreferencesAsync(cancellationToken);
        return new TaskExportColumnPreferenceDto(
            NormalizeOrDefaultTaskExportColumns(preferences.TaskExportColumns));
    }

    public async Task<TaskExportColumnPreferenceDto> SaveTaskExportColumnPreferenceAsync(
        TaskExportColumnPreferenceSaveRequest request,
        CancellationToken cancellationToken)
    {
        var columns = TaskMarkdownExportColumns.Normalize(request.Columns);
        var preferences = await ReadPreferencesAsync(cancellationToken);
        await WritePreferencesAsync(
            preferences with { TaskExportColumns = columns },
            cancellationToken);

        logger.LogInformation(
            "Saved task export column preference {TaskExportColumns}.",
            columns);

        return new TaskExportColumnPreferenceDto(columns);
    }

    private async Task<StoredPreferences> ReadPreferencesAsync(CancellationToken cancellationToken)
    {
        var preferencesPath = pathProvider.GetPreferencesPath();
        if (!File.Exists(preferencesPath))
        {
            return CreateDefaultPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(preferencesPath);
            return await JsonSerializer.DeserializeAsync<StoredPreferences>(
                stream,
                JsonOptions,
                cancellationToken) ?? CreateDefaultPreferences();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Could not read app preferences from {PreferencesPath}.", preferencesPath);
            return CreateDefaultPreferences();
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not read app preferences from {PreferencesPath}.", preferencesPath);
            return CreateDefaultPreferences();
        }
    }

    private async Task WritePreferencesAsync(StoredPreferences preferences, CancellationToken cancellationToken)
    {
        var preferencesPath = pathProvider.GetPreferencesPath();
        var preferencesDirectory = Path.GetDirectoryName(preferencesPath);

        if (!string.IsNullOrWhiteSpace(preferencesDirectory))
        {
            Directory.CreateDirectory(preferencesDirectory);
        }

        await File.WriteAllTextAsync(
            preferencesPath,
            JsonSerializer.Serialize(preferences, JsonOptions),
            cancellationToken);
    }

    private async Task<string> NormalizeOrDefaultBodyFormatCodeAsync(
        string? code,
        CancellationToken cancellationToken)
    {
        try
        {
            return await NormalizeBodyFormatCodeAsync(code, cancellationToken);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception, "Stored editor body format preference is invalid.");
            return await NormalizeBodyFormatCodeAsync(DefaultBodyFormatCode, cancellationToken);
        }
    }

    private async Task<string> NormalizeBodyFormatCodeAsync(string? code, CancellationToken cancellationToken)
    {
        var normalizedCode = string.IsNullOrWhiteSpace(code)
            ? DefaultBodyFormatCode
            : code.Trim().ToUpperInvariant();

        var exists = await dbContext.BodyFormats
            .AsNoTracking()
            .AnyAsync(format => format.Code == normalizedCode, cancellationToken);

        if (!exists)
        {
            throw new ValidationException("Editor body format is invalid.", "bodyFormatCode");
        }

        return normalizedCode;
    }

    private static double NormalizeLayoutValue(
        double? value,
        double minimum,
        double maximum,
        string field)
    {
        if (value is null || !double.IsFinite(value.Value) || value.Value < minimum || value.Value > maximum)
        {
            throw new ValidationException("Layout preference is invalid.", field);
        }

        return Math.Round(value.Value);
    }

    private static StoredPreferences CreateDefaultPreferences()
    {
        return new StoredPreferences(
            DefaultBodyFormatCode,
            DefaultMarkdownEditType,
            DefaultEditorHeight,
            null,
            null,
            DefaultLayoutMode,
            DefaultShowSourceFields,
            DefaultShowOwner,
            DefaultShowResponsible,
            DefaultShowRelationships,
            DefaultAllowEditingCompletedTasks,
            DefaultAllowEditingCancelledTasks,
            null,
            null,
            null,
            null,
            DefaultWindowIsMaximized,
            DefaultColorScheme,
            TaskFilterLayout: DefaultTaskFilterLayout,
            FontSize: DefaultFontSize);
    }

    private static string? NormalizeTaskListScope(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (string.Equals(normalized, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            return "ALL";
        }

        return int.TryParse(normalized, out var id) && id > 0
            ? id.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;
    }

    private static string NormalizeOrDefaultLayoutMode(string? layoutMode)
    {
        try
        {
            return NormalizeLayoutMode(layoutMode);
        }
        catch (ValidationException)
        {
            return DefaultLayoutMode;
        }
    }

    private static string NormalizeOrDefaultTaskFilterLayout(string? taskFilterLayout)
    {
        try
        {
            return NormalizeTaskFilterLayout(taskFilterLayout);
        }
        catch (ValidationException)
        {
            return DefaultTaskFilterLayout;
        }
    }

    private static string NormalizeOrDefaultColorScheme(string? colorScheme)
    {
        try
        {
            return NormalizeColorScheme(colorScheme);
        }
        catch (ValidationException)
        {
            return DefaultColorScheme;
        }
    }

    private static string NormalizeOrDefaultFontSize(string? fontSize)
    {
        try
        {
            return NormalizeFontSize(fontSize);
        }
        catch (ValidationException)
        {
            return DefaultFontSize;
        }
    }

    private static string NormalizeOrDefaultMarkdownEditType(string? markdownEditType)
    {
        try
        {
            return NormalizeMarkdownEditType(markdownEditType);
        }
        catch (ValidationException)
        {
            return DefaultMarkdownEditType;
        }
    }

    private static int NormalizeOrDefaultEditorHeight(int? editorHeight)
    {
        try
        {
            return NormalizeEditorHeight(editorHeight, "editorHeight");
        }
        catch (ValidationException)
        {
            return DefaultEditorHeight;
        }
    }

    private static int NormalizeEditorHeight(int? editorHeight, string field)
    {
        if (editorHeight is null || editorHeight < MinimumEditorHeight || editorHeight > MaximumEditorHeight)
        {
            throw new ValidationException("Editor height preference is invalid.", field);
        }

        return editorHeight.Value;
    }

    private static string NormalizeMarkdownEditType(string? markdownEditType)
    {
        var normalizedMarkdownEditType = string.IsNullOrWhiteSpace(markdownEditType)
            ? DefaultMarkdownEditType
            : markdownEditType.Trim().ToUpperInvariant();

        if (normalizedMarkdownEditType is MarkdownEditTypes.Markdown or MarkdownEditTypes.Wysiwyg)
        {
            return normalizedMarkdownEditType;
        }

        throw new ValidationException("Markdown edit type is invalid.", "markdownEditType");
    }

    private static string NormalizeLayoutMode(string? layoutMode)
    {
        var normalizedLayoutMode = string.IsNullOrWhiteSpace(layoutMode)
            ? DefaultLayoutMode
            : layoutMode.Trim().ToUpperInvariant();

        if (normalizedLayoutMode is LayoutPreferenceModes.Auto
            or LayoutPreferenceModes.SideBySide
            or LayoutPreferenceModes.Stacked)
        {
            return normalizedLayoutMode;
        }

        throw new ValidationException("Layout mode is invalid.", "layoutMode");
    }

    private static string NormalizeTaskFilterLayout(string? taskFilterLayout)
    {
        var normalizedTaskFilterLayout = string.IsNullOrWhiteSpace(taskFilterLayout)
            ? DefaultTaskFilterLayout
            : taskFilterLayout.Trim().ToUpperInvariant();

        if (normalizedTaskFilterLayout is TaskFilterLayoutModes.Compact
            or TaskFilterLayoutModes.Expanded)
        {
            return normalizedTaskFilterLayout;
        }

        throw new ValidationException("Task filter layout is invalid.", "taskFilterLayout");
    }

    private IReadOnlyList<string> NormalizeOrDefaultTaskExportColumns(
        IReadOnlyCollection<string>? columns)
    {
        try
        {
            return TaskMarkdownExportColumns.Normalize(columns);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception, "Stored task export column preference is invalid.");
            return TaskMarkdownExportColumns.All;
        }
    }

    private static string NormalizeColorScheme(string? colorScheme)
    {
        var normalizedColorScheme = string.IsNullOrWhiteSpace(colorScheme)
            ? DefaultColorScheme
            : colorScheme.Trim().ToUpperInvariant();

        if (normalizedColorScheme is ColorSchemes.Light or ColorSchemes.Dark)
        {
            return normalizedColorScheme;
        }

        throw new ValidationException("Color scheme is invalid.", "colorScheme");
    }

    private static string NormalizeFontSize(string? fontSize)
    {
        var normalizedFontSize = string.IsNullOrWhiteSpace(fontSize)
            ? DefaultFontSize
            : fontSize.Trim().ToUpperInvariant();

        if (normalizedFontSize is FontSizeModes.Small
            or FontSizeModes.Standard
            or FontSizeModes.Large)
        {
            return normalizedFontSize;
        }

        throw new ValidationException("Font size preference is invalid.", "fontSize");
    }

    private static IReadOnlyDictionary<string, string> NormalizeStoredTaskSortModes(
        IReadOnlyDictionary<string, string>? taskSortModes)
    {
        var normalized = TaskViews.ToDictionary(
            view => view,
            _ => TaskListSortModes.Attention,
            StringComparer.OrdinalIgnoreCase);

        if (taskSortModes is null)
        {
            return normalized;
        }

        foreach (var pair in taskSortModes)
        {
            var view = pair.Key.Trim().ToLowerInvariant();
            if (!normalized.ContainsKey(view))
            {
                continue;
            }

            try
            {
                normalized[view] = NormalizeTaskSortMode(pair.Value);
            }
            catch (ValidationException)
            {
                normalized[view] = TaskListSortModes.Attention;
            }
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, string> MergeTaskSortModes(
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string>? requested)
    {
        var merged = current.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        if (requested is null)
        {
            return merged;
        }

        foreach (var pair in requested)
        {
            var view = pair.Key.Trim().ToLowerInvariant();
            if (!TaskViews.Contains(view, StringComparer.Ordinal))
            {
                throw new ValidationException("Task sort view is invalid.", "taskSortModes");
            }

            merged[view] = NormalizeTaskSortMode(pair.Value);
        }

        return merged;
    }

    private static string NormalizeTaskSortMode(string? taskSortMode)
    {
        var normalized = string.IsNullOrWhiteSpace(taskSortMode)
            ? TaskListSortModes.Attention
            : taskSortMode.Trim().ToUpperInvariant();

        if (TaskListSortModes.All.Contains(normalized, StringComparer.Ordinal))
        {
            return normalized;
        }

        throw new ValidationException("Task sort mode is invalid.", "taskSortModes");
    }

    private static IReadOnlyDictionary<string, string> NormalizeStoredTaskSortDirections(
        IReadOnlyDictionary<string, string>? taskSortDirections,
        IReadOnlyDictionary<string, string> taskSortModes)
    {
        var normalized = TaskViews.ToDictionary(
            view => view,
            view => InferLegacyTaskSortDirection(taskSortModes.GetValueOrDefault(view)),
            StringComparer.OrdinalIgnoreCase);

        if (taskSortDirections is null)
        {
            return normalized;
        }

        foreach (var pair in taskSortDirections)
        {
            var view = pair.Key.Trim().ToLowerInvariant();
            if (!normalized.ContainsKey(view))
            {
                continue;
            }

            try
            {
                normalized[view] = NormalizeTaskSortDirection(pair.Value);
            }
            catch (ValidationException)
            {
                normalized[view] = InferLegacyTaskSortDirection(taskSortModes.GetValueOrDefault(view));
            }
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, string> MergeTaskSortDirections(
        IReadOnlyDictionary<string, string> current,
        IReadOnlyDictionary<string, string>? requested)
    {
        var merged = current.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        if (requested is null)
        {
            return merged;
        }

        foreach (var pair in requested)
        {
            var view = pair.Key.Trim().ToLowerInvariant();
            if (!TaskViews.Contains(view, StringComparer.Ordinal))
            {
                throw new ValidationException("Task sort view is invalid.", "taskSortDirections");
            }

            merged[view] = NormalizeTaskSortDirection(pair.Value);
        }

        return merged;
    }

    private static string NormalizeTaskSortDirection(string? taskSortDirection)
    {
        var normalized = string.IsNullOrWhiteSpace(taskSortDirection)
            ? TaskListSortDirections.Ascending
            : taskSortDirection.Trim().ToUpperInvariant();

        if (TaskListSortDirections.All.Contains(normalized, StringComparer.Ordinal))
        {
            return normalized;
        }

        throw new ValidationException("Task sort direction is invalid.", "taskSortDirections");
    }

    private static string InferLegacyTaskSortDirection(string? taskSortMode)
    {
        return taskSortMode is TaskListSortModes.RecentlyUpdated
            or TaskListSortModes.NewestCreated
            or TaskListSortModes.TitleDescending
            ? TaskListSortDirections.Descending
            : TaskListSortDirections.Ascending;
    }

    private WindowPreferenceDto NormalizeOrDefaultWindowPreference(StoredPreferences preferences)
    {
        try
        {
            var isMaximized = preferences.WindowIsMaximized ?? DefaultWindowIsMaximized;
            if (preferences.WindowLeft is null
                || preferences.WindowTop is null
                || preferences.WindowWidth is null
                || preferences.WindowHeight is null)
            {
                return new WindowPreferenceDto(null, null, null, null, isMaximized);
            }

            return new WindowPreferenceDto(
                NormalizeWindowCoordinate(preferences.WindowLeft, "left"),
                NormalizeWindowCoordinate(preferences.WindowTop, "top"),
                NormalizeWindowSize(preferences.WindowWidth, MinimumWindowWidth, MaximumWindowWidth, "width"),
                NormalizeWindowSize(preferences.WindowHeight, MinimumWindowHeight, MaximumWindowHeight, "height"),
                isMaximized);
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception, "Stored window preference is invalid.");
            return new WindowPreferenceDto(null, null, null, null, DefaultWindowIsMaximized);
        }
    }

    private static int NormalizeWindowCoordinate(int? value, string field)
    {
        if (value is null || value < MinimumWindowCoordinate || value > MaximumWindowCoordinate)
        {
            throw new ValidationException("Window preference is invalid.", field);
        }

        return value.Value;
    }

    private static int NormalizeWindowSize(int? value, int minimum, int maximum, string field)
    {
        if (value is null || value < minimum || value > maximum)
        {
            throw new ValidationException("Window preference is invalid.", field);
        }

        return value.Value;
    }
}

public interface IAppPreferencePathProvider
{
    string GetPreferencesPath();
}

public sealed class AppPreferencePathProvider : IAppPreferencePathProvider
{
    public string GetPreferencesPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var preferencesDirectory = Path.Combine(appDataPath, "Okf-Todo");

        return Path.Combine(preferencesDirectory, "app-preferences.json");
    }
}

public sealed record EditorPreferenceDto(string BodyFormatCode, string MarkdownEditType, int EditorHeight);

public sealed record EditorPreferenceSaveRequest(string? BodyFormatCode, string? MarkdownEditType, int? EditorHeight);

public static class MarkdownEditTypes
{
    public const string Markdown = "MARKDOWN";
    public const string Wysiwyg = "WYSIWYG";
}

public static class LayoutPreferenceModes
{
    public const string Auto = "AUTO";
    public const string SideBySide = "SIDE_BY_SIDE";
    public const string Stacked = "STACKED";
}

public static class TaskFilterLayoutModes
{
    public const string Compact = "COMPACT";
    public const string Expanded = "EXPANDED";
}

public static class ColorSchemes
{
    public const string Light = "LIGHT";
    public const string Dark = "DARK";
}

public static class FontSizeModes
{
    public const string Small = "SMALL";
    public const string Standard = "STANDARD";
    public const string Large = "LARGE";
}

public static class TaskListSortModes
{
    public const string Attention = "ATTENTION";
    public const string Priority = "PRIORITY";
    public const string DueDate = "DUE_DATE";
    public const string WaitingLongest = "WAITING_LONGEST";
    public const string RecentlyUpdated = "RECENTLY_UPDATED";
    public const string StaleFirst = "STALE_FIRST";
    public const string NewestCreated = "NEWEST_CREATED";
    public const string OldestCreated = "OLDEST_CREATED";
    public const string TitleAscending = "TITLE_ASC";
    public const string TitleDescending = "TITLE_DESC";
    public const string TaskType = "TASK_TYPE";
    public const string Status = "STATUS";

    public static readonly string[] All =
    [
        Attention,
        Priority,
        DueDate,
        WaitingLongest,
        RecentlyUpdated,
        StaleFirst,
        NewestCreated,
        OldestCreated,
        TitleAscending,
        TitleDescending,
        TaskType,
        Status
    ];
}

public static class TaskListSortDirections
{
    public const string Ascending = "ASC";
    public const string Descending = "DESC";

    public static readonly string[] All = [Ascending, Descending];
}

public sealed record LayoutPreferenceDto(
    double? TaskListWidth,
    double? TaskListHeight,
    string LayoutMode,
    bool ShowSourceFields,
    bool ShowOwner,
    bool ShowResponsible,
    bool ShowRelationships,
    bool AllowEditingCompletedTasks,
    bool AllowEditingCancelledTasks,
    bool TaskSelectionCoachmarkSeen,
    string ColorScheme,
    IReadOnlyDictionary<string, string> TaskSortModes,
    IReadOnlyDictionary<string, string> TaskSortDirections,
    string? TaskListScope,
    string TaskFilterLayout,
    string FontSize);

public sealed record LayoutPreferenceSaveRequest(
    double? TaskListWidth,
    double? TaskListHeight,
    string? LayoutMode,
    bool? ShowSourceFields = null,
    bool? ShowOwner = null,
    bool? ShowResponsible = null,
    bool? ShowRelationships = null,
    bool? AllowEditingCompletedTasks = null,
    bool? AllowEditingCancelledTasks = null,
    bool? TaskSelectionCoachmarkSeen = null,
    string? ColorScheme = null,
    IReadOnlyDictionary<string, string>? TaskSortModes = null,
    IReadOnlyDictionary<string, string>? TaskSortDirections = null,
    string? TaskListScope = null,
    string? TaskFilterLayout = null,
    string? FontSize = null);

public sealed record WindowPreferenceDto(int? Left, int? Top, int? Width, int? Height, bool IsMaximized);

public sealed record WindowPreferenceSaveRequest(int? Left, int? Top, int? Width, int? Height, bool IsMaximized);

public sealed record TaskExportColumnPreferenceDto(IReadOnlyList<string> Columns);

public sealed record TaskExportColumnPreferenceSaveRequest(IReadOnlyCollection<string>? Columns);

internal sealed record StoredPreferences(
    string? EditorBodyFormatCode,
    string? MarkdownEditType,
    int? EditorHeight,
    double? TaskListWidth,
    double? TaskListHeight,
    string? LayoutMode,
    bool? ShowSourceFields,
    bool? ShowOwner,
    bool? ShowResponsible,
    bool? ShowRelationships,
    bool? AllowEditingCompletedTasks,
    bool? AllowEditingCancelledTasks,
    int? WindowLeft,
    int? WindowTop,
    int? WindowWidth,
    int? WindowHeight,
    bool? WindowIsMaximized,
    string? ColorScheme,
    string? BackupDirectory = null,
    string? TaskExportDirectory = null,
    IReadOnlyDictionary<string, string>? TaskSortModes = null,
    IReadOnlyDictionary<string, string>? TaskSortDirections = null,
    bool? TaskSelectionCoachmarkSeen = null,
    string? TaskListScope = null,
    IReadOnlyCollection<string>? TaskExportColumns = null,
    string? TaskFilterLayout = null,
    string? FontSize = null);
