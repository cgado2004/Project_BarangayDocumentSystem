namespace BarangayDocumentSystem.Helper;

/// <summary>
/// Every message box in the application.
///
/// ── DRY: the clearest duplication in the original code ──────────────────
/// The v1 UI contained EIGHTEEN MessageBox.Show calls. Seven of them were
/// near-identical copies of:
///
///     MessageBox.Show("Please select a resident first.", "No selection",
///                     MessageBoxButtons.OK, MessageBoxIcon.Information);
///
/// each repeating the caption, the button set and the icon. They had already
/// drifted — some said "Please select a resident first.", others "Please
/// select a request first.", with inconsistent captions.
///
/// Six methods now cover every case. Call sites shrink from four arguments to
/// one, captions and icons can never disagree, and changing the house style of
/// all dialogs is a single edit here.
/// </summary>
public static class Dialog
{
    /// <summary>Something the user must fix before continuing.</summary>
    public static void Warn(string message, string caption = "Invalid input") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    /// <summary>Neutral information.</summary>
    public static void Info(string message, string caption = "Information") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

    /// <summary>Something went wrong.</summary>
    public static void Error(string message, string caption = "Error") =>
        MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>
    /// Replaces the seven hand-written "Please select X first" boxes.
    /// </summary>
    public static void SelectFirst(string what) =>
        MessageBox.Show($"Please select a {what} first.", "No selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

    /// <summary>Yes/No question. True when the user chooses Yes.</summary>
    public static bool Confirm(string message, string caption = "Please confirm") =>
        MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Yes/No for a destructive action — warning icon, No preselected.</summary>
    public static bool ConfirmDestructive(string message, string caption = "Confirm") =>
        MessageBox.Show(message, caption, MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
            == DialogResult.Yes;
}
