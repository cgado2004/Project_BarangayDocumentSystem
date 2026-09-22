using System.Configuration;
using System.Globalization;

namespace BarangayDocumentSystem.App;

/// <summary>
/// I read App.config through this one class.
///
/// My reason for wrapping it: ConfigurationManager returns null for a missing
/// key, and if I called it directly from ten different places then one typo in
/// the config file would give me a NullReferenceException somewhere deep in a
/// form, with nothing to tell me which setting was wrong.
///
/// Here every read goes through a helper that falls back to a sensible default
/// and never returns null. If my group-mate deletes a line from App.config by
/// accident, the app still starts and still works - it just uses the built-in
/// value instead of crashing on them.
/// </summary>
internal static class AppSettings
{
    /// <summary>
    /// I read a text setting. If the key is missing or blank I hand back the
    /// fallback rather than null, so the caller never has to null-check.
    /// </summary>
    public static string Text(string key, string fallback)
    {
        try
        {
            string? value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
        catch (ConfigurationErrorsException)
        {
            // The config file itself is malformed. I would rather the program
            // run on its defaults than refuse to open at all.
            return fallback;
        }
    }

    /// <summary>
    /// I read a peso amount.
    ///
    /// I parse with InvariantCulture on purpose. A Windows machine set to a
    /// locale that uses a comma for the decimal point would otherwise read
    /// "100.00" as one hundred thousand, and I am not willing to let a
    /// regional setting change what the barangay charges.
    /// </summary>
    public static decimal Money(string key, decimal fallback)
    {
        string raw = Text(key, string.Empty);
        if (raw.Length == 0) return fallback;

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
               && value >= 0
            ? value
            : fallback;   // a negative or unreadable fee is a mistake, so I ignore it
    }

    /// <summary>I read a whole number, again refusing anything negative.</summary>
    public static int Count(string key, int fallback)
    {
        string raw = Text(key, string.Empty);
        if (raw.Length == 0) return fallback;

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
               && value >= 0
            ? value
            : fallback;
    }

    /// <summary>
    /// True when App.config asks for the real database.
    ///
    /// I compare without case so "MySQL", "mysql" and "MySql" all work - I do
    /// not want my group-mates losing an afternoon to a capital letter.
    /// </summary>
    public static bool UseMySql() =>
        Text("Storage", "Memory").Equals("MySQL", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The MySQL connection string. I return an empty string when it is
    /// missing so the caller can show a clear message instead of handing a
    /// null to the driver.
    /// </summary>
    public static string ConnectionString()
    {
        try
        {
            return ConfigurationManager.ConnectionStrings["BarangayDb"]?.ConnectionString ?? string.Empty;
        }
        catch (ConfigurationErrorsException)
        {
            return string.Empty;
        }
    }
}
