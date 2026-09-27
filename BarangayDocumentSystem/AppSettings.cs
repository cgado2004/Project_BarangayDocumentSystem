// =====================================================================
//  PART:    Configuration - the one reader of App.config
//  ORIGIN:  leader_draft - Clint Wood Gado (lived inside my Program.cs in v3.1)
//  EDITS:   Clint Wood Gado - v3.2: its own file; Flag() and a named
//           ConnectionString() so DatabaseSettings can read through it
//           instead of through System.Configuration
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BarangayDocumentSystem;

/// <summary>
/// I read App.config through this one class.
///
/// I read the file with System.Xml.Linq instead of the
/// System.Configuration.ConfigurationManager, so the only package the
/// solution needs to restore is the MySQL driver - and, because I control
/// the parse, a malformed file degrades to the built-in defaults instead of
/// throwing from somewhere deep inside a form.
///
/// Every read goes through a helper that falls back to a sensible default
/// and never returns null. If a group-mate deletes a line from App.config by
/// accident, the app still starts and still works - it just uses the
/// built-in value instead of crashing on them.
///
/// Frent's DatabaseSettings used to read the same file through
/// ConfigurationManager. Two readers of one file is a DRY violation, so it
/// now reads through here too.
/// </summary>
internal static class AppSettings
{
    private static XDocument? _doc;
    private static bool _loaded;

    /// <summary>
    /// The configuration, loaded once. MSBuild names the copied file after
    /// the assembly, so I probe the names the build produces as well as
    /// the plain one, and I take the first that exists and parses.
    /// </summary>
    private static XDocument? Document()
    {
        if (_loaded) return _doc;
        _loaded = true;

        string[] candidates =
        {
            "BarangayDocumentSystem.exe.config",
            "BarangayDocumentSystem.dll.config",
            "App.config"
        };

        foreach (string name in candidates)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, name);
                if (!File.Exists(path)) continue;
                _doc = XDocument.Load(path);
                break;
            }
            catch
            {
                // unreadable or malformed - try the next candidate, and if
                // none of them work, the defaults answer every read.
            }
        }

        return _doc;
    }

    /// <summary>
    /// I read a text setting. If the key is missing or blank I hand back the
    /// fallback rather than null, so the caller never has to null-check.
    /// </summary>
    public static string Text(string key, string fallback)
    {
        try
        {
            var doc = Document();
            if (doc is null) return fallback;

            var value = doc.Root?
                .Element("appSettings")?
                .Elements("add")
                .FirstOrDefault(e => (string?)e.Attribute("key") == key)?
                .Attribute("value");

            string? text = (string?)value;
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }
        catch
        {
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

    /// <summary>I read a yes/no setting. "true", "false", "yes", "no", "1"
    /// and "0" all work, in any case; anything else means the fallback.</summary>
    public static bool Flag(string key, bool fallback)
    {
        string raw = Text(key, string.Empty);
        if (raw.Length == 0) return fallback;

        if (bool.TryParse(raw, out bool value)) return value;

        return raw switch
        {
            "1" => true,
            "0" => false,
            _ when raw.Equals("yes", StringComparison.OrdinalIgnoreCase) => true,
            _ when raw.Equals("no", StringComparison.OrdinalIgnoreCase) => false,
            _ => fallback
        };
    }

    /// <summary>
    /// True when App.config asks for the real database.
    ///
    /// I compare without case so "MySQL", "mysql" and "MySql" all work - I do
    /// not want my group-mates losing an afternoon to a capital letter.
    /// </summary>
    public static bool UseMySql() =>
        Text("Storage", "MySQL").Equals("MySQL", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A named entry from &lt;connectionStrings&gt;. I return an empty string
    /// when it is missing so the caller can fall back to a default instead
    /// of handing a null to the driver.
    /// </summary>
    public static string ConnectionString(string name)
    {
        try
        {
            var doc = Document();
            if (doc is null) return string.Empty;

            var element = doc.Root?
                .Element("connectionStrings")?
                .Elements("add")
                .FirstOrDefault(e => (string?)e.Attribute("name") == name);

            return ((string?)element?.Attribute("connectionString"))?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
