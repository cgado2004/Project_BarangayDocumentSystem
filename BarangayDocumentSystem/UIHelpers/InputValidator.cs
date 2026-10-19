using System.Windows.Forms;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BarangayDocumentSystem.UIHelpers;

public static class InputValidator
{
    public static bool Required(TextBox box, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(box.Text)) return true;
        Fail(box, $"{fieldName} is required.");
        return false;
    }

    // name check: letters, spaces, dots, hyphens only
    public static bool NameText(TextBox box, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(box.Text)) return true;

        string val = box.Text.Trim();
        if (val.All(c => char.IsLetter(c) || c == ' ' || c == '.' || c == '-')) return true;

        Fail(box, $"{fieldName} can only contain letters, spaces, hyphens, and periods.");
        return false;
    }

    // contact check: exactly 11 digits starting with '09'
    public static bool ContactNumber11(TextBox box, string fieldName = "Contact number")
    {
        if (!Required(box, fieldName)) return false;

        string val = box.Text.Trim();
        if (val.Length == 11 && val.All(char.IsDigit) && val.StartsWith("09")) return true;

        Fail(box, $"{fieldName} must be an 11-digit number starting with '09' (e.g. 09171234567).");
        return false;
    }

    public static bool RequiredSelection(ComboBox combo, string fieldName)
    {
        if (combo.SelectedIndex >= 0 || !string.IsNullOrWhiteSpace(combo.Text)) return true;
        Dialog.Warn($"Please choose a {fieldName}.");
        combo.Focus();
        return false;
    }

    public static bool NotFuture(DateTimePicker picker, string fieldName)
    {
        if (picker.Value.Date <= DateTime.Today) return true;
        Dialog.Warn($"{fieldName} cannot be in the future.");
        picker.Focus();
        return false;
    }

    public static bool PlausibleBirthDate(DateTimePicker picker)
    {
        if (!NotFuture(picker, "Date of birth")) return false;
        if (picker.Value.Date >= DateTime.Today.AddYears(-130)) return true;
        Dialog.Warn("Please check the date of birth — age is not plausible.");
        picker.Focus();
        return false;
    }

    public static bool NotBefore(DateTimePicker later, DateTimePicker earlier, string laterName, string earlierName)
    {
        if (later.Value.Date >= earlier.Value.Date) return true;
        Dialog.Warn($"{laterName} cannot be before {earlierName}.");
        later.Focus();
        return false;
    }

    // keypress filters
    public static void AllowDigitsOnly(KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
        e.Handled = true;
    }

    public static void AllowNameCharsOnly(KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar) || char.IsLetter(e.KeyChar) || e.KeyChar == ' ' || e.KeyChar == '.' || e.KeyChar == '-') return;
        e.Handled = true;
    }

    private static void Fail(TextBox box, string message)
    {
        Dialog.Warn(message);
        box.Focus();
        box.SelectAll();
    }

  
    public static bool ValidSuffix(TextBox box)
    {
        if (string.IsNullOrWhiteSpace(box.Text)) return true;

        string val = box.Text.Trim().TrimEnd('.').ToUpperInvariant();
        string[] allowed = { "JR", "SR", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        if (Array.IndexOf(allowed, val) >= 0) return true;

        Fail(box, "Suffix must be one of Jr., Sr., II, III, IV, V — or leave it blank.");
        return false;
    }

    // ── Philippe
    private static readonly Regex NamePattern =
        new Regex(@"^[a-zA-ZÀ-ÿ' \-\.]+$", RegexOptions.Compiled);

    public static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && NamePattern.IsMatch(name);
    }
}