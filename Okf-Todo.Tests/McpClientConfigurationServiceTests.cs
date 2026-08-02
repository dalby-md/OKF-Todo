using System.Text.Json;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class McpClientConfigurationServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(
        Path.GetTempPath(),
        $"okf-todo-mcp-config-{Guid.NewGuid():N}");

    [Fact]
    public void EnsureConfiguration_CreatesSourceCheckoutCommandForCurrentBuildConfiguration()
    {
        var applicationDirectory = Path.Combine(_testRoot, "Okf-Todo", "bin", "Release", "net8.0");
        var projectPath = Path.Combine(_testRoot, "Okf-Todo", "Okf-Todo.csproj");
        Directory.CreateDirectory(applicationDirectory);
        Directory.CreateDirectory(Path.Combine(_testRoot, "docs"));
        File.WriteAllText(projectPath, "<Project />");
        File.WriteAllText(Path.Combine(_testRoot, "docs", "PRD.md"), "# Product");

        var result = new McpClientConfigurationService(applicationDirectory).EnsureConfiguration();

        Assert.Equal(Path.Combine(applicationDirectory, "integration", "mcp-config.json"), result.Path);
        Assert.True(File.Exists(result.Path));
        Assert.Contains("source checkout through dotnet run --no-build (Release)", result.LaunchDescription);

        using var document = JsonDocument.Parse(result.Json);
        var server = document.RootElement.GetProperty("mcpServers").GetProperty("okf-todo");
        Assert.Equal("dotnet", server.GetProperty("command").GetString());
        Assert.Equal(
            [
                "run",
                "--no-build",
                "--configuration",
                "Release",
                "--project",
                Path.GetFullPath(projectPath),
                "--",
                "--mcp"
            ],
            server.GetProperty("args").EnumerateArray().Select(value => value.GetString()).ToArray());
    }

    [Fact]
    public void EnsureConfiguration_PreservesInstallerGeneratedConfiguration()
    {
        var applicationDirectory = Path.Combine(_testRoot, "installed");
        var configPath = Path.Combine(applicationDirectory, "integration", "mcp-config.json");
        var installerJson = "{\"installer\":true}";
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, installerJson);

        var result = new McpClientConfigurationService(applicationDirectory).EnsureConfiguration();

        Assert.Equal(configPath, result.Path);
        Assert.Equal(installerJson, result.Json);
        Assert.Equal("installed executable", result.LaunchDescription);
        Assert.Equal(installerJson, File.ReadAllText(configPath));
    }

    [Fact]
    public void EnsureConfiguration_CreatesExecutableCommandWhenInstalledConfigIsMissing()
    {
        var applicationDirectory = Path.Combine(_testRoot, "installed-missing-config");
        Directory.CreateDirectory(applicationDirectory);
        var executablePath = Path.Combine(
            applicationDirectory,
            OperatingSystem.IsWindows() ? "Okf-Todo.exe" : "Okf-Todo");
        File.WriteAllText(executablePath, string.Empty);

        var result = new McpClientConfigurationService(applicationDirectory).EnsureConfiguration();

        using var document = JsonDocument.Parse(result.Json);
        var server = document.RootElement.GetProperty("mcpServers").GetProperty("okf-todo");
        Assert.Equal(executablePath, server.GetProperty("command").GetString());
        Assert.Equal(
            ["--mcp"],
            server.GetProperty("args").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal("installed executable", result.LaunchDescription);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
