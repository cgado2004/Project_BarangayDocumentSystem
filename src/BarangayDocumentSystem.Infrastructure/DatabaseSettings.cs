using System;
using System.Configuration;

namespace BarangayDocumentSystem.Database;


/// Where the MySQL server is and how to log in.

/// The environment variable <see cref="EnvironmentVariable"/>, if set, wins
/// over the file — handy for keeping a real password out of source control.

public sealed class DatabaseSettings
{
    /// <summary>A stock local MySQL / XAMPP install: user root, blank password.</summary>
    public const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;";

    /// <summary>Name of the &lt;connectionStrings&gt; entry in App.config.</summary>
    public const string ConnectionStringName = "BarangayDb";

    /// <summary>Name of the &lt;appSettings&gt; key that turns demo data on or off.</summary>
    public const string SeedSettingName = "SeedSampleData";

    public const string EnvironmentVariable = "BARANGAY_DB_CONNECTION";

    public string ConnectionString { get; }
    public bool SeedSampleData { get; }

    private DatabaseSettings(string connectionString, bool seedSampleData)
    {
        ConnectionString = connectionString;
        SeedSampleData = seedSampleData;
    }

    public static DatabaseSettings Load()
    {
        string connectionString = DefaultConnectionString;

        var entry = ConfigurationManager.ConnectionStrings[ConnectionStringName];
        if (entry != null && !string.IsNullOrWhiteSpace(entry.ConnectionString))
            connectionString = entry.ConnectionString;

        string fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            connectionString = fromEnvironment;

        bool seed = true;
        string seedSetting = ConfigurationManager.AppSettings[SeedSettingName];
        if (!string.IsNullOrWhiteSpace(seedSetting) && bool.TryParse(seedSetting, out bool parsed))
            seed = parsed;

        return new DatabaseSettings(connectionString, seed);
    }
}
