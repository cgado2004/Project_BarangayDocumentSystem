using System.Windows.Forms;
using System.Drawing;
using System;
namespace BarangayDocumentSystem.Forms;

/// <summary>
/// A small modal text-input dialog, built entirely in code.
///
/// WinForms has MessageBox for output but no built-in input box, so this
/// fills the gap. Everything is created and wired programmatically to show
/// that the designer is a convenience, not a requirement.
/// </summary>
public static class Prompt
{
    /// <summary>Returns the typed text, or an empty string if cancelled.</summary>
    public static string Show(IWin32Window owner, string title, string question)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(460, 190),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = question,
            Location = new Point(18, 18),
            Size = new Size(424, 46)
        };

        var textBox = new TextBox
        {
            Location = new Point(18, 70),
            Size = new Size(424, 27),
            MaxLength = 200
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(226, 120),
            Size = new Size(104, 38)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(338, 120),
            Size = new Size(104, 38)
        };

        // Controls.Add is what actually puts them on the form — forgetting it
        // is a classic WinForms bug: the control exists but never appears.
        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        form.AcceptButton = ok;    // Enter
        form.CancelButton = cancel; // Esc

        return form.ShowDialog(owner) == DialogResult.OK
            ? textBox.Text.Trim()
            : string.Empty;
    }
}
