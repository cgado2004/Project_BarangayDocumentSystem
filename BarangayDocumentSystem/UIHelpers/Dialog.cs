// =====================================================================
//  PART:    UIHelpers - every message box in the app, in one voice
//  ORIGIN:  the group's shared design - first modelled in Draft - Jonathan F. Del Rosario,
//           given this place in the tree by Fdraft - Frent Dhieniel Raborar;
//           the code and comments in this file are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.1 content (DialogBase, metrics), header
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Drawing;
using System.Windows.Forms;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.UIHelpers;

/// <summary>
/// Standardised message boxes, one place for every "the app speaks to the
/// user" moment.
///
/// v3 had a dozen MessageBox.Show calls with their own titles, icons and
/// phrasing, and they had drifted: some said "Missing information", some
/// said nothing useful at all. v3.1 routes them all through here, so every
/// message the app can produce has the same voice, the same icons, and a
/// title that says which screen is talking.
/// </summary>
public static class Dialog
{
    /// <summary>Plain information.</summary>
    public static void Info(IWin32Window? owner, string message, string title = "Information") =>
        MessageBox.Show(owner, message, title,
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    /// <summary>Something the user must fix before going on.</summary>
    public static void Warn(IWin32Window? owner, string message, string title = "Check your entries") =>
        MessageBox.Show(owner, message, title,
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Something went wrong that the user cannot fix here.</summary>
    public static void Error(IWin32Window? owner, string message, string title = "Error") =>
        MessageBox.Show(owner, message, title,
            MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>A yes/no question. True when the user said yes.</summary>
    public static bool Confirm(IWin32Window? owner, string message, string title = "Confirm") =>
        MessageBox.Show(owner, message, title,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>A yes/no question where yes is destructive, so the icon
    /// carries the warning.</summary>
    public static bool ConfirmDanger(IWin32Window? owner, string message, string title = "Confirm") =>
        MessageBox.Show(owner, message, title,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    /// <summary>The one-line complaint a failed validation shows. It names
    /// the field, so the user is not left hunting for what was wrong.</summary>
    public static void FieldProblem(IWin32Window? owner, string field, string problem) =>
        Warn(owner, $"{field}: {problem}", "Missing or invalid information");
}

/// <summary>
/// The shared setup for every dialog form in the app.
///
/// I make them sizable, give them a MinimumSize and turn on AutoScroll, so a
/// dialog can never end up with its OK button off the bottom of a small
/// screen. That is the usual way WinForms dialogs break on somebody else's
/// machine, and it is invisible to me on mine.
///
/// v3.1 note: the concrete forms are partial classes with a parameterless
/// constructor and an InitializeComponent, which is the shape the Visual
/// Studio designer needs to open them on its design surface.
/// </summary>
public class DialogBase : Form
{
    protected DialogBase()
    {
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        BackColor = Canvas;
        Font = Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        Padding = new Padding(22);
        KeyPreview = true;
    }

    /// <summary>
    /// The metrics the designer half would otherwise set through the base
    /// constructor: the title and the starting size, plus a floor under the
    /// size so the dialog can shrink but never clip its buttons.
    /// </summary>
    protected void SetDialogMetrics(string title, int width, int height)
    {
        Text = title;
        ClientSize = new Size(width, height);
        MinimumSize = new Size(Math.Max(360, width - 60), Math.Max(240, height - 60));
    }
}
