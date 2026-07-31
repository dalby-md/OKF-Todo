namespace OkfTodo.InstalledContractTests;

internal sealed class InstalledProduct
{
    private const string InstallDirectoryVariable = "OKF_TODO_INSTALL_DIR";

    private InstalledProduct(string rootPath)
    {
        RootPath = rootPath;
        ApplicationPath = Path.Combine(rootPath, "Okf-Todo.exe");
        OkfRootPath = Path.Combine(rootPath, "okf", "todo-database");
        OkfEntryPath = Path.Combine(OkfRootPath, "index.md");
    }

    public string RootPath { get; }
    public string ApplicationPath { get; }
    public string OkfRootPath { get; }
    public string OkfEntryPath { get; }

    public static InstalledProduct Load()
    {
        var configuredPath = Environment.GetEnvironmentVariable(InstallDirectoryVariable);
        var rootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Okf-Todo")
            : configuredPath;

        var product = new InstalledProduct(Path.GetFullPath(rootPath));
        product.RequireFile(product.ApplicationPath, "installed desktop, OKF command, and MCP executable");
        product.RequireFile(product.OkfEntryPath, "installed OKF entry point");
        return product;
    }

    public OkfKnowledge LoadOkfKnowledge() => new(OkfRootPath);

    private void RequireFile(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The {description} was not found at '{path}'. Install OKF-Todo, " +
                $"or set {InstallDirectoryVariable} to the installation directory.",
                path);
        }
    }
}
