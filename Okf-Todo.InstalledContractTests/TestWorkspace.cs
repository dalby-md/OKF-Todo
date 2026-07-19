using Microsoft.Data.Sqlite;

namespace OkfTodo.InstalledContractTests;

internal sealed class TestWorkspace : IAsyncDisposable
{
    public TestWorkspace(string scenario)
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "Okf-Todo.InstalledContractTests",
            $"{scenario}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
        DatabasePath = Path.Combine(DirectoryPath, "okf-todo.db");
    }

    public string DirectoryPath { get; }
    public string DatabasePath { get; }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
