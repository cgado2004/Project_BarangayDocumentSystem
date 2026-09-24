using System.Diagnostics;

namespace BarangayDocumentSystem.Helper;

/// <summary>
/// Where unhandled errors go when the program does not know what else to do.
///
/// Ported into v3.1.1 from Jonathan F. Del Rosario's Draft branch, adapted to
/// this project's conventions. The idea is his: an error the app survives
/// with is still an error somebody has to explain later, so it is written to
/// a log beside the user's own data rather than vanishing with the process.
///
/// The file lands in %LOCALAPPDATA%\BarangayDocumentSystem\errors.log - one
/// file per Windows account, never inside the program folder (which may not
/// be writable), and never inside the repository (which must stay clean).
///
/// Every call is wrapped so a failing logger can never turn one crash into
/// two: if the disk is full or the folder is locked, the log line is simply
/// lost and the original error carries on being the only problem.
/// </summary>
public static class ErrorLogger
{
    /// <summary>The full path of the log file, for messages that point the
    /// user at it.</summary>
    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarangayDocumentSystem", "errors.log");

    /// <summary>
    /// I append one exception, with a UTC timestamp, to the log. Never throws.
    /// </summary>
    public static void Write(Exception error)
    {
        try
        {
            string? folder = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.AppendAllText(LogFilePath,
                DateTime.Now.ToString("u") + Environment.NewLine +
                error + Environment.NewLine + Environment.NewLine);
        }
        catch (Exception loggingError)
        {
            // Logging failed - say so on the debug output and let the
            // original error continue to be the problem worth solving.
            Debug.WriteLine(loggingError);
            Debug.WriteLine(error);
        }
    }
}
