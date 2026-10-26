using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;









internal static class CueBanner
{
    private const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);


    public static void Set(TextBox textBox, string text)
    {
        if (textBox is null) throw new ArgumentNullException(nameof(textBox));






        if (textBox.IsHandleCreated)
            Apply(textBox, text);
        else
            textBox.HandleCreated += (_, _) => Apply(textBox, text);
    }

    private static void Apply(TextBox textBox, string text) =>
        SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, text ?? string.Empty);
}
