using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class TaskMarkdownExportServiceTests
{
    [Fact]
    public async Task ExportAsync_CurrentList_WritesCompleteOperationalTableAndEscapesMarkdown()
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
                    "Investigate | [mail]\nthread",
                    defaultList.Id,
                    priorityCode: "URGENT",
                    sourceCode: "EMAIL",
                    sourceReference: "CASE_42",
                    owner: "Platform | team",
                    responsible: "Ada *Lovelace*",
                    tags: ["customer", "mail|thread"]),
                CancellationToken.None);
            await database.Tasks.CreateAsync(
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
                new TaskMarkdownExportRequest(TaskMarkdownExportKinds.CurrentList, defaultList.Id),
                CancellationToken.None);

            Assert.False(result.Cancelled);
            Assert.Equal(2, result.TaskCount);
            Assert.Equal("Default list", result.ScopeName);
            Assert.Equal(Path.GetFullPath(exportPath), result.FilePath);
            Assert.Matches(
                "^okf-todo-tasks-default-list-[0-9]{8}-[0-9]{4}\\.md$",
                picker.SuggestedFileNames.Single());

            var markdown = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("- Scope: All non-Trash tasks in Default list", markdown);
            Assert.Contains("| ID | Title | Type | Status | Priority |", markdown);
            Assert.DoesNotContain("| ID | Title | List |", markdown);
            Assert.Contains($"| #{exportedTask.Id} | Investigate \\| \\[mail\\]<br>thread |", markdown);
            Assert.Contains("Platform \\| team", markdown);
            Assert.Contains("Ada \\*Lovelace\\*", markdown);
            Assert.Contains("Email: CASE\\_42", markdown);
            Assert.Contains("mail\\|thread", markdown);
            Assert.Contains("Second task", markdown);
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
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_StarredAcrossAllLists_IncludesFinishedButExcludesUnstarredAndTrash()
    {
        await using var database = await TestDatabase.CreateAsync();
        var directory = CreateTestDirectory();
        var exportPath = Path.Combine(directory, "starred.md");

        try
        {
            var defaultList = (await database.TaskLists.ListAsync(CancellationToken.None))
                .Single(taskList => taskList.Name == "Default list");
            var supportList = await database.TaskLists.CreateAsync(
                new TaskListCreateRequest("Support"),
                CancellationToken.None);
            var defaultStar = await database.Tasks.CreateAsync(
                CreateRequest("Default star", defaultList.Id),
                CancellationToken.None);
            var supportStar = await database.Tasks.CreateAsync(
                CreateRequest("Completed support star", supportList.Id),
                CancellationToken.None);
            var unstarred = await database.Tasks.CreateAsync(
                CreateRequest("Ordinary task", supportList.Id),
                CancellationToken.None);
            var trashedStar = await database.Tasks.CreateAsync(
                CreateRequest("Trashed star", defaultList.Id),
                CancellationToken.None);

            await database.Tasks.SetStarredManyAsync(
                new TaskBulkStarRequest([defaultStar.Id, supportStar.Id, trashedStar.Id], true),
                CancellationToken.None);
            await database.Tasks.CompleteAsync(supportStar.Id, CancellationToken.None);
            await database.Tasks.MoveToTrashAsync(
                new TaskIdsRequest([trashedStar.Id]),
                CancellationToken.None);

            var picker = new TestMarkdownExportDestinationPicker(exportPath);
            using var loggerFactory = LoggerFactory.Create(_ => { });
            var service = new TaskMarkdownExportService(
                database.DbContext,
                picker,
                CreatePreferenceService(database, directory, loggerFactory),
                loggerFactory.CreateLogger<TaskMarkdownExportService>());

            var result = await service.ExportAsync(
                new TaskMarkdownExportRequest(TaskMarkdownExportKinds.Starred, null),
                CancellationToken.None);

            Assert.False(result.Cancelled);
            Assert.Equal(2, result.TaskCount);
            var markdown = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("- Scope: Starred non-Trash tasks in All lists", markdown);
            Assert.Contains("| ID | Title | List | Type |", markdown);
            Assert.Contains("Default star", markdown);
            Assert.Contains("Completed support star", markdown);
            Assert.Contains("| Support |", markdown);
            Assert.Contains("| Completed |", markdown);
            Assert.DoesNotContain(unstarred.Title, markdown);
            Assert.DoesNotContain("Trashed star", markdown);
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
                new TaskMarkdownExportRequest(TaskMarkdownExportKinds.CurrentList, null),
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
