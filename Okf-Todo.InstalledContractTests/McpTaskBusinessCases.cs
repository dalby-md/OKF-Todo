namespace OkfTodo.InstalledContractTests;

public sealed class McpTaskBusinessCases
{
    [Fact]
    public async Task InsertTask_ThroughInstalledMcp_PersistsTaskAndCreationHistory()
    {
        var product = InstalledProduct.Load();
        await using var workspace = new TestWorkspace("mcp-insert");
        await using var mcp = await McpClientHarness.StartAsync(product, workspace.DatabasePath);

        var toolNames = await mcp.ListToolNamesAsync();
        Assert.Contains("task_create", toolNames);
        Assert.Contains("task_get", toolNames);

        var created = await mcp.CallAsync("task_create", new Dictionary<string, object?>
        {
            ["title"] = "Investigate failed deployment",
            ["taskTypeCode"] = "INVESTIGATION",
            ["body"] = "Evidence collected through the installed MCP contract.",
            ["bodyFormatCode"] = "MARKDOWN",
            ["taskPriorityCode"] = "NORMAL",
            ["tags"] = new[] { "installed-contract", "deployment" }
        });

        var taskId = created.GetProperty("id").GetInt32();
        Assert.Equal("Investigate failed deployment", created.GetProperty("title").GetString());
        Assert.Equal("ACTIVE", created.GetProperty("taskStatusCode").GetString());

        var readBack = await mcp.CallAsync("task_get", new Dictionary<string, object?> { ["id"] = taskId });
        Assert.Equal("Investigate failed deployment", readBack.GetProperty("title").GetString());

        var stored = await SqliteEvidence.ReadTaskAsync(workspace.DatabasePath, taskId);
        Assert.Equal("Investigate failed deployment", stored.Title);
        Assert.Equal(1, await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_CREATED"));
    }

    [Fact]
    public async Task UpdateTask_ThroughInstalledMcp_PreservesReadFieldsAndWritesHistory()
    {
        var product = InstalledProduct.Load();
        await using var workspace = new TestWorkspace("mcp-update");
        await using var mcp = await McpClientHarness.StartAsync(product, workspace.DatabasePath);

        var created = await mcp.CallAsync("task_create", new Dictionary<string, object?>
        {
            ["title"] = "Review deployment log",
            ["taskTypeCode"] = "INVESTIGATION",
            ["body"] = "Original body",
            ["bodyFormatCode"] = "MARKDOWN",
            ["taskPriorityCode"] = "NORMAL",
            ["tags"] = new[] { "preserve-me" }
        });
        var taskId = created.GetProperty("id").GetInt32();
        var before = await mcp.CallAsync("task_get", new Dictionary<string, object?> { ["id"] = taskId });

        var updated = await mcp.CallAsync("task_update", BuildReplacementArguments(
            before,
            "Review deployment log and variables"));

        Assert.Equal("Review deployment log and variables", updated.GetProperty("title").GetString());
        Assert.Equal("Original body", updated.GetProperty("body").GetString());
        Assert.Contains(
            updated.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()),
            tag => tag == "preserve-me");

        var stored = await SqliteEvidence.ReadTaskAsync(workspace.DatabasePath, taskId);
        Assert.Equal("Review deployment log and variables", stored.Title);
        Assert.True(await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_UPDATED") >= 1);
    }

    private static IReadOnlyDictionary<string, object?> BuildReplacementArguments(
        System.Text.Json.JsonElement task,
        string replacementTitle)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = task.GetProperty("id").GetInt32(),
            ["title"] = replacementTitle,
            ["taskTypeCode"] = task.GetProperty("taskTypeCode").GetString(),
            ["body"] = task.GetProperty("body").GetString(),
            ["bodyFormatCode"] = task.GetProperty("bodyFormatCode").GetString(),
            ["taskPriorityCode"] = task.GetProperty("taskPriorityCode").GetString(),
            ["taskSourceCode"] = null,
            ["sourceReference"] = null,
            ["sourceUrl"] = null,
            ["deadline"] = null,
            ["activeWaitingForLabel"] = null,
            ["tags"] = task.GetProperty("tags").EnumerateArray()
                .Select(tag => tag.GetString()!)
                .ToArray()
        };
    }
}
