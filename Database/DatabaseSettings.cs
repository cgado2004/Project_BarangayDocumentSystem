using System;
using System.Configuration;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// Where the MySQL server is and how to log in.
///
/// Read from <c>App.config</c> (<c>connectionStrings</c> and
/// <c>appSettings</c>) — the standard place a .NET Framework WinForms app
/// keeps this, via <see cref="ConfigurationManager"/>. Changing the server
/// or password only means editing <c>BarangayDocumentSystem.exe.config</c>
/// next to the .exe; it never needs a rebuild.
///
/// The environment variable <see cref="EnvironmentVariable"/>, if set, wins
/// over the file — handy for keeping a real password out of source control.
/// </summary>
public sealed class DatabaseSettings
{
    /// <summary>A stock local MySQL / XAMPP install: user root, blank password.</summary>
    public const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;";

    /// <summary>Name of the &lt;connectionStrings&gt; entry in App.config.
    /// Unified with AppSettings, which reads the same entry: Jonathan's
    /// configuration reader and this class share one connection.</summary>
    public const string ConnectionStringName = "BarangayDatabase";

    public const string EnvironmentVariable = "BARANGAY_DB_CONNECTION";

    public string ConnectionString { get; }

    private DatabaseSettings(string connectionString)
    {
        ConnectionString = connectionString;
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

        return new DatabaseSettings(connectionString);
    }
}
