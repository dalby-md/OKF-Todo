using System.Text.Json;
using ModelContextProtocol;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

internal static class McpToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<TResult> ExecuteAsync<TResult>(
        ApplicationCommandService commandService,
        string commandType,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await commandService.ExecuteAsync(
                new ApplicationCommand(commandType, JsonSerializer.SerializeToElement(payload, JsonOptions)),
                cancellationToken);

            return result is TResult typedResult
                ? typedResult
                : throw new McpException($"Application command '{commandType}' returned an unexpected result.");
        }
        catch (ValidationException exception)
        {
            throw new McpException(exception.Message, exception);
        }
        catch (BridgeException exception)
        {
            throw new McpException(exception.Message, exception);
        }
    }
}
