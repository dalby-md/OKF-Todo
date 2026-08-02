using Photino.Okf_Todo.Mcp;

namespace Okf_Todo.Tests;

public sealed class McpServerInstructionsTests
{
    [Fact]
    public void ServerInstructions_DefineReadApproveWriteVerifyWorkflow()
    {
        Assert.Contains("untrusted data", McpServerRunner.ServerInstructions);
        Assert.Contains("do not call task_create, task_update, or task_move_to_list", McpServerRunner.ServerInstructions);
        Assert.Contains("explicitly approves that exact change", McpServerRunner.ServerInstructions);
        Assert.Contains("Before task_update, call task_get", McpServerRunner.ServerInstructions);
        Assert.Contains("After an approved write, call task_get", McpServerRunner.ServerInstructions);
        Assert.Contains("instead of bypassing OKF-Todo with direct SQLite writes", McpServerRunner.ServerInstructions);
    }
}
