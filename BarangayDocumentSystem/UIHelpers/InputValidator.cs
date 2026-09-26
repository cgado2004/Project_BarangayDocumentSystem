using System.Windows.Forms;
using System;
using System.Linq;
namespace BarangayDocumentSystem.UIHelpers;

/// <summary>
/// Reusable field validation.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// ResidentForm previously repeated this block for every required field:
///
///     if (string.IsNullOrWhiteSpace(txtX.Text))
///     {
///         MessageBox.Show("X is required.", "Invalid input", ...);
///         txtX.Focus();
///         if (txtX is TextBox tb) tb.SelectAll();
///         return false;
///     }
///
/// Six near-identical copies, each a chance to forget the Focus() or mistype
/// the caption. Each is now a single line.
///
/// Every method returns TRUE when the field is acceptable, so validation reads
/// as a readable chain of conditions.
/// </summary>
public static class InputValidator
{
    /// <summary>Field must not be blank.</summary>
    public static bool Required(TextBox box, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(box.Text)) return true;

        Fail(box, $"{fieldName} is required.");
        return false;
    }

    /// <summary>A ComboBox must have a selection.</summary>
    public static bool RequiredSelection(ComboBox combo, string fieldName)
    {
        if (combo.SelectedIndex >= 0 || !string.IsNullOrWhiteSpace(combo.Text)) return true;

        Dialog.Warn($"Please choose a {fieldName}.");
        combo.Focus();
        return false;
    }

    /// <summary>Philippine-style contact number: digits, spaces, + and - only.</summary>
    public static bool ContactNumber(TextBox box, string fieldName = "Contact number")
    {
        if (!Required(box, fieldName)) return false;

        string value = box.Text.Trim();
        if (value.All(c => char.IsDigit(c) || c is '+' or '-' or ' ')) return true;

        Fail(box, $"{fieldName} may only contain digits, spaces, + and -.");
        return false;
    }

    /// <summary>Date must not be in the future.</summary>
    public static bool NotFuture(DateTimePicker picker, string fieldName)
    {
        if (picker.Value.Date <= DateTime.Today) return true;

        Dialog.Warn($"{fieldName} cannot be in the future.");
        picker.Focus();
        return false;
    }

    /// <summary>Plausible human age — guards against typos like year 1067.</summary>
    public static bool PlausibleBirthDate(DateTimePicker picker)
    {
        if (!NotFuture(picker, "Date of birth")) return false;
        if (picker.Value.Date >= DateTime.Today.AddYears(-130)) return true;

        Dialog.Warn("Please check the date of birth — that age is not plausible.");
        picker.Focus();
        return false;
    }

    /// <summary>One date must not precede another.</summary>
    public static bool NotBefore(DateTimePicker later, DateTimePicker earlier,
                                 string laterName, string earlierName)
    {
        if (later.Value.Date >= earlier.Value.Date) return true;

        Dialog.Warn($"{laterName} cannot be before {earlierName}.");
        later.Focus();
        return false;
    }

    /// <summary>
    /// KeyPress filter allowing digits plus the given extra characters.
    /// Wire to a TextBox's KeyPress event.
    ///
    /// This is layer ONE of validation. The submit-time checks above are layer
    /// two, and they are what actually guarantee safety — a KeyPress filter is
    /// bypassed entirely by pasting with Ctrl+V.
    /// </summary>
    public static void AllowDigitsOnly(KeyPressEventArgs e, string extraAllowed = "")
    {
        if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
        if (extraAllowed.Contains(e.KeyChar)) return;

        e.Handled = true;   // swallow the keystroke
    }

    private static void Fail(TextBox box, string message)
    {
        Dialog.Warn(message);
        box.Focus();
        box.SelectAll();
    }
}
