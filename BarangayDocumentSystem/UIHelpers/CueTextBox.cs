// =====================================================================
//  PART:    UIHelpers - a text box with placeholder text
//  ORIGIN:  leader_draft - Clint Wood Gado (Fdraft has CueBanner.cs by Frent for the same job)
//  EDITS:   Clint Wood Gado - header only
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

/// <summary>
/// A text box with grey placeholder text ("Search name, purok...").
///
/// .NET Framework's TextBox has no PlaceholderText, so I send the
/// EM_SETCUEBANNER message myself. Windows forgets the cue whenever the
/// handle is recreated (a theme or DPI change does that), which is why I
/// apply it again in OnHandleCreated instead of once in the constructor.
/// </summary>
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
