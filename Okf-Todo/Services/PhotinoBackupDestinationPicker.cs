using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        var selectedPath = OperatingSystem.IsWindows()
            ? await ShowWindowsSaveFileAsync(
                activeWindow.WindowHandle,
                suggestedFileName,
                defaultDirectory)
            : await activeWindow.ShowSaveFileAsync(
                "Back up OKF Todo database",
                defaultDirectory,
                [("SQLite database", ["db"])]);

        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(selectedPath) ? null : selectedPath;
    }

    [SupportedOSPlatform("windows")]
    private static Task<string?> ShowWindowsSaveFileAsync(
        IntPtr ownerHandle,
        string suggestedFileName,
        string initialDirectory)
    {
        var completion = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogThread = new Thread(() =>
        {
            try
            {
                completion.SetResult(ShowWindowsSaveFile(
                    ownerHandle,
                    suggestedFileName,
                    initialDirectory));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        dialogThread.SetApartmentState(ApartmentState.STA);
        dialogThread.Start();
        return completion.Task;
    }

    [SupportedOSPlatform("windows")]
    private static string? ShowWindowsSaveFile(
        IntPtr ownerHandle,
        string suggestedFileName,
        string initialDirectory)
    {
        const int fileBufferCharacters = 32768;
        var fileBuffer = Marshal.AllocHGlobal(fileBufferCharacters * sizeof(char));
        var filterBuffer = Marshal.StringToHGlobalUni(
            "SQLite database (*.db)\0*.db\0All files (*.*)\0*.*\0\0");

        try
        {
            var suggestedFileNameCharacters = suggestedFileName.ToCharArray();
            Marshal.Copy(suggestedFileNameCharacters, 0, fileBuffer, suggestedFileNameCharacters.Length);
            Marshal.WriteInt16(fileBuffer, suggestedFileNameCharacters.Length * sizeof(char), 0);

            var dialog = new OpenFileName
            {
                StructSize = Marshal.SizeOf<OpenFileName>(),
                OwnerHandle = ownerHandle,
                Filter = filterBuffer,
                FilterIndex = 1,
                File = fileBuffer,
                MaxFile = fileBufferCharacters,
                InitialDirectory = initialDirectory,
                Title = "Back up OKF Todo database",
                Flags = OpenFileNameFlags.OverwritePrompt
                    | OpenFileNameFlags.PathMustExist
                    | OpenFileNameFlags.NoChangeDirectory
                    | OpenFileNameFlags.Explorer
                    | OpenFileNameFlags.EnableSizing,
                DefaultExtension = "db"
            };

            if (GetSaveFileName(ref dialog))
            {
                return Marshal.PtrToStringUni(fileBuffer);
            }

            var error = CommDlgExtendedError();
            if (error == 0)
            {
                return null;
            }

            throw new Win32Exception(
                unchecked((int)error),
                $"The native backup dialog failed with error 0x{error:X}.");
        }
        finally
        {
            Marshal.FreeHGlobal(filterBuffer);
            Marshal.FreeHGlobal(fileBuffer);
        }
    }

    [Flags]
    private enum OpenFileNameFlags : uint
    {
        OverwritePrompt = 0x00000002,
        NoChangeDirectory = 0x00000008,
        PathMustExist = 0x00000800,
        Explorer = 0x00080000,
        EnableSizing = 0x00800000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int StructSize;
        public IntPtr OwnerHandle;
        public IntPtr InstanceHandle;
        public IntPtr Filter;
        public IntPtr CustomFilter;
        public int MaxCustomFilter;
        public int FilterIndex;
        public IntPtr File;
        public int MaxFile;
        public IntPtr FileTitle;
        public int MaxFileTitle;
        public string? InitialDirectory;
        public string? Title;
        public OpenFileNameFlags Flags;
        public short FileOffset;
        public short FileExtension;
        public string? DefaultExtension;
        public IntPtr CustomData;
        public IntPtr Hook;
        public string? TemplateName;
        public IntPtr Reserved;
        public int ReservedSize;
        public int FlagsExtended;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSaveFileName([In, Out] ref OpenFileName dialog);

    [DllImport("comdlg32.dll")]
    private static extern uint CommDlgExtendedError();
}
