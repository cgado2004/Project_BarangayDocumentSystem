using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

/// <summary>
/// Grey placeholder text inside an empty <see cref="TextBox"/> — what
/// <c>TextBox.PlaceholderText</c> gives you for free on .NET 6+ WinForms.
/// .NET Framework's TextBox has no such property, so this sends the native
/// Win32 control the same message Windows itself uses for it
/// (<c>EM_SETCUEBANNER</c>), which every edit control has supported since
/// Windows Vista.
/// </summary>
internal static class CueBanner
{
    private const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    /// <summary>Shows <paramref name="text"/> in light grey while the box is empty and unfocused.</summary>
    public static void Set(TextBox textBox, string text)
    {
        if (textBox is null) throw new ArgumentNullException(nameof(textBox));

        // The native window must exist before it can be sent a message. Designer
        // code runs before the control is parented, so its handle usually isn't
        // created yet — wait for HandleCreated instead of forcing it early
        // (forcing .Handle here would create the control as its own top-level
        // window, which WinForms then has to re-parent).
        if (textBox.IsHandleCreated)
            Apply(textBox, text);
        else
            textBox.HandleCreated += (_, _) => Apply(textBox, text);
    }

    private static void Apply(TextBox textBox, string text) =>
        SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, text ?? string.Empty);
}
