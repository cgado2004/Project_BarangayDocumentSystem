using System;
using System.Configuration;

namespace BarangayDocumentSystem.Database;







public sealed class DatabaseSettings
{

    public const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;";


    public const string ConnectionStringName = "BarangayDb";


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
