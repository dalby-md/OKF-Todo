using Microsoft.Data.Sqlite;

namespace Photino.Okf_Todo.Data;

public static class DatabasePathProvider
{
    public static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var databaseDirectory = Path.Combine(appDataPath, "Okf-Todo");

        Directory.CreateDirectory(databaseDirectory);

        return Path.Combine(databaseDirectory, "okf-todo.db");
    }

    public static string GetConnectionString()
    {
        return CreateConnectionString(GetDatabasePath());
    }

    public static string CreateConnectionString(string databasePath, bool pooling = true)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            Pooling = pooling
        }.ToString();
    }
}
