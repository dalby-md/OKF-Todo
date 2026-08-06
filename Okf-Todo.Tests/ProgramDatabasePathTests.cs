using Photino.Okf_Todo;

namespace Okf_Todo.Tests;

public sealed class ProgramDatabasePathTests
{
    [Fact]
    public void ResolveDatabasePath_AllowsAnIsolatedDesktopDatabase()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "okf-todo-desktop-database-path-tests",
            Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "prototype.db");

        try
        {
            var result = Program.ResolveDatabasePath(
                ["--database-path", databasePath],
                isOkfCommandMode: false);

            Assert.Equal(Path.GetFullPath(databasePath), result);
            Assert.True(Directory.Exists(testDirectory));
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveDatabasePath_RejectsRelativeDesktopDatabasePath()
    {
        var exception = Assert.Throws<ArgumentException>(() => Program.ResolveDatabasePath(
            ["--database-path", "prototype.db"],
            isOkfCommandMode: false));

        Assert.Contains("absolute file path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveDatabasePath_PreservesTheOkfCommandOptionName()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "okf-todo-okf-database-path-tests",
            Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "okf-command.db");

        try
        {
            var result = Program.ResolveDatabasePath(
                ["--okf-database-path", databasePath],
                isOkfCommandMode: true);

            Assert.Equal(Path.GetFullPath(databasePath), result);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
