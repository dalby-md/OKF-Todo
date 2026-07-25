using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task MigrateAsync_CreatesCurrentSchemaAndPreservesExistingData()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Okf-Todo.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "migration-test.db");
        Directory.CreateDirectory(directory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false))
                .Options;

            await using (var dbContext = new AppDbContext(options))
            {
                await dbContext.Database.MigrateAsync();
                dbContext.Issues.Add(new Issue
                {
                    Title = "Preserved across migration checks",
                    Status = "Open",
                    Priority = 0,
                    CreatedUtc = DateTime.UtcNow,
                    ModifiedUtc = DateTime.UtcNow,
                    BodyHtml = "<p>Migration test</p>",
                    BodyMarkdown = "Migration test",
                    EditorMode = "html"
                });
                await dbContext.SaveChangesAsync();
            }

            await using (var dbContext = new AppDbContext(options))
            {
                await dbContext.Database.MigrateAsync();

                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
                Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate"));
                Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
                Assert.Equal(1, await dbContext.Issues.CountAsync());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task AddTaskListsMigration_CreatesDefaultListAndBackfillsExistingTasks()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Okf-Todo.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "task-list-migration-test.db");
        Directory.CreateDirectory(directory);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false))
                .Options;
            await using var dbContext = new AppDbContext(options);
            var migrator = dbContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260725070231_AddTaskStarAndTrash");
            await new LookupSeedService(
                dbContext,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<LookupSeedService>.Instance)
                .SeedAsync();
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "TaskItems"
                    ("Title", "TaskTypeId", "TaskStatusId", "TaskPriorityId", "CreatedAt", "UpdatedAt", "ActivatedAt", "IsStarred")
                VALUES
                    ('Existing task',
                     (SELECT "Id" FROM "TaskTypes" WHERE "Code" = 'ERROR'),
                     (SELECT "Id" FROM "TaskStatuses" WHERE "Code" = 'ACTIVE'),
                     (SELECT "Id" FROM "TaskPriorities" WHERE "Code" = 'NORMAL'),
                     CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 0);
                """);

            await migrator.MigrateAsync();
            dbContext.ChangeTracker.Clear();

            var taskList = await dbContext.TaskLists.SingleAsync();
            var task = await dbContext.TaskItems.SingleAsync();
            Assert.Equal(TaskListService.DefaultListName, taskList.Name);
            Assert.Equal(taskList.Id, task.TaskListId);
            Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }
}
