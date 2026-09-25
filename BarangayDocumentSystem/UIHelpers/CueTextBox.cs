using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

/// <summary>Framework-compatible placeholder, reapplied after handle recreation.</summary>
public sealed class CueTextBox : TextBox
{
    private string _placeholder = string.Empty;
    public string PlaceholderText
    {
        get => _placeholder;
        set { _placeholder = value ?? string.Empty; if (IsHandleCreated) ApplyPlaceholder(); }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyPlaceholder();
    }

    private void ApplyPlaceholder() => SendMessage(Handle, 0x1501, IntPtr.Zero, _placeholder);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr parameter, string text);
}
