using BarangayDocumentSystem.Forms;

namespace BarangayDocumentSystem;

internal static class Program
{
    /// <summary>
    /// Entry point. [STAThread] sets a Single-Threaded Apartment, required by
    /// WinForms for COM interop — the clipboard, common dialogs, and printing
    /// will not work without it.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Starts the Windows MESSAGE LOOP: the loop that pulls events from the
        // OS queue and dispatches them to controls. In an event-driven program
        // this IS the repetition construct.
        Application.Run(new MainForm());
    }
}
