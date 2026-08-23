using Microsoft.Extensions.Logging.Abstractions;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class ExternalLinkServiceTests
{
    [Theory]
    [InlineData("https://github.com/dalby-md/Okf-Todo")]
    public void Open_LaunchesSupportedProjectLink(string url)
    {
        var launcher = new RecordingExternalLinkLauncher();
        var service = new ExternalLinkService(launcher, NullLogger<ExternalLinkService>.Instance);

        var result = service.Open(new ExternalLinkOpenRequest(url));

        Assert.Equal(url, result.Url);
        Assert.Equal(url, launcher.OpenedUri?.AbsoluteUri.TrimEnd('/'));
    }

    [Fact]
    public void Open_RejectsUnsupportedLinks()
    {
        var launcher = new RecordingExternalLinkLauncher();
        var service = new ExternalLinkService(launcher, NullLogger<ExternalLinkService>.Instance);

        var exception = Assert.Throws<BridgeException>(() =>
            service.Open(new ExternalLinkOpenRequest("https://example.com")));

        Assert.Equal("UnsupportedExternalLink", exception.Code);
        Assert.Null(launcher.OpenedUri);
    }

    private sealed class RecordingExternalLinkLauncher : IExternalLinkLauncher
    {
        public Uri? OpenedUri { get; private set; }

        public void Open(Uri uri)
        {
            OpenedUri = uri;
        }
    }
}
