// =====================================================================
//  PART:    Database - where the MySQL server is and how to log in
//  ORIGIN:  Fdraft - Frent Dhieniel Raborar
//  EDITS:   Clint Wood Gado - reads App.config through my AppSettings instead
//           of System.Configuration (one reader of the file, one less
//           assembly reference); otherwise Frent's order of precedence
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// The MySQL connection string and the seed switch, resolved in this order:
///
///   1. the environment variable BARANGAY_DB_CONNECTION, if set;
///   2. the BarangayDb entry in App.config;
///   3. a stock XAMPP install - root, blank password, database barangay_db.
///
/// Frent's rule, and a good one: the environment variable wins so a real
/// password never has to be typed into a file that lives in Git. The
/// repository is public; the password in App.config stays blank.
/// </summary>
public sealed class DatabaseSettings
{
    /// <summary>A stock local MySQL / XAMPP install: user root, blank
    /// password. CharSet=utf8mb4 so names like Peña store correctly.</summary>
    public const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;";

    /// <summary>Name of the &lt;connectionStrings&gt; entry in App.config.</summary>
    public const string ConnectionStringName = "BarangayDb";

    /// <summary>Name of the &lt;appSettings&gt; key that turns demo data on or off.</summary>
    public const string SeedSettingName = "SeedSampleData";

    public const string EnvironmentVariable = "BARANGAY_DB_CONNECTION";

    public string ConnectionString { get; }

    /// <summary>Fill an EMPTY database with my sample residents on first
    /// run. It never touches a database that already has residents, so
    /// switching it off later is not necessary - but it can be, for a
    /// barangay that wants to start from a clean slate.</summary>
    public bool SeedSampleData { get; }

    private DatabaseSettings(string connectionString, bool seedSampleData)
    {
        ConnectionString = connectionString;
        SeedSampleData = seedSampleData;
    }

    public static DatabaseSettings Load()
    {
        string connectionString = DefaultConnectionString;

        string fromConfig = AppSettings.ConnectionString(ConnectionStringName);
        if (fromConfig.Length > 0)
            connectionString = fromConfig;

        string? fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            connectionString = fromEnvironment!.Trim();

        return new DatabaseSettings(connectionString, AppSettings.Flag(SeedSettingName, true));
    }
}
