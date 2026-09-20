using System.Drawing;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Photino.Okf_Todo.Services;

public sealed class WindowPlacementService(ILogger<WindowPlacementService> logger)
{
    public void EnsureVisible(PhotinoWindow window)
    {
        try
        {
            // Inspect the actual native bounds after creation, including the window frame.
            // Photino reports these and monitor work areas in the same native coordinates.
            if (window.Maximized || window.Minimized)
                return;

            var bounds = new Rectangle(window.Location, window.Size);
            var workAreas = window.Monitors.Select(monitor => monitor.WorkArea).ToArray();
            var visibleBounds = FitToWorkArea(bounds, workAreas);
            if (visibleBounds == bounds)
                return;

            window.SetSize(visibleBounds.Size);
            window.SetLocation(visibleBounds.Location);
            logger.LogInformation("Adjusted restored window from {OldBounds} to {VisibleBounds} to fit the available desktop.", bounds, visibleBounds);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not adjust restored window to the available desktop.");
        }
    }

    internal static Rectangle FitToWorkArea(Rectangle bounds, IReadOnlyList<Rectangle> workAreas)
    {
        var validAreas = workAreas.Where(area => area.Width > 0 && area.Height > 0).ToArray();
        if (validAreas.Length == 0 || validAreas.Any(area => area.Contains(bounds)))
            return bounds;

        // Keep the window on the display containing most of it. If that display was
        // disconnected, use the nearest remaining display (including negative origins).
        var area = validAreas
            .OrderByDescending(candidate => IntersectionArea(bounds, candidate))
            .ThenBy(candidate => DistanceSquared(bounds, candidate))
            .First();
        var width = Math.Clamp(bounds.Width, 1, area.Width);
        var height = Math.Clamp(bounds.Height, 1, area.Height);
        return new Rectangle(
            Math.Clamp(bounds.Left, area.Left, area.Right - width),
            Math.Clamp(bounds.Top, area.Top, area.Bottom - height),
            width,
            height);
    }

    private static long IntersectionArea(Rectangle bounds, Rectangle area)
    {
        var intersection = Rectangle.Intersect(bounds, area);
        return (long)intersection.Width * intersection.Height;
    }

    private static double DistanceSquared(Rectangle bounds, Rectangle area)
    {
        var x = bounds.Left + bounds.Width / 2d;
        var y = bounds.Top + bounds.Height / 2d;
        var dx = x - Math.Clamp(x, area.Left, area.Right);
        var dy = y - Math.Clamp(y, area.Top, area.Bottom);
        return dx * dx + dy * dy;
    }
}
