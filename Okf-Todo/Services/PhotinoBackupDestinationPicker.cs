using Photino.NET;

namespace Photino.Okf_Todo.Services;

public interface IBackupDestinationPicker
{
    Task<string?> PickAsync(
        string suggestedFileName,
        string? initialDirectory,
        CancellationToken cancellationToken);
}

public sealed class PhotinoBackupDestinationPicker : IBackupDestinationPicker
{
    private PhotinoWindow? window;

    public void Attach(PhotinoWindow photinoWindow)
    {
        window = photinoWindow;
    }

    public async Task<string?> PickAsync(
        string suggestedFileName,
        string? initialDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var activeWindow = window
            ?? throw new InvalidOperationException("The native file dialog is not ready.");
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var defaultDirectory = !string.IsNullOrWhiteSpace(initialDirectory)
            && Directory.Exists(initialDirectory)
                ? initialDirectory
                : documentsPath;
        var selectedPath = await activeWindow.ShowSaveFileAsync(
            $"Back up OKF Todo database as {suggestedFileName}",
            defaultDirectory,
            [("SQLite database", ["db"])]);

        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(selectedPath) ? null : selectedPath;
    }
}
