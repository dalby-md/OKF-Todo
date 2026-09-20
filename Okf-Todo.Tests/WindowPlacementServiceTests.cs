using System.Drawing;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class WindowPlacementServiceTests
{
    [Theory]
    [InlineData(0, 0, 1946, 1226, 0, 0, 1920, 1128)]
    [InlineData(1800, 900, 800, 600, 1120, 528, 800, 600)]
    [InlineData(2200, 100, 1280, 800, 640, 100, 1280, 800)]
    [InlineData(-100, -50, 1000, 700, 0, 0, 1000, 700)]
    [InlineData(120, 80, 1440, 900, 120, 80, 1440, 900)]
    public void RestoredBoundsFitInsideDesktopIncludingTaskbar(
        int left, int top, int width, int height,
        int expectedLeft, int expectedTop, int expectedWidth, int expectedHeight)
    {
        var result = WindowPlacementService.FitToWorkArea(
            new Rectangle(left, top, width, height), [new Rectangle(0, 0, 1920, 1128)]);

        Assert.Equal(new Rectangle(expectedLeft, expectedTop, expectedWidth, expectedHeight), result);
    }

    [Fact]
    public void KeepsWindowOnNegativeOriginMonitorWithLargestOverlap()
    {
        var result = WindowPlacementService.FitToWorkArea(
            new Rectangle(-1400, 200, 1600, 1000),
            [new Rectangle(0, 0, 1920, 1040), new Rectangle(-1600, 0, 1600, 900)]);

        Assert.Equal(new Rectangle(-1600, 0, 1600, 900), result);
    }

    [Fact]
    public void PreservesBoundsWhenMonitorInformationIsUnavailable()
    {
        var bounds = new Rectangle(100, 100, 1280, 800);
        Assert.Equal(bounds, WindowPlacementService.FitToWorkArea(bounds, [Rectangle.Empty]));
    }
}
