using System;
using System.Configuration;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Configuration
{
    public class AppSettings
    {
        public BarangayProfile Profile { get; private set; }
        public string ConnectionString { get; private set; }

        public static AppSettings Load()
        {
            // Sample seeding was removed: the production database starts empty
            // and only the fee schedule is seeded (Database/schema.sql).
            var database = ConfigurationManager.ConnectionStrings["BarangayDatabase"];
            if (database == null || string.IsNullOrWhiteSpace(database.ConnectionString))
                throw new ConfigurationErrorsException("App.config: add the BarangayDatabase connection string.");
            try { new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(database.ConnectionString); }
            catch (ArgumentException error) { throw new ConfigurationErrorsException("App.config: the database connection string is invalid.", error); }
            return new AppSettings
            {
                Profile = new BarangayProfile(ReadRequired("BarangayName"), ReadRequired("CityName"),
                    ReadRequired("ProvinceName"), ReadRequired("PunongBarangay")),
                ConnectionString = database.ConnectionString
            };
        }

        private static string ReadRequired(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new ConfigurationErrorsException("App.config: " + key + " must not be blank.");
            return value.Trim();
        }
    }
}
