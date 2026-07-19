namespace OkfTodo.InstalledContractTests;

internal sealed class OkfKnowledge
{
    private readonly string rootPath;

    public OkfKnowledge(string rootPath)
    {
        this.rootPath = Path.GetFullPath(rootPath);
        RequireFile(Path.Combine(this.rootPath, "index.md"));
    }

    public string RequireApplicationCommandContract(params string[] requiredTerms)
    {
        var path = Path.Combine(rootPath, "references", "application-command-interface.md");
        var content = File.ReadAllText(RequireFile(path));
        RequireTerms(path, content, requiredTerms);
        return content;
    }

    public OkfTable RequireTable(string fileName, string tableName, params string[] requiredColumns)
    {
        var path = Path.Combine(rootPath, "tables", fileName);
        var content = File.ReadAllText(RequireFile(path));
        RequireTerms(path, content, [$"# {tableName}", .. requiredColumns.Select(column => $"`{column}`")]);
        return new OkfTable(tableName, requiredColumns.ToHashSet(StringComparer.Ordinal));
    }

    private static void RequireTerms(string path, string content, IEnumerable<string> requiredTerms)
    {
        foreach (var term in requiredTerms)
        {
            if (!content.Contains(term, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Installed OKF file '{path}' does not describe required contract term '{term}'.");
            }
        }
    }

    private string RequireFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"OKF link escaped the installed OKF root: '{path}'.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Installed OKF file was not found: '{fullPath}'.", fullPath);
        }

        return fullPath;
    }
}

internal sealed record OkfTable(string Name, IReadOnlySet<string> Columns);
