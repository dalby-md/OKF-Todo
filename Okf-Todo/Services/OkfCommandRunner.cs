using System.Text.Json;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Bridge;

namespace Photino.Okf_Todo.Services;

public sealed class OkfCommandRunner(
    BridgeMessageHandler messageHandler,
    ILogger<OkfCommandRunner> logger)
{
    public async Task<OkfCommandResult> RunAsync(
        string requestJson,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing command from the OKF command adapter.");

        var responseJson = await messageHandler.HandleAsync(requestJson, cancellationToken);
        using var response = JsonDocument.Parse(responseJson);
        var succeeded = response.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean();

        logger.LogInformation(
            "OKF command adapter completed with success status {Succeeded}.",
            succeeded);

        return new OkfCommandResult(responseJson, succeeded);
    }
}

public sealed record OkfCommandResult(string ResponseJson, bool Succeeded);
