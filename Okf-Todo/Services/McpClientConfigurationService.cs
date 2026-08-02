using System.Text;
using System.Text.Json;

namespace Photino.Okf_Todo.Services;

public sealed class McpClientConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _applicationBaseDirectory;

    public McpClientConfigurationService(string applicationBaseDirectory)
    {
        _applicationBaseDirectory = Path.GetFullPath(applicationBaseDirectory);
    }

    public McpClientConfiguration EnsureConfiguration()
    {
        var configPath = Path.Combine(_applicationBaseDirectory, "integration", "mcp-config.json");
        var sourceProjectPath = ResolveSourceProjectPath(_applicationBaseDirectory);

        if (sourceProjectPath is null && File.Exists(configPath))
        {
            return new McpClientConfiguration(
                configPath,
                File.ReadAllText(configPath),
                "installed executable");
        }

        var configuration = sourceProjectPath is not null
            ? CreateSourceConfiguration(
                sourceProjectPath,
                ResolveBuildConfiguration(_applicationBaseDirectory))
            : CreateExecutableConfiguration(_applicationBaseDirectory);
        var json = JsonSerializer.Serialize(configuration.Value, JsonOptions) + Environment.NewLine;

        try
        {
            WriteConfiguration(configPath, json);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Okf-Todo",
                "integration",
                "mcp-config.json");
            WriteConfiguration(configPath, json);
        }

        return new McpClientConfiguration(configPath, json, configuration.Description);
    }

    internal static string? ResolveSourceProjectPath(string applicationBaseDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(applicationBaseDirectory));
             directory is not null;
             directory = directory.Parent)
        {
            var projectPath = Path.Combine(directory.FullName, "Okf-Todo", "Okf-Todo.csproj");
            var productDocsPath = Path.Combine(directory.FullName, "docs", "PRD.md");
            if (File.Exists(projectPath) && File.Exists(productDocsPath))
            {
                return projectPath;
            }
        }

        return null;
    }

    internal static string ResolveBuildConfiguration(string applicationBaseDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(applicationBaseDirectory));
             directory.Parent is not null;
             directory = directory.Parent)
        {
            if (string.Equals(directory.Parent.Name, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return directory.Name;
            }
        }

        return "Debug";
    }

    private static (object Value, string Description) CreateSourceConfiguration(
        string projectPath,
        string buildConfiguration)
    {
        return (CreateConfiguration(
            "dotnet",
            [
                "run",
                "--no-build",
                "--configuration",
                buildConfiguration,
                "--project",
                Path.GetFullPath(projectPath),
                "--",
                "--mcp"
            ]),
            $"source checkout through dotnet run --no-build ({buildConfiguration})");
    }

    private static (object Value, string Description) CreateExecutableConfiguration(string applicationBaseDirectory)
    {
        var executableName = OperatingSystem.IsWindows() ? "Okf-Todo.exe" : "Okf-Todo";
        var executablePath = Path.Combine(applicationBaseDirectory, executableName);
        if (File.Exists(executablePath))
        {
            return (CreateConfiguration(executablePath, ["--mcp"]), "installed executable");
        }

        return (CreateConfiguration(
            "dotnet",
            [Path.Combine(applicationBaseDirectory, "Okf-Todo.dll"), "--mcp"]),
            "framework-dependent application");
    }

    private static object CreateConfiguration(string command, IReadOnlyCollection<string> arguments)
    {
        return new
        {
            mcpServers = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["okf-todo"] = new
                {
                    command,
                    args = arguments
                }
            }
        };
    }

    private static void WriteConfiguration(string path, string json)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("The MCP configuration path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".mcp-config.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}

public sealed record McpClientConfiguration(
    string Path,
    string Json,
    string LaunchDescription);
