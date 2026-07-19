using System.Diagnostics;
using System.Text.Json;

namespace OkfTodo.InstalledContractTests;

internal sealed class OkfCommandClient(InstalledProduct product, string databasePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<JsonElement> ExecuteAsync(
        string commandType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var messageId = $"installed-test-{Guid.NewGuid():N}";
        var request = JsonSerializer.Serialize(new
        {
            messageId,
            type = commandType,
            payload
        }, JsonOptions);

        var startInfo = new ProcessStartInfo
        {
            FileName = product.ApplicationPath,
            WorkingDirectory = product.RootPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("--okf-command");
        startInfo.ArgumentList.Add("--okf-database-path");
        startInfo.ArgumentList.Add(databasePath);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start installed application '{product.ApplicationPath}'.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.StandardInput.WriteAsync(request.AsMemory(), cancellationToken);
        process.StandardInput.Close();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Installed OKF command '{commandType}' did not exit within 45 seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Installed OKF command '{commandType}' exited with code {process.ExitCode}. " +
                $"stdout: {standardOutput} stderr: {standardError}");
        }

        using var response = JsonDocument.Parse(standardOutput);
        var root = response.RootElement;
        if (!root.GetProperty("ok").GetBoolean())
        {
            throw new InvalidOperationException($"Installed OKF command '{commandType}' failed: {standardOutput}");
        }

        if (!string.Equals(root.GetProperty("messageId").GetString(), messageId, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Installed OKF command '{commandType}' returned the wrong messageId.");
        }

        return root.GetProperty("payload").Clone();
    }
}
