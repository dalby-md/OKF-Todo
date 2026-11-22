namespace Okf_Todo.Tests;

/// <summary>
/// .agents/skills is the source of truth for repository skills. Claude Code only reads
/// .claude/skills, so that folder holds a committed copy made by tools/sync-claude-skills.ps1.
/// </summary>
public sealed class ClaudeSkillsCopyTests
{
    private const string FixHint = "Run .\\tools\\sync-claude-skills.ps1 to update the copy.";

    [Fact]
    public void ClaudeSkills_MatchAgentSkills()
    {
        var repositoryRoot = FindRepositoryRoot();
        var source = Path.Combine(repositoryRoot, ".agents", "skills");
        var target = Path.Combine(repositoryRoot, ".claude", "skills");

        Assert.True(Directory.Exists(source), $"Skill source folder not found: {source}");
        Assert.True(Directory.Exists(target), $".claude/skills is missing. {FixHint}");

        var sourceFiles = ListRelativeFiles(source);
        var targetFiles = ListRelativeFiles(target);

        var missing = sourceFiles.Except(targetFiles, StringComparer.OrdinalIgnoreCase).ToList();
        var extra = targetFiles.Except(sourceFiles, StringComparer.OrdinalIgnoreCase).ToList();
        var changed = sourceFiles
            .Intersect(targetFiles, StringComparer.OrdinalIgnoreCase)
            .Where(file => !File.ReadAllBytes(Path.Combine(source, file))
                .AsSpan()
                .SequenceEqual(File.ReadAllBytes(Path.Combine(target, file))))
            .ToList();

        Assert.True(missing.Count == 0, $"Missing from .claude/skills: {string.Join(", ", missing)}. {FixHint}");
        Assert.True(extra.Count == 0, $"Not in .agents/skills: {string.Join(", ", extra)}. {FixHint}");
        Assert.True(changed.Count == 0, $"Out of date in .claude/skills: {string.Join(", ", changed)}. {FixHint}");
    }

    private static List<string> ListRelativeFiles(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Okf-Todo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find the repository root (Okf-Todo.slnx).");
    }
}
