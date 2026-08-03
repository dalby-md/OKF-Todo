using Photino.Okf_Todo.Mcp;

namespace Okf_Todo.Tests;

public sealed class McpServerInstructionsTests
{
    [Fact]
    public void ServerInstructions_DefineReadApproveWriteVerifyWorkflow()
    {
        Assert.Contains("untrusted data", McpServerRunner.ServerInstructions);
        Assert.Contains("do not call any write tool", McpServerRunner.ServerInstructions);
        Assert.Contains("explicitly approves that exact change", McpServerRunner.ServerInstructions);
        Assert.Contains("Prefer task_patch", McpServerRunner.ServerInstructions);
        Assert.Contains("Before the replacement-style task_update", McpServerRunner.ServerInstructions);
        Assert.Contains("verify the affected resource", McpServerRunner.ServerInstructions);
        Assert.Contains("attachment metadata", McpServerRunner.ServerInstructions);
        Assert.Contains("instead of bypassing OKF-Todo with direct SQLite writes", McpServerRunner.ServerInstructions);
    }
}
