using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class AppPreferenceServiceTests
{
    [Fact]
    public async Task EditorPreference_AllowsTwoHundredPixelMinimum()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var preferencesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var preferencesPath = Path.Combine(preferencesDirectory, "app-preferences.json");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var now = DateTime.UtcNow;
            dbContext.BodyFormats.Add(new BodyFormat
            {
                Code = "HTML",
                Name = "HTML",
                SortOrder = 10,
                IsActive = true,
                IsSystem = true,
                IsSelected = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            await dbContext.SaveChangesAsync();

            var service = new AppPreferenceService(
                dbContext,
                new TestAppPreferencePathProvider(preferencesPath),
                NullLogger<AppPreferenceService>.Instance);

            var saved = await service.SaveEditorPreferenceAsync(
                new EditorPreferenceSaveRequest("HTML", MarkdownEditTypes.Markdown, 200),
                CancellationToken.None);

            Assert.Equal(200, saved.EditorHeight);

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.SaveEditorPreferenceAsync(
                    new EditorPreferenceSaveRequest("HTML", MarkdownEditTypes.Markdown, 199),
                    CancellationToken.None));

            Assert.Equal("editorHeight", exception.Field);
        }
        finally
        {
            if (Directory.Exists(preferencesDirectory))
            {
                Directory.Delete(preferencesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WindowPreference_PreservesRestoredBoundsWhenSavedAsMaximized()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var preferencesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var preferencesPath = Path.Combine(preferencesDirectory, "app-preferences.json");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            var service = new AppPreferenceService(
                dbContext,
                new TestAppPreferencePathProvider(preferencesPath),
                NullLogger<AppPreferenceService>.Instance);

            var initial = await service.GetWindowPreferenceAsync(CancellationToken.None);
            Assert.True(initial.IsMaximized);
            Assert.Null(initial.Left);
            Assert.Null(initial.Top);
            Assert.Null(initial.Width);
            Assert.Null(initial.Height);

            var restored = await service.SaveWindowPreferenceAsync(
                new WindowPreferenceSaveRequest(120, 80, 1440, 900, false),
                CancellationToken.None);

            Assert.False(restored.IsMaximized);
            Assert.Equal(120, restored.Left);
            Assert.Equal(80, restored.Top);
            Assert.Equal(1440, restored.Width);
            Assert.Equal(900, restored.Height);

            var maximized = await service.SaveWindowPreferenceAsync(
                new WindowPreferenceSaveRequest(null, null, null, null, true),
                CancellationToken.None);

            Assert.True(maximized.IsMaximized);
            Assert.Equal(120, maximized.Left);
            Assert.Equal(80, maximized.Top);
            Assert.Equal(1440, maximized.Width);
            Assert.Equal(900, maximized.Height);

            var loaded = await service.GetWindowPreferenceAsync(CancellationToken.None);
            Assert.True(loaded.IsMaximized);
            Assert.Equal(120, loaded.Left);
            Assert.Equal(80, loaded.Top);
            Assert.Equal(1440, loaded.Width);
            Assert.Equal(900, loaded.Height);
        }
        finally
        {
            if (Directory.Exists(preferencesDirectory))
            {
                Directory.Delete(preferencesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LayoutPreference_PersistsReadyViewSort()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var preferencesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var preferencesPath = Path.Combine(preferencesDirectory, "app-preferences.json");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            var service = new AppPreferenceService(
                dbContext,
                new TestAppPreferencePathProvider(preferencesPath),
                NullLogger<AppPreferenceService>.Instance);

            var saved = await service.SaveLayoutPreferenceAsync(
                new LayoutPreferenceSaveRequest(
                    null,
                    null,
                    null,
                    TaskSortModes: new Dictionary<string, string>
                    {
                        ["ready"] = TaskListSortModes.DueDate
                    },
                    TaskSortDirections: new Dictionary<string, string>
                    {
                        ["ready"] = TaskListSortDirections.Descending
                    }),
                CancellationToken.None);

            Assert.Equal(TaskListSortModes.DueDate, saved.TaskSortModes["ready"]);
            Assert.Equal(TaskListSortDirections.Descending, saved.TaskSortDirections["ready"]);

            var loaded = await service.GetLayoutPreferenceAsync(CancellationToken.None);
            Assert.Equal(TaskListSortModes.DueDate, loaded.TaskSortModes["ready"]);
            Assert.Equal(TaskListSortDirections.Descending, loaded.TaskSortDirections["ready"]);
        }
        finally
        {
            if (Directory.Exists(preferencesDirectory))
            {
                Directory.Delete(preferencesDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TaskExportColumnPreference_PersistsSelectedColumnsForCurrentUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var preferencesDirectory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var preferencesPath = Path.Combine(preferencesDirectory, "app-preferences.json");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            var service = new AppPreferenceService(
                dbContext,
                new TestAppPreferencePathProvider(preferencesPath),
                NullLogger<AppPreferenceService>.Instance);

            var defaults = await service.GetTaskExportColumnPreferenceAsync(CancellationToken.None);
            Assert.Equal(TaskMarkdownExportColumns.All, defaults.Columns);

            var saved = await service.SaveTaskExportColumnPreferenceAsync(
                new TaskExportColumnPreferenceSaveRequest(
                    [TaskMarkdownExportColumns.Title, TaskMarkdownExportColumns.Tags]),
                CancellationToken.None);
            Assert.Equal(
                [TaskMarkdownExportColumns.Title, TaskMarkdownExportColumns.Tags],
                saved.Columns);

            var loaded = await service.GetTaskExportColumnPreferenceAsync(CancellationToken.None);
            Assert.Equal(saved.Columns, loaded.Columns);

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                service.SaveTaskExportColumnPreferenceAsync(
                    new TaskExportColumnPreferenceSaveRequest([]),
                    CancellationToken.None));
            Assert.Equal("columns", exception.Field);
        }
        finally
        {
            if (Directory.Exists(preferencesDirectory))
            {
                Directory.Delete(preferencesDirectory, recursive: true);
            }
        }
    }

    private sealed class TestAppPreferencePathProvider(string preferencesPath) : IAppPreferencePathProvider
    {
        public string GetPreferencesPath()
        {
            return preferencesPath;
        }
    }
}
