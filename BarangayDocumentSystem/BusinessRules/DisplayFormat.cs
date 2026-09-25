using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace BarangayDocumentSystem.BusinessRules;

/// <summary>
/// One home for every display format I use across the system.
///
/// I pulled these together after my professor pointed out the same peso
/// format being retyped at a dozen call sites. Two currency bugs had already
/// slipped through that way - once "PHP " across five files, and once a plain
/// ASCII "P" on the payment screen - because each copy could drift without
/// the others noticing.
///
/// Now there is one method per format. If a format is wrong, it is wrong in
/// exactly one place and I fix it once.
/// </summary>
public static class DisplayFormat
{
    /// <summary>The Philippine peso sign, U+20B1. I use this and never a plain
    /// ASCII "P" - I have already had to fix that bug twice.</summary>
    public const string PesoSign = "₱";

    public const string MoneyFormat = "N2";

    /// <summary>The formal date I print on certificates: "September 16, 2026".</summary>
    public const string LongDateFormat = "MMMM d, yyyy";

    /// <summary>The sortable date I use in grids: "2026-09-16". Year first, so
    /// sorting the column alphabetically also sorts it chronologically.</summary>
    public const string GridDateFormat = "yyyy-MM-dd";

    /// <summary>The date and time I stamp on a document footer.</summary>
    public const string StampFormat = "MMMM d, yyyy  h:mm tt";

    /// <summary>Money with thousands separators, no symbol - for the inside
    /// of sentences, where the peso sign would repeat.</summary>
    public const string PlainMoneyFormat = "#,##0.00";

    /// <summary>Hours with up to one decimal, for facility rentals:
    /// "3" or "2.5".</summary>
    public const string HoursFormat = "0.#";

    /// <summary>Thousands with no decimals, for the income a declarant
    /// swears to on a cedula: "120,000".</summary>
    public const string IncomeFormat = "#,##0";

    /// <summary>₱1,234.50 - the only way I render money for display, so every
    /// screen shows an amount the same way.</summary>
    public static string Peso(decimal amount) =>
        $"{PesoSign}{amount.ToString(MoneyFormat)}";

    /// <summary>1,234.50 with no currency sign, for use inside a sentence
    /// that already carries one.</summary>
    public static string PlainMoney(decimal amount) =>
        amount.ToString(PlainMoneyFormat);

    /// <summary>"₱1,234.50", or the word "FREE" when I have waived the fee. I
    /// spell it out because "₱0.00" looks like an error to a resident.</summary>
    public static string PesoOrFree(decimal amount) =>
        amount > 0 ? Peso(amount) : "FREE";

    public static string LongDate(DateTime value) => value.ToString(LongDateFormat);

    public static string GridDate(DateTime value) => value.ToString(GridDateFormat);

    public static string Stamp(DateTime value) => value.ToString(StampFormat);

    public static string Hours(decimal hours) => hours.ToString(HoursFormat);

    public static string Income(decimal amount) => amount.ToString(IncomeFormat);
}
