using System.Text;

namespace OkfTodo.InstalledContractTests;

public sealed class OkfSqliteKnowledgeCases
{
    [Fact]
    public async Task InsertTaskAndAttachment_UsingInstalledOkfKnowledge_IsVisibleInSqliteAndApplication()
    {
        var product = InstalledProduct.Load();
        var knowledge = product.LoadOkfKnowledge();
        await using var workspace = new TestWorkspace("okf-sqlite-insert");
        var okf = new OkfCommandClient(product, workspace.DatabasePath);

        await BootstrapDatabaseAsync(okf);
        var mutator = new OkfSqliteMutator(knowledge, workspace.DatabasePath);
        var content = Encoding.UTF8.GetBytes("Attachment written from installed OKF table knowledge.");
        var taskId = await mutator.InsertTaskWithAttachmentAsync(
            "Task inserted from OKF schema knowledge",
            "okf-evidence.txt",
            "text/plain",
            content,
            "Direct SQLite capability test");

        var stored = await SqliteEvidence.ReadTaskAsync(workspace.DatabasePath, taskId);
        Assert.Equal("Task inserted from OKF schema knowledge", stored.Title);
        var attachment = await SqliteEvidence.ReadAttachmentAsync(
            workspace.DatabasePath,
            taskId,
            "okf-evidence.txt");
        Assert.Equal(content, attachment.Content);

        var applicationRead = await okf.ExecuteAsync("task.get", new { id = taskId });
        Assert.Equal("Task inserted from OKF schema knowledge", applicationRead.GetProperty("title").GetString());

        Assert.Equal(0, await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_CREATED"));
        Assert.Equal(0, await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "ATTACHMENT_ADDED"));
    }

    [Fact]
    public async Task UpdateTask_UsingInstalledOkfKnowledge_ChangesSqliteButBypassesApplicationHistory()
    {
        var product = InstalledProduct.Load();
        var knowledge = product.LoadOkfKnowledge();
        await using var workspace = new TestWorkspace("okf-sqlite-update");
        var okf = new OkfCommandClient(product, workspace.DatabasePath);
        var created = await BootstrapDatabaseAsync(okf);
        var taskId = created.GetProperty("id").GetInt32();
        var historyBefore = await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_UPDATED");

        var mutator = new OkfSqliteMutator(knowledge, workspace.DatabasePath);
        await mutator.UpdateTaskTitleAsync(taskId, "Title updated directly from OKF schema knowledge");

        var stored = await SqliteEvidence.ReadTaskAsync(workspace.DatabasePath, taskId);
        Assert.Equal("Title updated directly from OKF schema knowledge", stored.Title);
        var applicationRead = await okf.ExecuteAsync("task.get", new { id = taskId });
        Assert.Equal(
            "Title updated directly from OKF schema knowledge",
            applicationRead.GetProperty("title").GetString());
        Assert.Equal(historyBefore, await SqliteEvidence.CountLogEntriesAsync(
            workspace.DatabasePath,
            taskId,
            "TASK_UPDATED"));
    }

    private static Task<System.Text.Json.JsonElement> BootstrapDatabaseAsync(OkfCommandClient okf) =>
        okf.ExecuteAsync("task.create", new
        {
            title = "Database bootstrap task",
            taskTypeCode = "REQUEST",
            body = (string?)null,
            bodyFormatCode = (string?)null,
            taskPriorityCode = (string?)null,
            taskSourceCode = (string?)null,
            sourceReference = (string?)null,
            sourceUrl = (string?)null,
            deadline = (DateTime?)null,
            activeWaitingForLabel = (string?)null,
            tags = Array.Empty<string>()
        });
}
