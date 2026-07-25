using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace OkfTodo.InstalledContractTests;

internal sealed class OkfSqliteMutator(OkfKnowledge knowledge, string databasePath)
{
    public async Task<int> InsertTaskWithAttachmentAsync(
        string title,
        string fileName,
        string contentType,
        byte[] content,
        string description)
    {
        var taskTable = knowledge.RequireTable(
            "task-items.md",
            "TaskItems",
            "Id",
            "Title",
            "TaskListId",
            "TaskTypeId",
            "TaskStatusId",
            "CreatedAt",
            "UpdatedAt",
            "ActivatedAt");
        var attachmentTable = knowledge.RequireTable(
            "task-attachments.md",
            "TaskAttachments",
            "TaskId",
            "FileName",
            "ContentType",
            "FileSize",
            "Sha256Hash",
            "ContentBlob",
            "Description",
            "CreatedAt");
        knowledge.RequireTable("task-types.md", "TaskTypes", "Id", "IsActive", "SortOrder");
        knowledge.RequireTable("task-statuses.md", "TaskStatuses", "Id", "IsActive", "SortOrder");
        knowledge.RequireTable(
            "task-lists.md",
            "TaskLists",
            "Id",
            "Name",
            "SortOrder",
            "CreatedAt",
            "UpdatedAt");

        await using var connection = await OpenReadWriteAsync();
        await using var transaction = connection.BeginTransaction();
        var taskTypeId = await SelectFirstActiveLookupIdAsync(connection, transaction, "TaskTypes");
        var taskStatusId = await SelectFirstActiveLookupIdAsync(connection, transaction, "TaskStatuses");
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var taskListId = await ResolveTaskListIdAsync(connection, transaction, now);

        await using var insertTask = connection.CreateCommand();
        insertTask.Transaction = transaction;
        insertTask.CommandText = $"""
            INSERT INTO {taskTable.Name}
                (Title, TaskListId, TaskTypeId, TaskStatusId, CreatedAt, UpdatedAt, ActivatedAt)
            VALUES
                ($title, $taskListId, $taskTypeId, $taskStatusId, $now, $now, $now);
            SELECT last_insert_rowid();
            """;
        insertTask.Parameters.AddWithValue("$title", title);
        insertTask.Parameters.AddWithValue("$taskListId", taskListId);
        insertTask.Parameters.AddWithValue("$taskTypeId", taskTypeId);
        insertTask.Parameters.AddWithValue("$taskStatusId", taskStatusId);
        insertTask.Parameters.AddWithValue("$now", now);
        var taskId = Convert.ToInt32(await insertTask.ExecuteScalarAsync());

        await using var insertAttachment = connection.CreateCommand();
        insertAttachment.Transaction = transaction;
        insertAttachment.CommandText = $"""
            INSERT INTO {attachmentTable.Name}
                (TaskId, FileName, ContentType, FileSize, Sha256Hash, ContentBlob, Description, CreatedAt)
            VALUES
                ($taskId, $fileName, $contentType, $fileSize, $sha256Hash, $contentBlob, $description, $now);
            """;
        insertAttachment.Parameters.AddWithValue("$taskId", taskId);
        insertAttachment.Parameters.AddWithValue("$fileName", fileName);
        insertAttachment.Parameters.AddWithValue("$contentType", contentType);
        insertAttachment.Parameters.AddWithValue("$fileSize", content.LongLength);
        insertAttachment.Parameters.AddWithValue(
            "$sha256Hash",
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
        insertAttachment.Parameters.AddWithValue("$contentBlob", content);
        insertAttachment.Parameters.AddWithValue("$description", description);
        insertAttachment.Parameters.AddWithValue("$now", now);
        await insertAttachment.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
        return taskId;
    }

    public async Task UpdateTaskTitleAsync(int taskId, string replacementTitle)
    {
        var taskTable = knowledge.RequireTable(
            "task-items.md",
            "TaskItems",
            "Id",
            "Title",
            "UpdatedAt");
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        await using var connection = await OpenReadWriteAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {taskTable.Name}
            SET Title = $title, UpdatedAt = $updatedAt
            WHERE Id = $taskId;
            """;
        command.Parameters.AddWithValue("$title", replacementTitle);
        command.Parameters.AddWithValue("$updatedAt", now);
        command.Parameters.AddWithValue("$taskId", taskId);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidDataException($"OKF-guided SQLite update did not find task {taskId}.");
        }
    }

    private async Task<SqliteConnection> OpenReadWriteAsync()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true,
            Pooling = false
        }.ToString());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<long> SelectFirstActiveLookupIdAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT Id
            FROM {tableName}
            WHERE IsActive = 1
            ORDER BY SortOrder, Id
            LIMIT 1;
            """;
        return Convert.ToInt64(await command.ExecuteScalarAsync()
            ?? throw new InvalidDataException($"Isolated database contains no active lookup in {tableName}."));
    }

    private static async Task<long> ResolveTaskListIdAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string now)
    {
        await using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText =
            """
            SELECT Id
            FROM TaskLists
            ORDER BY CASE WHEN Name = 'Default list' COLLATE NOCASE THEN 0 ELSE 1 END,
                     SortOrder,
                     Id
            LIMIT 1;
            """;
        var resolved = await select.ExecuteScalarAsync();
        if (resolved is not null && resolved is not DBNull)
        {
            return Convert.ToInt64(resolved);
        }

        await using var create = connection.CreateCommand();
        create.Transaction = transaction;
        create.CommandText =
            """
            INSERT INTO TaskLists (Name, SortOrder, CreatedAt, UpdatedAt)
            VALUES ('Default list', 10, $now, $now);
            SELECT last_insert_rowid();
            """;
        create.Parameters.AddWithValue("$now", now);
        return Convert.ToInt64(await create.ExecuteScalarAsync()
            ?? throw new InvalidDataException("Could not create Default list."));
    }
}
