using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class TaskMarkdownExportServiceTests
{
    [Fact]
    public async Task ExportAsync_CurrentResults_PreservesRequestedOrderAndEscapesMarkdown()
    {
        await using var database = await TestDatabase.CreateAsync();
        var directory = CreateTestDirectory();
        var exportPath = Path.Combine(directory, "current-list.md");

        try
        {
            var defaultList = (await database.TaskLists.ListAsync(CancellationToken.None))
                .Single(taskList => taskList.Name == "Default list");
            var supportList = await database.TaskLists.CreateAsync(
                new TaskListCreateRequest("Support"),
                CancellationToken.None);
            var exportedTask = await database.Tasks.CreateAsync(
                CreateRequest(
                    "Investigate | [mail]\nthread <script>",
                    defaultList.Id,
                    priorityCode: "URGENT",
                    sourceCode: "EMAIL",
                    sourceReference: "CASE_42",
                    owner: "Platform | team",
                    responsible: "Ada *Lovelace*",
                    tags: ["customer", "mail|thread"]),
                CancellationToken.None);
            var secondTask = await database.Tasks.CreateAsync(
                CreateRequest("Second task", defaultList.Id),
                CancellationToken.None);
            await database.Tasks.CreateAsync(
                CreateRequest("Different list task", supportList.Id),
                CancellationToken.None);
            var trashedTask = await database.Tasks.CreateAsync(
                CreateRequest("Trashed task", defaultList.Id),
                CancellationToken.None);
            await database.Tasks.MoveToTrashAsync(
                new TaskIdsRequest([trashedTask.Id]),
                CancellationToken.None);
            var checklistService = new TaskChecklistService(database.DbContext);
            await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(exportedTask.Id, "Check | logs <script>"),
                CancellationToken.None);
            var checklist = await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(exportedTask.Id, "Deploy approved fix"),
                CancellationToken.None);
            var completedChecklistItem = checklist.Single(item => item.Text == "Deploy approved fix");
            await checklistService.SetCompletedAsync(
                new TaskChecklistCompleteRequest(
                    exportedTask.Id,
                    completedChecklistItem.Id,
                    true),
                CancellationToken.None);
            await database.Tasks.CompleteAsync(exportedTask.Id, CancellationToken.None);

            var picker = new TestMarkdownExportDestinationPicker(exportPath);
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var preferenceService = CreatePreferenceService(database, directory, loggerFactory);
            var service = new TaskMarkdownExportService(
                database.DbContext,
                picker,
                preferenceService,
                loggerFactory.CreateLogger<TaskMarkdownExportService>());

            var result = await service.ExportAsync(
                new TaskMarkdownExportRequest(
                    [secondTask.Id, exportedTask.Id],
                    defaultList.Id,
                    "All",
                    "Title, descending",
                    [
                        TaskMarkdownExportColumns.Id,
                        TaskMarkdownExportColumns.Title,
                        TaskMarkdownExportColumns.Status,
                        TaskMarkdownExportColumns.Owner,
                        TaskMarkdownExportColumns.Responsible,
                        TaskMarkdownExportColumns.Source,
                        TaskMarkdownExportColumns.Tags,
                        TaskMarkdownExportColumns.Checklist,
                        TaskMarkdownExportColumns.ChecklistItems
                    ]),
                CancellationToken.None);

            Assert.False(result.Cancelled);
            Assert.Equal(2, result.TaskCount);
            Assert.Equal("Default list", result.ScopeName);
            Assert.Equal(Path.GetFullPath(exportPath), result.FilePath);
            Assert.Equal(TaskMarkdownExportKinds.CurrentResults, result.ExportKind);
            Assert.Matches(
                "^okf-todo-results-default-list-[0-9]{8}-[0-9]{4}\\.md$",
                picker.SuggestedFileNames.Single());

            var markdown = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("- Scope: All results in Default list", markdown);
            Assert.Contains("- Ordering: Title, descending", markdown);
            Assert.Contains("| ID | Title | Status | Owner | Responsible | Source | Tags | Checklist progress | Checklist items |", markdown);
            Assert.DoesNotContain("| Type |", markdown);
            Assert.DoesNotContain("| Priority |", markdown);
            Assert.DoesNotContain("| ID | Title | List |", markdown);
            Assert.Contains($"| #{exportedTask.Id} | Investigate \\| \\[mail\\]<br>thread &lt;script&gt; |", markdown);
            Assert.Contains("Platform \\| team", markdown);
            Assert.Contains("Ada \\*Lovelace\\*", markdown);
            Assert.Contains("Email: CASE\\_42", markdown);
            Assert.Contains("mail\\|thread", markdown);
            Assert.Contains("| 1/2 | Open — Check \\| logs &lt;script&gt;<br>Done — Deploy approved fix |", markdown);
            Assert.Contains("Second task", markdown);
            Assert.True(
                markdown.IndexOf("Second task", StringComparison.Ordinal)
                < markdown.IndexOf("Investigate", StringComparison.Ordinal));
            Assert.DoesNotContain("Different list task", markdown);
            Assert.DoesNotContain("Trashed task", markdown);
            Assert.Equal(
                Path.GetFullPath(directory),
                await preferenceService.GetTaskExportDirectoryAsync(CancellationToken.None));

            var bytes = await File.ReadAllBytesAsync(exportPath);
            Assert.False(bytes.Length >= 3
                && bytes[0] == 0xEF
                && bytes[1] == 0xBB
                && bytes[2] == 0xBF);

            var clipboard = await service.CreateHtmlClipboardAsync(
                new TaskMarkdownExportRequest(
                    [secondTask.Id, exportedTask.Id],
                    defaultList.Id,
                    "All",
                    "Title, descending",
                    [
                        TaskMarkdownExportColumns.Id,
                        TaskMarkdownExportColumns.Title,
                        TaskMarkdownExportColumns.Status,
                        TaskMarkdownExportColumns.Owner,
                        TaskMarkdownExportColumns.Responsible,
                        TaskMarkdownExportColumns.Source,
                        TaskMarkdownExportColumns.Tags,
                        TaskMarkdownExportColumns.Checklist,
                        TaskMarkdownExportColumns.ChecklistItems
                    ]),
                CancellationToken.None);

            Assert.Equal(2, clipboard.TaskCount);
            Assert.Equal("Default list", clipboard.ScopeName);
            Assert.Contains("<table", clipboard.Html);
            Assert.Contains(">Title</th>", clipboard.Html);
            Assert.Contains("Investigate | [mail]<br>thread &lt;script&gt;", clipboard.Html);
            Assert.DoesNotContain("<script>", clipboard.Html);
            Assert.Contains("Platform | team", clipboard.Html);
            Assert.Contains("Ada *Lovelace*", clipboard.Html);
            Assert.Contains("Email: CASE_42", clipboard.Html);
            Assert.Contains(">Checklist progress</th>", clipboard.Html);
            Assert.Contains(">Checklist items</th>", clipboard.Html);
            Assert.Contains("<ul style=\"margin:0;padding-left:18px\">", clipboard.Html);
            Assert.Contains("<strong>Open</strong> — Check | logs &lt;script&gt;", clipboard.Html);
            Assert.Contains("<strong>Done</strong> — Deploy approved fix", clipboard.Html);
            Assert.DoesNotContain("Different list task", clipboard.Html);
            Assert.DoesNotContain("Trashed task", clipboard.Html);
            Assert.True(
                clipboard.Html.IndexOf("Second task", StringComparison.Ordinal)
                < clipboard.Html.IndexOf("Investigate", StringComparison.Ordinal));
            Assert.Contains("| ID | Title | Status |", clipboard.PlainText);
            Assert.Contains("Open — Check \\| logs &lt;script&gt;<br>Done — Deploy approved fix", clipboard.PlainText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_RecipeOrder_SortsByOrderedColumnsAndDirections()
    {
        await using var database = await TestDatabase.CreateAsync();
        var directory = CreateTestDirectory();
        var exportPath = Path.Combine(directory, "recipe-order.md");

        try
        {
            var defaultList = (await database.TaskLists.ListAsync(CancellationToken.None))
                .Single(taskList => taskList.Name == "Default list");
            var normalAlpha = await database.Tasks.CreateAsync(
                CreateRequest("Alpha normal", defaultList.Id, priorityCode: "NORMAL"),
                CancellationToken.None);
            var urgent = await database.Tasks.CreateAsync(
                CreateRequest("Urgent task", defaultList.Id, priorityCode: "URGENT"),
                CancellationToken.None);
            var normalZulu = await database.Tasks.CreateAsync(
                CreateRequest("Zulu normal", defaultList.Id, priorityCode: "NORMAL"),
                CancellationToken.None);

            using var loggerFactory = LoggerFactory.Create(_ => { });
            var service = new TaskMarkdownExportService(
                database.DbContext,
                new TestMarkdownExportDestinationPicker(exportPath),
                CreatePreferenceService(database, directory, loggerFactory),
                loggerFactory.CreateLogger<TaskMarkdownExportService>());

            await service.ExportAsync(
                new TaskMarkdownExportRequest(
                    [normalAlpha.Id, normalZulu.Id, urgent.Id],
                    defaultList.Id,
                    "Active",
                    "Title, ascending",
                    [TaskMarkdownExportColumns.Priority, TaskMarkdownExportColumns.Title],
                    TaskMarkdownExportSortModes.Recipe,
                    new Dictionary<string, string>
                    {
                        [TaskMarkdownExportColumns.Priority] = TaskMarkdownExportSortDirections.Ascending,
                        [TaskMarkdownExportColumns.Title] = TaskMarkdownExportSortDirections.Descending
                    }),
                CancellationToken.None);

            var markdown = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("- Ordering: Export recipe: Priority ascending, then Title descending", markdown);
            Assert.Contains("| Priority | Title |", markdown);
            Assert.True(markdown.IndexOf("Urgent task", StringComparison.Ordinal)
                < markdown.IndexOf("Zulu normal", StringComparison.Ordinal));
            Assert.True(markdown.IndexOf("Zulu normal", StringComparison.Ordinal)
                < markdown.IndexOf("Alpha normal", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_WhenTaskIdsContainTrash_RejectsStaleResults()
    {
        await using var database = await TestDatabase.CreateAsync();
        var directory = CreateTestDirectory();
            var exportPath = Path.Combine(directory, "results.md");

        try
        {
            var defaultList = (await database.TaskLists.ListAsync(CancellationToken.None))
                .Single(taskList => taskList.Name == "Default list");
            var activeTask = await database.Tasks.CreateAsync(
                CreateRequest("Active task", defaultList.Id),
                CancellationToken.None);
            var trashedTask = await database.Tasks.CreateAsync(
                CreateRequest("Trashed task", defaultList.Id),
                CancellationToken.None);

            await database.Tasks.MoveToTrashAsync(
                new TaskIdsRequest([trashedTask.Id]),
                CancellationToken.None);

            var picker = new TestMarkdownExportDestinationPicker(exportPath);
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var service = new TaskMarkdownExportService(
                database.DbContext,
                picker,
                CreatePreferenceService(database, directory, loggerFactory),
                loggerFactory.CreateLogger<TaskMarkdownExportService>());

            var exception = await Assert.ThrowsAsync<ValidationException>(() => service.ExportAsync(
                new TaskMarkdownExportRequest(
                    [activeTask.Id, trashedTask.Id],
                    null,
                    "All",
                    "Smart priority, ascending",
                    [TaskMarkdownExportColumns.Title]),
                CancellationToken.None));

            Assert.Equal("taskIds", exception.Field);
            Assert.Contains("changed before export", exception.Message);
            Assert.False(File.Exists(exportPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_WhenNativeDialogIsCancelled_DoesNotWriteAFile()
    {
        await using var database = await TestDatabase.CreateAsync();
        var directory = CreateTestDirectory();

        try
        {
            await database.Tasks.CreateAsync(
                CreateRequest(
                    "Task that remains in the database",
                    (await database.TaskLists.EnsureDefaultListAsync()).Id),
                CancellationToken.None);
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var service = new TaskMarkdownExportService(
                database.DbContext,
                new TestMarkdownExportDestinationPicker(null),
                CreatePreferenceService(database, directory, loggerFactory),
                loggerFactory.CreateLogger<TaskMarkdownExportService>());

            var result = await service.ExportAsync(
                new TaskMarkdownExportRequest(
                    [1],
                    null,
                    "Active",
                    "Smart priority, ascending",
                    [TaskMarkdownExportColumns.Title]),
                CancellationToken.None);

            Assert.True(result.Cancelled);
            Assert.Null(result.FilePath);
            Assert.Empty(Directory.GetFiles(directory, "*.md"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static TaskSaveRequest CreateRequest(
        string title,
        int taskListId,
        string priorityCode = "NORMAL",
        string? sourceCode = null,
        string? sourceReference = null,
        string? owner = null,
        string? responsible = null,
        IReadOnlyCollection<string>? tags = null)
    {
        return new TaskSaveRequest(
            Id: null,
            Title: title,
            TaskTypeCode: "ERROR",
            Body: null,
            BodyFormatCode: "HTML",
            TaskPriorityCode: priorityCode,
            TaskSourceCode: sourceCode,
            SourceReference: sourceReference,
            SourceUrl: null,
            Deadline: new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
            Tags: tags,
            Owner: owner,
            Responsible: responsible,
            TaskListId: taskListId);
    }

    private static AppPreferenceService CreatePreferenceService(
        TestDatabase database,
        string directory,
        ILoggerFactory loggerFactory)
    {
        return new AppPreferenceService(
            database.DbContext,
            new TestPreferencePathProvider(Path.Combine(directory, "preferences.json")),
            loggerFactory.CreateLogger<AppPreferenceService>());
    }

    private static string CreateTestDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class TestMarkdownExportDestinationPicker(string? selectedPath)
        : ITaskMarkdownExportDestinationPicker
    {
        public List<string> SuggestedFileNames { get; } = [];

        public Task<string?> PickAsync(
            string suggestedFileName,
            string? initialDirectory,
            CancellationToken cancellationToken)
        {
            SuggestedFileNames.Add(suggestedFileName);
            return Task.FromResult(selectedPath);
        }
    }

    private sealed class TestPreferencePathProvider(string path) : IAppPreferencePathProvider
    {
        public string GetPreferencesPath() => path;
    }
}
