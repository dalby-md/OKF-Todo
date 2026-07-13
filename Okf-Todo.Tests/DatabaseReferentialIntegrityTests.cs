using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Photino.Okf_Todo.Data;

namespace Okf_Todo.Tests;

public sealed class DatabaseReferentialIntegrityTests
{
    [Fact]
    public async Task FreshDatabase_EnablesForeignKeyEnforcement()
    {
        await using var database = await FreshDatabase.CreateAsync();
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";

        var enabled = Convert.ToInt64(await command.ExecuteScalarAsync());

        Assert.Equal(1, enabled);
    }

    [Fact]
    public async Task FreshDatabase_RejectsOrphanedTaskComment()
    {
        await using var database = await FreshDatabase.CreateAsync();
        await using var command = database.Connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "TaskComments" ("TaskId", "CommentText", "CreatedAt")
            VALUES (999999, 'orphan', '2026-07-13T00:00:00Z');
            """;

        var exception = await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(19, exception.SqliteErrorCode);
    }

    [Fact]
    public async Task FreshDatabase_CascadesOwnedRowsAndRestrictsUsedLookups()
    {
        await using var database = await FreshDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var taskType = CreateLookup<TaskType>("ERROR", "Error", now);
        var taskStatus = CreateLookup<Photino.Okf_Todo.Data.TaskStatus>("ACTIVE", "Active", now);
        var task = new TaskItem
        {
            Title = "Integrity test",
            TaskType = taskType,
            TaskStatus = taskStatus,
            CreatedAt = now,
            UpdatedAt = now,
            Comments =
            [
                new TaskComment
                {
                    CommentText = "Owned comment",
                    CreatedAt = now
                }
            ]
        };

        database.DbContext.TaskItems.Add(task);
        await database.DbContext.SaveChangesAsync();

        var lookupDelete = database.Connection.CreateCommand();
        lookupDelete.CommandText = "DELETE FROM \"TaskTypes\" WHERE \"Id\" = $id;";
        lookupDelete.Parameters.AddWithValue("$id", taskType.Id);
        await using (lookupDelete)
        {
            var exception = await Assert.ThrowsAsync<SqliteException>(() => lookupDelete.ExecuteNonQueryAsync());
            Assert.Equal(19, exception.SqliteErrorCode);
        }

        await using var taskDelete = database.Connection.CreateCommand();
        taskDelete.CommandText = "DELETE FROM \"TaskItems\" WHERE \"Id\" = $id;";
        taskDelete.Parameters.AddWithValue("$id", task.Id);
        await taskDelete.ExecuteNonQueryAsync();

        database.DbContext.ChangeTracker.Clear();
        Assert.False(await database.DbContext.TaskComments.AnyAsync(comment => comment.TaskId == task.Id));
    }

    [Fact]
    public async Task FreshDatabase_EnforcesUniqueAndCheckConstraints()
    {
        await using var database = await FreshDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var taskType = CreateLookup<TaskType>("ERROR", "Error", now);
        var taskStatus = CreateLookup<Photino.Okf_Todo.Data.TaskStatus>("ACTIVE", "Active", now);
        var relationType = CreateLookup<TaskRelationType>("RELATED_TO", "Related to", now);
        relationType.ReverseName = "Related to";
        var task = new TaskItem
        {
            Title = "Constraint test",
            TaskType = taskType,
            TaskStatus = taskStatus,
            CreatedAt = now,
            UpdatedAt = now,
            WaitingTargets =
            [
                new TaskWaitingFor
                {
                    Label = "First target",
                    WaitingSince = now,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ]
        };

        database.DbContext.AddRange(task, relationType);
        await database.DbContext.SaveChangesAsync();

        await using var duplicateWaitingTarget = database.Connection.CreateCommand();
        duplicateWaitingTarget.CommandText = """
            INSERT INTO "TaskWaitingFors" ("TaskId", "Label", "WaitingSince", "CreatedAt", "UpdatedAt")
            VALUES ($taskId, 'Second target', $now, $now, $now);
            """;
        duplicateWaitingTarget.Parameters.AddWithValue("$taskId", task.Id);
        duplicateWaitingTarget.Parameters.AddWithValue("$now", now);

        var uniqueException = await Assert.ThrowsAsync<SqliteException>(
            () => duplicateWaitingTarget.ExecuteNonQueryAsync());
        Assert.Equal(19, uniqueException.SqliteErrorCode);

        await using var selfRelation = database.Connection.CreateCommand();
        selfRelation.CommandText = """
            INSERT INTO "TaskRelations" ("SourceTaskId", "TargetTaskId", "TaskRelationTypeId", "CreatedAt")
            VALUES ($taskId, $taskId, $relationTypeId, $now);
            """;
        selfRelation.Parameters.AddWithValue("$taskId", task.Id);
        selfRelation.Parameters.AddWithValue("$relationTypeId", relationType.Id);
        selfRelation.Parameters.AddWithValue("$now", now);

        var checkException = await Assert.ThrowsAsync<SqliteException>(() => selfRelation.ExecuteNonQueryAsync());
        Assert.Equal(19, checkException.SqliteErrorCode);
    }

    private static TLookup CreateLookup<TLookup>(string code, string name, DateTime now)
        where TLookup : LookupEntity, new()
    {
        return new TLookup
        {
            Code = code,
            Name = name,
            IsActive = true,
            IsSystem = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class FreshDatabase : IAsyncDisposable
    {
        private FreshDatabase(SqliteConnection connection, AppDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
        }

        public SqliteConnection Connection { get; }

        public AppDbContext DbContext { get; }

        public static async Task<FreshDatabase> CreateAsync()
        {
            var connection = new SqliteConnection(
                DatabasePathProvider.CreateConnectionString(":memory:", pooling: false));
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new FreshDatabase(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
