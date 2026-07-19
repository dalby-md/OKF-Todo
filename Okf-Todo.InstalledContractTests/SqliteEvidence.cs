using Microsoft.Data.Sqlite;

namespace OkfTodo.InstalledContractTests;

internal static class SqliteEvidence
{
    public static async Task<TaskRow> ReadTaskAsync(string databasePath, int taskId)
    {
        await using var connection = await OpenReadOnlyAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Title, Body, UpdatedAt FROM TaskItems WHERE Id = $taskId;";
        command.Parameters.AddWithValue("$taskId", taskId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidDataException($"Task {taskId} was not found in isolated SQLite database.");
        }

        return new TaskRow(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetString(2));
    }

    public static async Task<long> CountLogEntriesAsync(string databasePath, int taskId, string logTypeCode)
    {
        await using var connection = await OpenReadOnlyAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM TaskLogEntries AS log
            INNER JOIN TaskLogTypes AS type ON type.Id = log.TaskLogTypeId
            WHERE log.TaskId = $taskId AND type.Code = $logTypeCode;
            """;
        command.Parameters.AddWithValue("$taskId", taskId);
        command.Parameters.AddWithValue("$logTypeCode", logTypeCode);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    public static async Task<AttachmentRow> ReadAttachmentAsync(string databasePath, int taskId, string fileName)
    {
        await using var connection = await OpenReadOnlyAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FileName, ContentType, FileSize, Sha256Hash, ContentBlob, Description
            FROM TaskAttachments
            WHERE TaskId = $taskId AND FileName = $fileName;
            """;
        command.Parameters.AddWithValue("$taskId", taskId);
        command.Parameters.AddWithValue("$fileName", fileName);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidDataException($"Attachment '{fileName}' was not found for task {taskId}.");
        }

        return new AttachmentRow(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            (byte[])reader[4],
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    private static async Task<SqliteConnection> OpenReadOnlyAsync(string databasePath)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        await connection.OpenAsync();
        return connection;
    }
}

internal sealed record TaskRow(string Title, string? Body, string UpdatedAt);

internal sealed record AttachmentRow(
    string FileName,
    string? ContentType,
    long FileSize,
    string? Sha256Hash,
    byte[] Content,
    string? Description);
