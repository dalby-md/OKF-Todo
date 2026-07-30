using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class DatabaseMaintenanceServiceTests
{
    [Fact]
    public async Task PrepareRestoreAsync_AcceptsAnySourceNameLeavesItUnchangedAndTargetsManagedFile()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var activePath = Path.Combine(directory, "okf-todo.db");
        var sourcePath = Path.Combine(directory, "my-portable-backup.db");
        Directory.CreateDirectory(directory);
        using var loggerFactory = LoggerFactory.Create(_ => { });

        try
        {
            await CreateDatabaseWithIssueAsync(activePath, "Current database");
            await CreateDatabaseWithIssueAsync(sourcePath, "Restored database");
            var sourceBefore = await File.ReadAllBytesAsync(sourcePath);

            DatabaseMaintenanceResult result;
            await using (var dbContext = CreateContext(activePath))
            {
                var preferenceService = new AppPreferenceService(
                    dbContext,
                    new TestPreferencePathProvider(Path.Combine(directory, "preferences.json")),
                    loggerFactory.CreateLogger<AppPreferenceService>());
                var backupService = new DatabaseBackupService(
                    dbContext,
                    new CancelledBackupDestinationPicker(),
                    preferenceService,
                    loggerFactory.CreateLogger<DatabaseBackupService>());
                var service = new DatabaseMaintenanceService(
                    dbContext,
                    new FixedRestoreSourcePicker(sourcePath),
                    preferenceService,
                    backupService,
                    loggerFactory,
                    loggerFactory.CreateLogger<DatabaseMaintenanceService>());

                result = await service.PrepareRestoreAsync();

                Assert.Equal("my-portable-backup.db", result.SourceFileName);
                Assert.Equal(activePath, result.TargetPath);
                Assert.Equal("okf-todo.db", result.TargetFileName);
                Assert.Equal("Current database", await dbContext.Issues.Select(issue => issue.Title).SingleAsync());
            }

            Assert.Equal(sourceBefore, await File.ReadAllBytesAsync(sourcePath));
            PendingDatabaseOperationApplier.Apply(
                activePath,
                loggerFactory.CreateLogger("DatabaseMaintenanceServiceTests"));

            await using var restoredContext = CreateContext(activePath);
            Assert.Equal(
                "Restored database",
                await restoredContext.Issues.Select(issue => issue.Title).SingleAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Theory]
    [InlineData(DatabaseResetModes.Empty, 0)]
    [InlineData(DatabaseResetModes.Sample, 50)]
    public async Task PrepareResetAsync_AppliesOnlyAtRestartAndCreatesSafetyBackup(
        string mode,
        int expectedTaskCount)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.Tests",
            Guid.NewGuid().ToString("N"));
        var activePath = Path.Combine(directory, "okf-todo.db");
        Directory.CreateDirectory(directory);
        using var loggerFactory = LoggerFactory.Create(_ => { });

        try
        {
            DatabaseMaintenanceResult result;
            await using (var dbContext = CreateContext(activePath))
            {
                await dbContext.Database.MigrateAsync();
                dbContext.Issues.Add(new Issue
                {
                    Title = "Current database evidence",
                    Status = "Open",
                    CreatedUtc = DateTime.UtcNow,
                    ModifiedUtc = DateTime.UtcNow,
                    BodyHtml = "<p>Current</p>",
                    BodyMarkdown = "Current",
                    EditorMode = "html"
                });
                await dbContext.SaveChangesAsync();

                var preferenceService = new AppPreferenceService(
                    dbContext,
                    new TestPreferencePathProvider(Path.Combine(directory, "preferences.json")),
                    loggerFactory.CreateLogger<AppPreferenceService>());
                var backupService = new DatabaseBackupService(
                    dbContext,
                    new CancelledBackupDestinationPicker(),
                    preferenceService,
                    loggerFactory.CreateLogger<DatabaseBackupService>());
                var service = new DatabaseMaintenanceService(
                    dbContext,
                    new CancelledRestoreSourcePicker(),
                    preferenceService,
                    backupService,
                    loggerFactory,
                    loggerFactory.CreateLogger<DatabaseMaintenanceService>());

                await Assert.ThrowsAsync<ValidationException>(() =>
                    service.PrepareResetAsync(new DatabaseResetRequest(mode, "reset")));

                result = await service.PrepareResetAsync(
                    new DatabaseResetRequest(
                        mode,
                        DatabaseMaintenanceService.ResetConfirmation.ToLowerInvariant()));

                Assert.True(result.RequiresRestart);
                Assert.Equal("okf-todo.db", result.TargetFileName);
                Assert.True(File.Exists(result.SafetyBackupPath));
                Assert.Equal(1, await dbContext.Issues.CountAsync());
            }

            PendingDatabaseOperationApplier.Apply(
                activePath,
                loggerFactory.CreateLogger("DatabaseMaintenanceServiceTests"));

            await using (var replacementContext = CreateContext(activePath))
            {
                Assert.Equal(0, await replacementContext.Issues.CountAsync());
                Assert.Equal(expectedTaskCount, await replacementContext.TaskItems.CountAsync());
                Assert.Equal(
                    expectedTaskCount,
                    await replacementContext.TaskItems.CountAsync(task => task.IsSampleData));
                Assert.Empty(await replacementContext.Database.GetPendingMigrationsAsync());
            }

            await using var safetyContext = CreateContext(result.SafetyBackupPath!);
            Assert.Equal(1, await safetyContext.Issues.CountAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static AppDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task CreateDatabaseWithIssueAsync(string databasePath, string title)
    {
        await using var dbContext = CreateContext(databasePath);
        await dbContext.Database.MigrateAsync();
        dbContext.Issues.Add(new Issue
        {
            Title = title,
            Status = "Open",
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
            BodyHtml = $"<p>{title}</p>",
            BodyMarkdown = title,
            EditorMode = "html"
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class TestPreferencePathProvider(string path) : IAppPreferencePathProvider
    {
        public string GetPreferencesPath() => path;
    }

    private sealed class CancelledBackupDestinationPicker : IBackupDestinationPicker
    {
        public Task<string?> PickAsync(
            string suggestedFileName,
            string? initialDirectory,
            CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class CancelledRestoreSourcePicker : IDatabaseRestoreSourcePicker
    {
        public Task<string?> PickAsync(
            string? initialDirectory,
            CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class FixedRestoreSourcePicker(string sourcePath) : IDatabaseRestoreSourcePicker
    {
        public Task<string?> PickAsync(
            string? initialDirectory,
            CancellationToken cancellationToken) => Task.FromResult<string?>(sourcePath);
    }
}
