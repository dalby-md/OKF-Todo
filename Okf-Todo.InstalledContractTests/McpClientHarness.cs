using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace OkfTodo.InstalledContractTests;

internal sealed class McpClientHarness : IAsyncDisposable
{
    private readonly McpClient client;

    private McpClientHarness(McpClient client)
    {
        this.client = client;
    }

    public static async Task<McpClientHarness> StartAsync(
        InstalledProduct product,
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "OKF-Todo installed contract tests",
            Command = product.McpServerPath,
            Arguments = ["--database-path", databasePath],
            WorkingDirectory = Path.GetDirectoryName(product.McpServerPath),
            ShutdownTimeout = TimeSpan.FromSeconds(10)
        });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
        return new McpClientHarness(client);
    }

    public async Task<IReadOnlyCollection<string>> ListToolNamesAsync(
        CancellationToken cancellationToken = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(tool => tool.Name).ToArray();
    }

    public async Task<JsonElement> CallAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await client.CallToolAsync(
            toolName,
            arguments,
            cancellationToken: cancellationToken);
        if (result.IsError is true)
        {
            throw new InvalidOperationException($"Installed MCP tool '{toolName}' returned an error: {ReadText(result)}");
        }

        using var document = JsonDocument.Parse(ReadText(result));
        return document.RootElement.Clone();
    }

    private static string ReadText(CallToolResult result)
    {
        var text = result.Content.OfType<TextContentBlock>().SingleOrDefault()?.Text;
        return !string.IsNullOrWhiteSpace(text)
            ? text
            : throw new InvalidDataException("Installed MCP tool result did not contain exactly one text block.");
    }

    public ValueTask DisposeAsync() => client.DisposeAsync();
}
