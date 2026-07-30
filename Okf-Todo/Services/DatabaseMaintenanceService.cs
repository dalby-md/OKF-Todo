using System.Data;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class DatabaseMaintenanceService(
    AppDbContext dbContext,
    IDatabaseRestoreSourcePicker restoreSourcePicker,
    AppPreferenceService preferenceService,
    DatabaseBackupService backupService,
    ILoggerFactory loggerFactory,
    ILogger<DatabaseMaintenanceService> logger)
{
    public const string ResetConfirmation = "RESET DATABASE";
    private const string PendingMarkerFileName = ".okf-todo.pending-operation.json";
    private const string PendingDatabasePrefix = ".okf-todo.pending-";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DatabaseMaintenanceResult> PrepareRestoreAsync(
        CancellationToken cancellationToken = default)
    {
        var initialDirectory = await preferenceService.GetBackupDirectoryAsync(cancellationToken);
        var selectedPath = await restoreSourcePicker.PickAsync(initialDirectory, cancellationToken);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return DatabaseMaintenanceResult.CancelledResult;
        }

        var sourcePath = Path.GetFullPath(selectedPath);
        var activePath = GetActiveDatabasePath();
        if (string.Equals(sourcePath, activePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Choose a database file other than the active OKF-Todo database.",
                "sourcePath");
        }

        if (!File.Exists(sourcePath))
        {
            throw new ValidationException("The selected database file does not exist.", "sourcePath");
        }

        var stagingPath = CreateStagingPath(activePath);
        try
        {
            await CopyDatabaseAsync(sourcePath, stagingPath, cancellationToken);
            await ValidateSupportedDatabaseAsync(stagingPath, cancellationToken);
            await MigrateDatabaseAsync(stagingPath, cancellationToken);
            await ValidateCurrentDatabaseAsync(stagingPath, cancellationToken);

            return await CompletePreparationAsync(
                activePath,
                stagingPath,
                operation: DatabaseOperationTypes.Restore,
                sourceFileName: Path.GetFileName(sourcePath),
                cancellationToken);
        }
        catch
        {
            DeleteIfExists(stagingPath);
            throw;
        }
    }

    public async Task<DatabaseMaintenanceResult> PrepareResetAsync(
        DatabaseResetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
            request.Confirmation?.Trim(),
            ResetConfirmation,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"Type {ResetConfirmation} to confirm the database reset.",
                "confirmation");
        }

        var mode = request.Mode?.Trim().ToUpperInvariant();
        if (mode is not (DatabaseResetModes.Empty or DatabaseResetModes.Sample))
        {
            throw new ValidationException("Database reset mode is invalid.", "mode");
        }

        var activePath = GetActiveDatabasePath();
        var stagingPath = CreateStagingPath(activePath);
        try
        {
            await CreateFreshDatabaseAsync(
                stagingPath,
                includeSampleData: mode == DatabaseResetModes.Sample,
                cancellationToken);
            await ValidateCurrentDatabaseAsync(stagingPath, cancellationToken);

            return await CompletePreparationAsync(
                activePath,
                stagingPath,
                operation: mode == DatabaseResetModes.Sample
                    ? DatabaseOperationTypes.ResetSample
                    : DatabaseOperationTypes.ResetEmpty,
                sourceFileName: null,
                cancellationToken);
        }
        catch
        {
            DeleteIfExists(stagingPath);
            throw;
        }
    }

    private async Task<DatabaseMaintenanceResult> CompletePreparationAsync(
        string activePath,
        string stagingPath,
        string operation,
        string? sourceFileName,
        CancellationToken cancellationToken)
    {
        var databaseDirectory = Path.GetDirectoryName(activePath)
            ?? throw new InvalidOperationException("The active database path has no parent directory.");
        var markerPath = Path.Combine(databaseDirectory, PendingMarkerFileName);
        if (File.Exists(markerPath))
        {
            throw new ValidationException(
                "A database restore or reset is already prepared. Close and reopen OKF-Todo first.",
                "database");
        }

        var safetyDirectory = Path.Combine(databaseDirectory, "SafetyBackups");
        Directory.CreateDirectory(safetyDirectory);
        var safetyBackupPath = Path.Combine(
            safetyDirectory,
            $"okf-todo-before-{GetSafetyBackupLabel(operation)}-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        await backupService.CreateSafetyBackupAsync(safetyBackupPath, cancellationToken);

        var marker = new PendingDatabaseOperation(
            StagingFileName: Path.GetFileName(stagingPath),
            Operation: operation,
            PreparedAtUtc: DateTime.UtcNow);
        var temporaryMarkerPath = $"{markerPath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(
            temporaryMarkerPath,
            JsonSerializer.Serialize(marker, JsonOptions),
            cancellationToken);
        File.Move(temporaryMarkerPath, markerPath, true);

        logger.LogInformation(
            "Prepared database operation {Operation} for the next application start. Safety backup: {SafetyBackupPath}.",
            operation,
            safetyBackupPath);
        return new DatabaseMaintenanceResult(
            Cancelled: false,
            Operation: operation,
            SourceFileName: sourceFileName,
            TargetFileName: Path.GetFileName(activePath),
            TargetPath: activePath,
            SafetyBackupPath: safetyBackupPath,
            RequiresRestart: true);
    }

    private async Task ValidateSupportedDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(databasePath, SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken);
        await ValidateIntegrityAndCoreSchemaAsync(connection, cancellationToken);

        await using var historyExistsCommand = connection.CreateCommand();
        historyExistsCommand.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
        if (Convert.ToInt64(await historyExistsCommand.ExecuteScalarAsync(cancellationToken)) != 1)
        {
            throw new ValidationException(
                "The selected database predates the supported migration baseline.",
                "sourcePath");
        }

        var knownMigrations = dbContext.Database.GetMigrations()
            .ToHashSet(StringComparer.Ordinal);
        await using var migrationsCommand = connection.CreateCommand();
        migrationsCommand.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";
        await using var reader = await migrationsCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var migrationId = reader.GetString(0);
            if (!knownMigrations.Contains(migrationId))
            {
                throw new ValidationException(
                    "The selected database was created by a newer or incompatible version of OKF-Todo.",
                    "sourcePath");
            }
        }
    }

    private static async Task ValidateCurrentDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(databasePath, SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken);
        await ValidateIntegrityAndCoreSchemaAsync(connection, cancellationToken);

        await using var sampleColumnCommand = connection.CreateCommand();
        sampleColumnCommand.CommandText =
            "SELECT COUNT(*) FROM pragma_table_info('TaskItems') WHERE name = 'IsSampleData';";
        if (Convert.ToInt64(await sampleColumnCommand.ExecuteScalarAsync(cancellationToken)) != 1)
        {
            throw new InvalidDataException(
                "The prepared database does not contain the current sample-data marker.");
        }
    }

    private static async Task ValidateIntegrityAndCoreSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var integrityCommand = connection.CreateCommand();
        integrityCommand.CommandText = "PRAGMA quick_check;";
        var integrityResult = Convert.ToString(
            await integrityCommand.ExecuteScalarAsync(cancellationToken));
        if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"SQLite integrity check failed: {integrityResult}",
                "sourcePath");
        }

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'TaskItems';";
        if (Convert.ToInt64(await schemaCommand.ExecuteScalarAsync(cancellationToken)) != 1)
        {
            throw new ValidationException(
                "The selected file is not an OKF-Todo database.",
                "sourcePath");
        }
    }

    private static async Task CopyDatabaseAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var sourceConnection = CreateConnection(sourcePath, SqliteOpenMode.ReadOnly);
        await using var destinationConnection = CreateConnection(
            destinationPath,
            SqliteOpenMode.ReadWriteCreate);
        await sourceConnection.OpenAsync(cancellationToken);
        await destinationConnection.OpenAsync(cancellationToken);
        sourceConnection.BackupDatabase(destinationConnection);
    }

    private static async Task MigrateDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        await using var context = CreateContext(databasePath);
        await context.Database.MigrateAsync(cancellationToken);
    }

    private async Task CreateFreshDatabaseAsync(
        string databasePath,
        bool includeSampleData,
        CancellationToken cancellationToken)
    {
        await using var context = CreateContext(databasePath);
        await context.Database.MigrateAsync(cancellationToken);

        var lookupSeedService = new LookupSeedService(
            context,
            loggerFactory.CreateLogger<LookupSeedService>());
        await lookupSeedService.SeedAsync(cancellationToken);

        var taskListService = new TaskListService(context);
        await taskListService.EnsureDefaultListAsync(cancellationToken);
        if (!includeSampleData)
        {
            return;
        }

        var lifecycleService = new TaskLifecycleService(
            context,
            loggerFactory.CreateLogger<TaskLifecycleService>());
        var taskService = new TaskService(context, lifecycleService, taskListService);
        var sampleDataSeeder = new SampleDataSeeder(
            context,
            taskService,
            new TaskChecklistService(context),
            new TaskAttachmentService(context),
            new TaskRelationService(context),
            new ImageService(context, loggerFactory.CreateLogger<ImageService>()),
            loggerFactory.CreateLogger<SampleDataSeeder>());
        await sampleDataSeeder.SeedAsync(cancellationToken);
    }

    private string GetActiveDatabasePath()
    {
        var connection = dbContext.Database.GetDbConnection() as SqliteConnection
            ?? throw new InvalidOperationException("The application database is not a SQLite database.");
        if (string.IsNullOrWhiteSpace(connection.DataSource)
            || string.Equals(connection.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Database restore and reset require a file-based application database.",
                "database");
        }

        return Path.GetFullPath(connection.DataSource);
    }

    private static AppDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false))
            .Options;
        return new AppDbContext(options);
    }

    private static SqliteConnection CreateConnection(string databasePath, SqliteOpenMode mode)
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = mode,
            ForeignKeys = true,
            Pooling = false
        }.ToString());
    }

    private static string CreateStagingPath(string activePath)
    {
        var databaseDirectory = Path.GetDirectoryName(activePath)
            ?? throw new InvalidOperationException("The active database path has no parent directory.");
        var markerPath = Path.Combine(databaseDirectory, PendingMarkerFileName);
        if (File.Exists(markerPath))
        {
            throw new ValidationException(
                "A database restore or reset is already prepared. Close and reopen OKF-Todo first.",
                "database");
        }

        return Path.Combine(
            databaseDirectory,
            $"{PendingDatabasePrefix}{Guid.NewGuid():N}.db");
    }

    private static string GetSafetyBackupLabel(string operation)
    {
        return operation == DatabaseOperationTypes.Restore ? "restore" : "reset";
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

public static class PendingDatabaseOperationApplier
{
    private const string PendingMarkerFileName = ".okf-todo.pending-operation.json";
    private const string PendingDatabasePrefix = ".okf-todo.pending-";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Apply(string databasePath, ILogger logger)
    {
        var activePath = Path.GetFullPath(databasePath);
        var databaseDirectory = Path.GetDirectoryName(activePath)
            ?? throw new InvalidOperationException("The active database path has no parent directory.");
        var markerPath = Path.Combine(databaseDirectory, PendingMarkerFileName);
        if (!File.Exists(markerPath))
        {
            return;
        }

        var marker = JsonSerializer.Deserialize<PendingDatabaseOperation>(
            File.ReadAllText(markerPath),
            JsonOptions)
            ?? throw new InvalidDataException("The pending database operation is invalid.");
        if (!string.Equals(
                marker.StagingFileName,
                Path.GetFileName(marker.StagingFileName),
                StringComparison.Ordinal)
            || !marker.StagingFileName.StartsWith(PendingDatabasePrefix, StringComparison.Ordinal)
            || !marker.StagingFileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The pending database staging filename is invalid.");
        }

        var stagingPath = Path.Combine(databaseDirectory, marker.StagingFileName);
        if (!File.Exists(stagingPath))
        {
            throw new FileNotFoundException(
                "The prepared database file is missing.",
                stagingPath);
        }

        EnsureExclusiveAccess(activePath);
        SqliteConnection.ClearAllPools();
        DeleteSidecar(activePath, "-wal");
        DeleteSidecar(activePath, "-shm");

        var rollbackPath = Path.Combine(
            databaseDirectory,
            $"okf-todo-before-apply-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        if (File.Exists(activePath))
        {
            File.Replace(stagingPath, activePath, rollbackPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(stagingPath, activePath);
        }

        File.Delete(markerPath);
        logger.LogInformation(
            "Applied prepared database operation {Operation} from {PreparedAtUtc}.",
            marker.Operation,
            marker.PreparedAtUtc);
    }

    private static void EnsureExclusiveAccess(string activePath)
    {
        if (!File.Exists(activePath))
        {
            return;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = activePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "BEGIN EXCLUSIVE; COMMIT;";
        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException exception)
        {
            throw new InvalidOperationException(
                "The database is in use by another OKF-Todo process. Close the MCP server and try again.",
                exception);
        }
    }

    private static void DeleteSidecar(string activePath, string suffix)
    {
        var sidecarPath = $"{activePath}{suffix}";
        if (File.Exists(sidecarPath))
        {
            File.Delete(sidecarPath);
        }
    }
}

public static class DatabaseResetModes
{
    public const string Empty = "EMPTY";
    public const string Sample = "SAMPLE";
}

public static class DatabaseOperationTypes
{
    public const string Restore = "RESTORE";
    public const string ResetEmpty = "RESET_EMPTY";
    public const string ResetSample = "RESET_SAMPLE";
}

public sealed record DatabaseResetRequest(string? Mode, string? Confirmation);

public sealed record DatabaseMaintenanceResult(
    bool Cancelled,
    string? Operation,
    string? SourceFileName,
    string? TargetFileName,
    string? TargetPath,
    string? SafetyBackupPath,
    bool RequiresRestart)
{
    public static DatabaseMaintenanceResult CancelledResult { get; } =
        new(true, null, null, null, null, null, false);
}

public sealed record PendingDatabaseOperation(
    string StagingFileName,
    string Operation,
    DateTime PreparedAtUtc);
