using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Photino.Okf_Todo.Mcp;

namespace Okf_Todo.Tests;

public sealed class McpServerProcessTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task McpServer_ListsToolsAndCreatesTaskWithTimelineEntry()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "Okf-Todo.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "mcp.db");
        Process? process = null;

        try
        {
            var serverPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Okf-Todo",
                "bin",
#if DEBUG
                "Debug",
#else
                "Release",
#endif
                "net8.0",
                "Okf-Todo.exe"));
            Assert.True(File.Exists(serverPath), $"Unified OKF-Todo executable was not found at {serverPath}.");

            var startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("--mcp");
            startInfo.ArgumentList.Add("--database-path");
            startInfo.ArgumentList.Add(databasePath);

            process = new Process { StartInfo = startInfo };
            Assert.True(process.Start());
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { },
                    clientInfo = new { name = "Okf-Todo.Tests", version = "1.0" }
                }
            });
            using var initialize = await ReadResponseAsync(process, 1);
            Assert.Equal("2.0", initialize.RootElement.GetProperty("jsonrpc").GetString());
            Assert.Equal(
                McpServerRunner.ServerInstructions,
                initialize.RootElement.GetProperty("result").GetProperty("instructions").GetString());

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                method = "notifications/initialized"
            });
            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/list"
            });

            using var toolsResponse = await ReadResponseAsync(process, 2);
            var toolNames = toolsResponse.RootElement
                .GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "task_list",
                    "task_get_lookups",
                    "task_list_lists",
                    "task_get",
                    "task_get_context",
                    "task_create",
                    "task_update",
                    "task_patch",
                    "task_move_to_list",
                    "task_undo_list_move",
                    "task_get_timeline",
                    "task_add_comment",
                    "task_delete_comment",
                    "task_complete",
                    "task_cancel",
                    "task_reopen",
                    "task_set_starred",
                    "task_bulk_set_starred",
                    "task_set_waiting",
                    "task_clear_waiting",
                    "task_move_to_trash",
                    "task_restore_from_trash",
                    "task_checklist_list",
                    "task_checklist_add",
                    "task_checklist_update",
                    "task_checklist_set_completed",
                    "task_checklist_reorder",
                    "task_checklist_delete",
                    "task_relationship_options",
                    "task_relationship_list",
                    "task_relationship_add",
                    "task_relationship_delete",
                    "task_attachment_list",
                    "task_attachment_get",
                    "task_attachment_add",
                    "task_attachment_delete",
                    "task_list_create",
                    "task_list_rename",
                    "task_list_reorder",
                    "task_list_delete"
                },
                toolNames);

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 3,
                method = "tools/call",
                @params = new
                {
                    name = "task_create",
                    arguments = new
                    {
                        title = "Created through MCP",
                        taskTypeCode = "REQUEST",
                        taskPriorityCode = "NORMAL",
                        tags = new[] { "mcp" }
                    }
                }
            });
            using var createResponse = await ReadResponseAsync(process, 3);
            var createResult = createResponse.RootElement.GetProperty("result");
            Assert.False(
                createResult.TryGetProperty("isError", out var isError)
                && isError.GetBoolean());
            var taskId = ReadTextContent(createResponse).GetProperty("id").GetInt32();

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 4,
                method = "tools/call",
                @params = new
                {
                    name = "task_get_timeline",
                    arguments = new { taskId }
                }
            });
            using var timelineResponse = await ReadResponseAsync(process, 4);
            var timeline = ReadTextContent(timelineResponse);
            Assert.Contains(
                timeline.EnumerateArray(),
                entry => entry.GetProperty("logTypeCode").GetString() == "TASK_CREATED");

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 5,
                method = "tools/call",
                @params = new
                {
                    name = "task_patch",
                    arguments = new
                    {
                        id = taskId,
                        changes = new
                        {
                            taskPriorityCode = "URGENT",
                            owner = "MCP owner"
                        }
                    }
                }
            });
            using var patchResponse = await ReadResponseAsync(process, 5);
            var patched = ReadTextContent(patchResponse);
            Assert.Equal("URGENT", patched.GetProperty("taskPriorityCode").GetString());
            Assert.Equal("MCP owner", patched.GetProperty("owner").GetString());
            Assert.Equal("Created through MCP", patched.GetProperty("title").GetString());

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 6,
                method = "tools/call",
                @params = new
                {
                    name = "task_checklist_add",
                    arguments = new { taskId, text = "Verify expanded MCP" }
                }
            });
            using var checklistResponse = await ReadResponseAsync(process, 6);
            Assert.Contains(
                ReadTextContent(checklistResponse).EnumerateArray(),
                item => item.GetProperty("text").GetString() == "Verify expanded MCP");

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 7,
                method = "tools/call",
                @params = new
                {
                    name = "task_add_comment",
                    arguments = new { taskId, commentText = "MCP progress note" }
                }
            });
            using var commentResponse = await ReadResponseAsync(process, 7);
            Assert.Contains(
                ReadTextContent(commentResponse).EnumerateArray(),
                item => item.GetProperty("kind").GetString() == "comment"
                    && item.GetProperty("text").GetString() == "MCP progress note");

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 8,
                method = "tools/call",
                @params = new
                {
                    name = "task_attachment_add",
                    arguments = new
                    {
                        taskId,
                        fileName = "evidence.txt",
                        contentType = "text/plain",
                        base64Data = Convert.ToBase64String("evidence"u8.ToArray())
                    }
                }
            });
            using var attachmentResponse = await ReadResponseAsync(process, 8);
            Assert.Contains(
                ReadTextContent(attachmentResponse).EnumerateArray(),
                item => item.GetProperty("fileName").GetString() == "evidence.txt");

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 9,
                method = "tools/call",
                @params = new
                {
                    name = "task_get_context",
                    arguments = new { id = taskId }
                }
            });
            using var contextResponse = await ReadResponseAsync(process, 9);
            var context = ReadTextContent(contextResponse);
            Assert.Single(context.GetProperty("checklist").EnumerateArray());
            Assert.Single(context.GetProperty("attachments").EnumerateArray());
            Assert.Contains(
                context.GetProperty("timeline").EnumerateArray(),
                item => item.GetProperty("text").GetString() == "MCP progress note");

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 10,
                method = "tools/call",
                @params = new
                {
                    name = "task_complete",
                    arguments = new { id = taskId }
                }
            });
            using var completeResponse = await ReadResponseAsync(process, 10);
            Assert.Equal("COMPLETED", ReadTextContent(completeResponse).GetProperty("taskStatusCode").GetString());

            await SendAsync(process, new
            {
                jsonrpc = "2.0",
                id = 11,
                method = "tools/call",
                @params = new
                {
                    name = "task_reopen",
                    arguments = new { id = taskId }
                }
            });
            using var reopenResponse = await ReadResponseAsync(process, 11);
            Assert.Equal("ACTIVE", ReadTextContent(reopenResponse).GetProperty("taskStatusCode").GetString());

            process.StandardInput.Close();
            using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(exitTimeout.Token);
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("OKF-Todo MCP server is using database", await standardErrorTask);
        }
        finally
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            process?.Dispose();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static async Task SendAsync(Process process, object message)
    {
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message, JsonOptions));
        await process.StandardInput.FlushAsync();
    }

    private static async Task<JsonDocument> ReadResponseAsync(Process process, int id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (line is null)
            {
                throw new InvalidOperationException($"MCP server stdout closed before response {id}.");
            }

            var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("id", out var responseId)
                && responseId.ValueKind == JsonValueKind.Number
                && responseId.GetInt32() == id)
            {
                return document;
            }

            document.Dispose();
        }
    }

    private static JsonElement ReadTextContent(JsonDocument response)
    {
        var text = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.False(string.IsNullOrWhiteSpace(text));
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }
}
