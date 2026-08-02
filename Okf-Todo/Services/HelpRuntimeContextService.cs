namespace Photino.Okf_Todo.Services;

public sealed class HelpRuntimeContextService
{
    private readonly string _applicationBaseDirectory;
    private readonly string _databasePath;
    private readonly McpClientConfigurationService _mcpConfigurationService;

    public HelpRuntimeContextService(
        string applicationBaseDirectory,
        string databasePath,
        McpClientConfigurationService mcpConfigurationService)
    {
        _applicationBaseDirectory = Path.GetFullPath(applicationBaseDirectory);
        _databasePath = Path.GetFullPath(databasePath);
        _mcpConfigurationService = mcpConfigurationService;
    }

    public HelpRuntimeContext GetContext()
    {
        var mcpConfiguration = _mcpConfigurationService.EnsureConfiguration();
        return new HelpRuntimeContext(
            GetOperatingSystemName(),
            ResolveOkfEntryPath(_applicationBaseDirectory),
            _databasePath,
            mcpConfiguration.Path,
            mcpConfiguration.Json.TrimEnd(),
            mcpConfiguration.LaunchDescription);
    }

    internal static string ResolveOkfEntryPath(string applicationBaseDirectory)
    {
        var baseDirectory = Path.GetFullPath(applicationBaseDirectory);
        var installedPath = Path.Combine(baseDirectory, "okf", "todo-database", "index.md");
        if (File.Exists(installedPath))
        {
            return installedPath;
        }

        var macOsBundlePath = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "Resources",
            "okf",
            "todo-database",
            "index.md"));
        if (File.Exists(macOsBundlePath))
        {
            return macOsBundlePath;
        }

        for (var directory = new DirectoryInfo(baseDirectory); directory is not null; directory = directory.Parent)
        {
            var sourcePath = Path.Combine(directory.FullName, "docs", "okf", "todo-database", "index.md");
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return installedPath;
    }

    private static string GetOperatingSystemName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        return "this operating system";
    }
}

public sealed record HelpRuntimeContext(
    string OperatingSystem,
    string OkfEntryPath,
    string DatabasePath,
    string McpConfigPath,
    string McpConfigJson,
    string McpLaunchDescription);
