// =====================================================================
//  PART:    Forms - a one-line text prompt (the rejection reason)
//  ORIGIN:  the group's shared design - first modelled in Draft - Jonathan F. Del Rosario,
//           given this place in the tree by Fdraft - Frent Dhieniel Raborar;
//           the code and comments in this file are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.1 content, header
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Quick input prompts - the one-question dialogs I use where a full form
/// would be ceremony.
///
/// The request screen uses <see cref="Text"/> to collect a rejection reason;
/// the money documents use <see cref="Money"/> wherever a bare number is
/// wanted. Both refuse an empty answer, because "the clerk skipped the
/// question" is not an answer the record can carry.
/// </summary>
public static class Prompt
{
    /// <summary>
    /// One free-text question. Returns the trimmed answer, or null when the
    /// user cancelled.
    /// </summary>
    public static string? Text(IWin32Window? owner, string title, string question)
    {
        using var form = new PromptForm(title, question, multiline: true);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Value : null;
    }

    /// <summary>
    /// One single-line question.
    /// </summary>
    public static string? Line(IWin32Window? owner, string title, string question)
    {
        using var form = new PromptForm(title, question, multiline: false);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Value : null;
    }

    /// <summary>
    /// One of a fixed list of choices. Returns the chosen item, or null when
    /// the user cancelled.
    /// </summary>
    public static string? Choice(IWin32Window? owner, string title, string question,
                                 params string[] options)
    {
        using var form = new ChoiceForm(title, question, options);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Value : null;
    }
}

/// <summary>The dialog behind <see cref="Prompt.Text"/> and
/// <see cref="Prompt.Line"/>.</summary>
internal class PromptForm : DialogBase
{
    public string Value { get; private set; } = string.Empty;

    private readonly TextBox _input = new();

    public PromptForm(string title, string question, bool multiline)
    {
        Text = title;
        SetDialogMetrics(title, 480, 280);

        var body = new SmoothPanel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };

        var q = new Label
        {
            Text = question,
            Font = Body,
            ForeColor = Ink,
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = System.Drawing.Color.Transparent
        };

        _input.Font = Body;
        _input.Dock = DockStyle.Top;
        _input.Multiline = multiline;
        _input.Height = multiline ? 84 : 30;
        _input.BackColor = Surface;
        _input.ForeColor = Ink;

        body.Controls.Add(_input);
        body.Controls.Add(q);

        var ok = UiFactory.PrimaryButton("Confirm", 120);
        ok.Accent = Danger;
        var cancel = UiFactory.SecondaryButton("Cancel", 110);
        var bar = UiFactory.ButtonBar(ok, cancel);

        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        ok.Click += (_, _) =>
        {
            if (_input.Text.Trim().Length == 0)
            {
                Dialog.Warn(this, "An answer is required.", title);
                return;
            }
            Value = _input.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };
        CancelButton = cancel;

        Controls.Add(body);
        Controls.Add(bar);
    }
}

/// <summary>The dialog behind <see cref="Prompt.Choice"/>.</summary>
internal class ChoiceForm : DialogBase
{
    public string Value { get; private set; } = string.Empty;

    private readonly ListBox _list = new();

    public ChoiceForm(string title, string question, string[] options)
    {
        Text = title;
        SetDialogMetrics(title, 460, 340);

        var body = new SmoothPanel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };

        var q = new Label
        {
            Text = question,
            Font = Body,
            ForeColor = Ink,
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = System.Drawing.Color.Transparent
        };

        _list.Font = Body;
        _list.Dock = DockStyle.Top;
        _list.Height = 150;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.BackColor = Surface;
        _list.ForeColor = Ink;
        foreach (string option in options) _list.Items.Add(option);
        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedItem is not null) Value = _list.SelectedItem.ToString() ?? string.Empty;
        };
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItem is not null)
            {
                Value = _list.SelectedItem.ToString() ?? string.Empty;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        body.Controls.Add(_list);
        body.Controls.Add(q);

        var ok = UiFactory.PrimaryButton("Confirm", 120);
        var cancel = UiFactory.SecondaryButton("Cancel", 110);
        var bar = UiFactory.ButtonBar(ok, cancel);

        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        ok.Click += (_, _) =>
        {
            if (Value.Length == 0)
            {
                Dialog.Warn(this, "Choose one of the options.", title);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        CancelButton = cancel;

        Controls.Add(body);
        Controls.Add(bar);
    }
}
