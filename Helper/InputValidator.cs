using System.Globalization;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Helper;

/// <summary>
/// The outcome of one validation rule: whether it passed, and the sentence I
/// show when it did not. A record rather than an exception, because a form
/// checks many rules in a row and wants to report the FIRST failure, not
/// unwind the stack at it.
/// </summary>
public readonly record struct Check(bool Ok, string Error)
{
    public static Check Pass => new(true, string.Empty);

    public static Check Fail(string error) => new(false, error);

    public static explicit operator bool(Check check) => check.Ok;
}

/// <summary>
/// Field validation logic, v3.1's answer to a bug I kept finding: the v3
/// forms each carried their own half-dozen if-statements, and the resident
/// form and the request form disagreed about what a valid phone number is.
///
/// Every rule the app applies to user input lives here, so the rules are
/// written once and every form enforces the same ones. A bad record is
/// refused at the door, which is much easier than cleaning it up later.
/// </summary>
public static class InputValidator
{
    // -----------------------------------------------------------------
    //  Text
    // -----------------------------------------------------------------

    /// <summary>A required single-line field with a sensible length. I trim
    /// first - trailing spaces in a surname are not data, they are typos.</summary>
    public static Check RequiredText(string value, string field, int maxLength = 60)
    {
        string text = value.Trim();
        if (text.Length == 0)
            return Check.Fail($"{field} is required.");
        if (text.Length > maxLength)
            return Check.Fail($"{field} is too long (at most {maxLength} characters).");
        return Check.Pass;
    }

    /// <summary>A name part. Beyond being required text, it must contain at
    /// least one letter and nothing but letters, spaces, hyphens, periods and
    /// apostrophes - the characters Philippine names actually use (De los
    /// Santos, O'Brien, Peña is covered separately by Unicode letters).</summary>
    public static Check NamePart(string value, string field, bool required = true)
    {
        string text = value.Trim();
        if (text.Length == 0)
            return required ? Check.Fail($"{field} is required.") : Check.Pass;

        if (text.Length > 60)
            return Check.Fail($"{field} is too long.");

        if (!text.Any(char.IsLetter))
            return Check.Fail($"{field} must contain at least one letter.");

        foreach (char c in text)
        {
            bool allowed = char.IsLetter(c) || c == ' ' || c == '-' ||
                           c == '.' || c == '\'';
            if (!allowed)
                return Check.Fail($"{field} contains a character that cannot appear in a name (\"{c}\").");
        }

        return Check.Pass;
    }

    /// <summary>A Philippine mobile or landline number: 09XXXXXXXXX, an
    /// 08XX landline with area code, or the same with +63 / 0 prefixes and
    /// dashes or spaces. Optional - an empty contact field is allowed, since
    /// plenty of residents have no phone.</summary>
    public static Check PhoneNumber(string value, string field = "Contact number")
    {
        string text = new(value.Where(char.IsDigit).ToArray());

        if (text.Length == 0)
            return Check.Pass;

        if (value.Trim().StartsWith("+63")) text = "0" + text.Substring(2);

        if (text.Length == 11 && text.StartsWith("09")) return Check.Pass;
        if (text.Length == 10 && text.StartsWith("0"))  return Check.Pass;

        return Check.Fail($"{field} must be a Philippine mobile number (09XXXXXXXXX) " +
                          "or a landline with its area code (e.g. 0842141234).");
    }

    /// <summary>The purpose printed on a certificate. It has to be fit for
    /// paper: required, and no placeholder markers left in it.</summary>
    public static Check Purpose(string value, string field = "Purpose")
    {
        string text = value.Trim();
        if (text.Length == 0)
            return Check.Fail($"{field} is required - it is printed on the document.");
        if (text.Length > 160)
            return Check.Fail($"{field} is too long for the certificate (at most 160 characters).");
        if (text.Contains('[', StringComparison.Ordinal) || text.Contains(']', StringComparison.Ordinal))
            return Check.Fail($"{field} still contains placeholder brackets.");
        return Check.Pass;
    }

    /// <summary>An official receipt number. Money must never be recorded
    /// against something untraceable, so I require at least three characters
    /// of letters, digits, dashes or spaces - enough to be findable, loose
    /// enough to match whatever the treasury's numbering looks like.</summary>
    public static Check ReceiptNumber(string value, string field = "Official receipt number")
    {
        string text = value.Trim();
        if (text.Length < 3)
            return Check.Fail($"{field} is required - a payment must be traceable to a receipt.");
        if (text.Length > 40)
            return Check.Fail($"{field} is too long.");

        foreach (char c in text)
        {
            if (!(char.IsLetterOrDigit(c) || c == '-' || c == ' ' || c == '/'))
                return Check.Fail($"{field} may contain letters, numbers, dashes and slashes only.");
        }

        return Check.Pass;
    }

    // -----------------------------------------------------------------
    //  Numbers
    // -----------------------------------------------------------------

    /// <summary>A money amount read from a text field.</summary>
    public static Check Money(string value, string field, out decimal amount,
                              decimal min = 0m, decimal max = 1_000_000m)
    {
        amount = 0m;
        string text = value.Trim();

        if (text.Length == 0)
            return Check.Fail($"{field} is required.");

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) &&
            !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
            return Check.Fail($"{field} must be a number, for example 250.00.");

        if (amount < min)
            return Check.Fail($"{field} cannot be less than {Service.DisplayFormat.Peso(min)}.");
        if (amount > max)
            return Check.Fail($"{field} cannot be more than {Service.DisplayFormat.Peso(max)}.");

        return Check.Pass;
    }

    /// <summary>An assessed amount for a variable-fee document. The minimum
    /// is 0.01 rather than 0: an assessed fee of zero is a mistake, because
    /// free documents are free by RULE, not by typing 0 into a box.</summary>
    public static Check AssessedAmount(decimal value, string field, decimal max = 100_000m)
    {
        if (value <= 0m)
            return Check.Fail($"{field} must be more than zero. If the document is free, it is free by rule - choose the document that says so.");
        if (value > max)
            return Check.Fail($"{field} is beyond the plausible range (at most {Service.DisplayFormat.Peso(max)}).");
        return Check.Pass;
    }

    /// <summary>Hours of facility use.</summary>
    public static Check Hours(decimal value, string field = "Hours of use",
                              decimal max = 720m)
    {
        if (value <= 0m)
            return Check.Fail($"{field} must be more than zero.");
        if (value > max)
            return Check.Fail($"{field} is beyond the plausible range (a month is about 720 hours).");
        return Check.Pass;
    }

    // -----------------------------------------------------------------
    //  Dates
    // -----------------------------------------------------------------

    /// <summary>A date of birth that makes a living person: not in the future,
    /// not more than 130 years ago.</summary>
    public static Check DateOfBirth(DateTime value)
    {
        if (value > DateTime.Today)
            return Check.Fail("The date of birth cannot be in the future.");
        if (value < DateTime.Today.AddYears(-130))
            return Check.Fail("The date of birth is more than 130 years in the past.");
        return Check.Pass;
    }

    /// <summary>Residency cannot predate the resident. A person cannot start
    /// living somewhere before they were born.</summary>
    public static Check ResidencyAfterBirth(DateTime dateOfBirth, DateTime dateOfResidency)
    {
        if (dateOfResidency.Date < dateOfBirth.Date)
            return Check.Fail("The date of residency cannot be earlier than the date of birth.");
        if (dateOfResidency > DateTime.Today)
            return Check.Fail("The date of residency cannot be in the future.");
        return Check.Pass;
    }

    /// <summary>The purok must be one of the real fourteen, so a typo cannot
    /// invent a place the barangay does not have.</summary>
    public static Check Purok(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Check.Fail("Please choose a purok.");
        if (!Puroks.Contains(value.Trim()))
            return Check.Fail($"\"{value.Trim()}\" is not one of the puroks of Barangay Magugpo Poblacion.");
        return Check.Pass;
    }
}
