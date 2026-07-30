using Microsoft.Extensions.Logging;
using Photino.NET;

namespace Photino.Okf_Todo.Services;

public sealed class ApplicationLifetimeService(ILogger<ApplicationLifetimeService> logger)
{
    private PhotinoWindow? window;

    public void Attach(PhotinoWindow photinoWindow)
    {
        window = photinoWindow;
    }

    public ApplicationCloseResult RequestClose()
    {
        var activeWindow = window
            ?? throw new InvalidOperationException("The application window is not ready.");

        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            logger.LogInformation("Closing OKF-Todo after a prepared database operation.");
            activeWindow.Close();
        });

        return new ApplicationCloseResult(true);
    }
}

public sealed record ApplicationCloseResult(bool Closing);
