using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class HelpRuntimeContextServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(
        Path.GetTempPath(),
        $"okf-todo-help-context-{Guid.NewGuid():N}");

    [Fact]
    public void GetContext_UsesInstalledOkfEntryAndConfiguredDatabasePaths()
    {
        var applicationDirectory = Path.Combine(_testRoot, "application");
        var okfEntryPath = Path.Combine(applicationDirectory, "okf", "todo-database", "index.md");
        var databasePath = Path.Combine(_testRoot, "data", "custom.db");
        Directory.CreateDirectory(Path.GetDirectoryName(okfEntryPath)!);
        File.WriteAllText(okfEntryPath, "# OKF");

        var mcpConfigurationService = new McpClientConfigurationService(applicationDirectory);
        var result = new HelpRuntimeContextService(
            applicationDirectory,
            databasePath,
            mcpConfigurationService).GetContext();

        Assert.Equal(Path.GetFullPath(okfEntryPath), result.OkfEntryPath);
        Assert.Equal(Path.GetFullPath(databasePath), result.DatabasePath);
        Assert.False(string.IsNullOrWhiteSpace(result.OperatingSystem));
        Assert.Equal(
            Path.Combine(applicationDirectory, "integration", "mcp-config.json"),
            result.McpConfigPath);
        Assert.Contains("mcpServers", result.McpConfigJson);
        Assert.Contains("framework-dependent application", result.McpLaunchDescription);
    }

    [Fact]
    public void ResolveOkfEntryPath_FindsSourceCheckoutFromBuildOutput()
    {
        var applicationDirectory = Path.Combine(_testRoot, "Okf-Todo", "bin", "Debug", "net8.0");
        var okfEntryPath = Path.Combine(_testRoot, "docs", "okf", "todo-database", "index.md");
        Directory.CreateDirectory(applicationDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(okfEntryPath)!);
        File.WriteAllText(okfEntryPath, "# OKF");

        var result = HelpRuntimeContextService.ResolveOkfEntryPath(applicationDirectory);

        Assert.Equal(Path.GetFullPath(okfEntryPath), result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
