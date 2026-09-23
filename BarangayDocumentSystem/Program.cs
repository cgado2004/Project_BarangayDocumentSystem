using System.Globalization;
using System.Xml.Linq;
using BarangayDocumentSystem.DBContext;
using BarangayDocumentSystem.Helper;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;

namespace BarangayDocumentSystem;

/// <summary>
/// Where my program starts, and the only place that decides which concrete
/// classes get used.
///
/// Everything below this point is handed what it needs through its
/// constructor, so no screen ever creates its own repository. That is what
/// lets me swap the storage without touching a single form.
///
/// ApplicationConfiguration.Initialize() carries the PerMonitorV2 high-DPI
/// mode from the .csproj (core fix 2); AppTheme.Resolve() picks the best of
/// the Inter / SF Pro / Roboto stack this machine actually has (core fix 3);
/// then the composition happens: profile, fees, store, shell.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // I work out which fonts this machine actually has before I create a
        // single control, so every form is built with the right family from
        // the start rather than being restyled afterwards.
        AppTheme.Resolve();

        // I read App.config first, so the barangay details and the fees on
        // every printed document come from the file rather than from numbers
        // I hard-coded months ago.
        BarangayProfile.Current = new BarangayProfile
        {
            BarangayName   = AppSettings.Text("Barangay.Name", "Barangay Magugpo Poblacion"),
            CityName       = AppSettings.Text("Barangay.City", "City of Tagum"),
            ProvinceName   = AppSettings.Text("Barangay.Province", "Davao del Norte"),
            PunongBarangay = AppSettings.Text("Barangay.PunongBarangay", "HON. EUGENIA SOLIS HINGPIT, MD"),
            OfficeHours    = AppSettings.Text("Barangay.OfficeHours", "Monday to Friday, 8:00 AM - 5:00 PM")
        };

        var fees = new FeeSchedule(
            AppSettings.Money("Fee.Clearance.Local", 100m),
            AppSettings.Money("Fee.Clearance.Abroad", 200m),
            AppSettings.Money("Fee.Certification", 100m),
            AppSettings.Money("Fee.BusinessClearance.Standard", 200m),
            AppSettings.Money("Fee.LuponFiling", 150m),
            AppSettings.Money("Fee.Facility.Hourly", 200m),
            AppSettings.Money("Fee.CommunityTax.Base", 5m),
            AppSettings.Money("Fee.CommunityTax.PerThousand", 1m),
            AppSettings.Money("Fee.CommunityTax.Cap", 5000m),
            AppSettings.Count("Rule.JobseekerResidencyMonths", 6),
            AppSettings.Count("Rule.RA11032.SimpleWorkingDays", 3));

        // This is the one decision I make that moves the whole system onto a
        // real database. Every screen only ever sees IBarangayRepository, so
        // nothing else in my program has to change.
        IBarangayRepository repository = CreateRepository(fees);

        Application.Run(new MainShell(repository, fees));
    }

    /// <summary>
    /// I pick the store named in App.config.
    ///
    /// If MySQL is asked for but cannot be reached, I do NOT let the program
    /// die on a raw driver exception. I explain what went wrong and fall back
    /// to the in-memory data, because a group-mate who has not set up XAMPP
    /// yet should still be able to open the app and see it work.
    /// </summary>
    private static IBarangayRepository CreateRepository(FeeSchedule fees)
    {
        if (!AppSettings.UseMySql())
            return new InMemoryBarangayRepository(fees);

        string connection = AppSettings.ConnectionString();

        if (string.IsNullOrWhiteSpace(connection))
        {
            Dialog.Info(null,
                "App.config asks for MySQL storage but the BarangayDb " +
                "connection string is missing.\n\n" +
                "I am starting with the built-in sample data instead.");
            return new InMemoryBarangayRepository(fees);
        }

        // NOTE FOR MY GROUP-MATES
        // The MySQL repository class is not in this version yet. The database
        // scripts in db/ are complete and ready to run, so when the repository
        // is added the only change needed here is the line below:
        //
        //     return new MySqlBarangayRepository(connection, fees);
        //
        // Until then, asking for MySQL gives the sample data and this warning,
        // which is honest rather than pretending the database is wired up.
        Dialog.Info(null,
            "MySQL storage is selected in App.config, but the MySQL " +
            "repository is not included in this build yet.\n\n" +
            "The scripts in the db folder are ready to run - see " +
            "docs/05-database-guide.md.\n\n" +
            "I am starting with the built-in sample data for now.");

        return new InMemoryBarangayRepository(fees);
    }
}

/// <summary>
/// I read App.config through this one class.
///
/// v3.1 reads the file with System.Xml.Linq instead of the
/// System.Configuration.ConfigurationManager package, which keeps the whole
/// solution building with no NuGet restore and no network - and, because I
/// control the parse, a malformed file degrades to the built-in defaults
/// instead of throwing from somewhere deep in a form.
///
/// Every read goes through a helper that falls back to a sensible default
/// and never returns null. If my group-mate deletes a line from App.config
/// by accident, the app still starts and still works - it just uses the
/// built-in value instead of crashing on them.
/// </summary>
internal static class AppSettings
{
    private static XDocument? _doc;
    private static bool _loaded;

    /// <summary>
    /// The configuration, loaded once. MSBuild names the copied file after
    /// the assembly, so I probe the names the SDK produces as well as the
    /// plain one, and I take the first that exists and parses.
    /// </summary>
    private static XDocument? Document()
    {
        if (_loaded) return _doc;
        _loaded = true;

        string[] candidates =
        {
            "BarangayDocumentSystem.dll.config",
            "BarangayDocumentSystem.exe.config",
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
            var doc = Document();
            if (doc is null) return string.Empty;

            var element = doc.Root?
                .Element("connectionStrings")?
                .Elements("add")
                .FirstOrDefault(e => (string?)e.Attribute("name") == "BarangayDb");

            return (string?)element?.Attribute("connectionString") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
