using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Photino.Okf_Todo.Services;

public sealed class ExternalLinkService(
    IExternalLinkLauncher launcher,
    ILogger<ExternalLinkService> logger)
{
    private static readonly HashSet<string> SupportedUrls = new(StringComparer.Ordinal)
    {
        "https://github.com/dalby-md/Okf-Todo"
    };

    public ExternalLinkOpenResult Open(ExternalLinkOpenRequest request)
    {
        var url = request.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url) || !SupportedUrls.Contains(url))
        {
            throw new BridgeException(
                "UnsupportedExternalLink",
                "The requested external link is not supported.");
        }

        try
        {
            launcher.Open(new Uri(url, UriKind.Absolute));
            logger.LogInformation("Opened external project link {ExternalUrl}.", url);
            return new ExternalLinkOpenResult(url);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not open external project link {ExternalUrl}.", url);
            throw new BridgeException(
                "ExternalLinkOpenFailed",
                "Could not open the project link in the default browser.");
        }
    }
}

public interface IExternalLinkLauncher
{
    void Open(Uri uri);
}

public sealed class SystemExternalLinkLauncher : IExternalLinkLauncher
{
    public void Open(Uri uri)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("The operating system did not start a browser process.");
        }
    }
}

public sealed record ExternalLinkOpenRequest(string? Url);

public sealed record ExternalLinkOpenResult(string Url);
