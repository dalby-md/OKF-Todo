using System.Text;
using System.Text.Json;

namespace OkfTodo.InstalledContractTests;

public sealed class OkfCommandBusinessCases
{
    [Fact]
    public async Task InsertTaskAndAttachment_ThroughInstalledOkfCommand_PersistsContentAndHistory()
    {
        var product = InstalledProduct.Load();
        var knowledge = product.LoadOkfKnowledge();
        knowledge.RequireApplicationCommandContract(
            "task.create",
            "task.attachment.create",
            "taskTypeCode",
            "base64Data");
        await using var workspace = new TestWorkspace("okf-command-insert");
        var okf = new OkfCommandClient(product, workspace.DatabasePath);

        var created = await okf.ExecuteAsync("task.create", new
        {
            title = "Investigate customer export timeout",
            taskTypeCode = "INVESTIGATION",
            body = "Customer evidence supplied to the AI harness.",
            bodyFormatCode = "MARKDOWN",
            taskPriorityCode = "NORMAL",
            taskSourceCode = "EMAIL",
            sourceReference = "MAIL-2026-0719",
            sourceUrl = (string?)null,
            deadline = (DateTime?)null,
            activeWaitingForLabel = (string?)null,
            tags = new[] { "installed-contract", "customer" }
        });
        var taskId = created.GetProperty("id").GetInt32();
        var attachmentContent = Encoding.UTF8.GetBytes("Timeout after 30 seconds\r\nCorrelation: abc-123");

        await okf.ExecuteAsync("task.attachment.create", new
        {
            taskId,
            fileName = "export-timeout.log",
            contentType = "text/plain",
            base64Data = Convert.ToBase64String(attachmentContent),
            description = "Customer diagnostic output"
        });

        var readBack = await okf.ExecuteAsync("task.get", new { id = taskId });
        Assert.Equal("Investigate customer export timeout", readBack.GetProperty("title").GetString());

        var attachment = await SqliteEvidence.ReadAttachmentAsync(
            workspace.DatabasePath,
            taskId,
            "export-timeout.log");
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal(attachmentContent, attachment.Content);
        Assert.False(string.IsNullOrWhiteSpace(attachment.Sha256Hash));
        Assert.Equal(1, await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "ATTACHMENT_ADDED"));
    }

    [Fact]
    public async Task UpdateTask_ThroughInstalledOkfCommand_PreservesReadFieldsAndWritesHistory()
    {
        var product = InstalledProduct.Load();
        var knowledge = product.LoadOkfKnowledge();
        knowledge.RequireApplicationCommandContract("task.get", "task.update", "preserve");
        await using var workspace = new TestWorkspace("okf-command-update");
        var okf = new OkfCommandClient(product, workspace.DatabasePath);

        var created = await okf.ExecuteAsync("task.create", new
        {
            title = "Prepare customer handover",
            taskTypeCode = "REQUEST",
            body = "Keep this body",
            bodyFormatCode = "MARKDOWN",
            taskPriorityCode = "NORMAL",
            taskSourceCode = (string?)null,
            sourceReference = (string?)null,
            sourceUrl = (string?)null,
            deadline = (DateTime?)null,
            activeWaitingForLabel = (string?)null,
            tags = new[] { "handover" }
        });
        var taskId = created.GetProperty("id").GetInt32();
        var before = await okf.ExecuteAsync("task.get", new { id = taskId });

        var updated = await okf.ExecuteAsync("task.update", CreateReplacementPayload(
            before,
            "Prepare reviewed customer handover"));

        Assert.Equal("Prepare reviewed customer handover", updated.GetProperty("title").GetString());
        Assert.Equal("Keep this body", updated.GetProperty("body").GetString());
        Assert.Contains(
            updated.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()),
            tag => tag == "handover");

        var stored = await SqliteEvidence.ReadTaskAsync(workspace.DatabasePath, taskId);
        Assert.Equal("Prepare reviewed customer handover", stored.Title);
        Assert.True(await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_UPDATED") >= 1);
    }

    private static object CreateReplacementPayload(JsonElement task, string replacementTitle) => new
    {
        id = task.GetProperty("id").GetInt32(),
        title = replacementTitle,
        taskTypeCode = ReadNullableString(task, "taskTypeCode"),
        body = ReadNullableString(task, "body"),
        bodyFormatCode = ReadNullableString(task, "bodyFormatCode"),
        taskPriorityCode = ReadNullableString(task, "taskPriorityCode"),
        taskSourceCode = ReadNullableString(task, "taskSourceCode"),
        sourceReference = ReadNullableString(task, "sourceReference"),
        sourceUrl = ReadNullableString(task, "sourceUrl"),
        deadline = ReadNullableString(task, "deadline"),
        activeWaitingForLabel = task.TryGetProperty("activeWaitingFor", out var waitingFor)
            && waitingFor.ValueKind != JsonValueKind.Null
                ? ReadNullableString(waitingFor, "label")
                : null,
        tags = task.GetProperty("tags").EnumerateArray()
            .Select(tag => tag.GetString()!)
            .ToArray()
    };

    private static string? ReadNullableString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind != JsonValueKind.Null
                ? property.GetString()
                : null;
    }
}
