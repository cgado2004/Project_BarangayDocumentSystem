// ---------------------------------------------------------------------------
//  AppConfig.cs - the one place that reads App.config.
//  The comments here are in my own voice. If you are new to this project,
//  read this file first: everything you might want to change about how the
//  system behaves is a line in App.config, and this file is where those
//  lines become values the rest of the code can use.
// ---------------------------------------------------------------------------
using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace BarangayDocumentSystem.Config
{
    /// <summary>
    /// My settings reader.
    ///
    /// I did not want fee amounts, cut-off hours, lock-out counts, report
    /// folders or connection strings typed inside the code, because then
    /// changing them means rebuilding the program. Everything lives in
    /// App.config, and this class is the only reader of it, so there is one
    /// place to look when a setting seems to be ignored.
    ///
    /// Every reader has a fallback value. That is deliberate: if a teammate
    /// copies App.config to a machine and forgets one line, the program must
    /// still start and behave the way it did before the key existed, instead
    /// of dying at the login screen.
    /// </summary>
    public static class AppConfig
    {
        private static bool _loaded;

        // ==================================================================
        //  General
        // ==================================================================

        /// <summary>Which storage the app talks to. MySQL is the real one for
        /// the barangay; Memory is the demo store that saves nothing.</summary>
        public static string StorageProvider { get; private set; }

        /// <summary>True when the program should load sample residents and
        /// requests into an empty database so the screens are not blank.</summary>
        public static bool SeedSampleData { get; private set; }

        /// <summary>Where the program writes its own log file if something
        /// goes wrong. I keep it next to the .exe by default.</summary>
        public static string LogFolder { get; private set; }

        // ==================================================================
        //  Branding (the letterhead on every printed paper)
        // ==================================================================

        public static string BarangayName { get; private set; }
        public static string CityName { get; private set; }
        public static string ProvinceName { get; private set; }
        public static string PunongBarangay { get; private set; }
        public static string OfficeHours { get; private set; }

        /// <summary>The seal shown on the login card, the sidebar and every
        /// printed document. I am not changing this file, only pointing at it.</summary>
        public static string LogoFile { get; private set; }

        // ==================================================================
        //  Fees
        // ==================================================================

        public static decimal FeeBarangayClearanceLocal { get; private set; }
        public static decimal FeeBarangayClearanceAbroad { get; private set; }
        public static decimal FeeCertification { get; private set; }
        public static decimal FeeBusinessClearance { get; private set; }
        public static decimal FeeLuponFiling { get; private set; }
        public static decimal FeeFacilityPerHour { get; private set; }

        /// <summary>Student is a FEE CATEGORY, not a classification.
        /// I removed "Student" from the resident classifications as asked, but
        /// students still get a discount on the documents listed below.</summary>
        public static bool StudentDiscountEnabled { get; private set; }
        public static int StudentDiscountPercent { get; private set; }
        public static string StudentDiscountDocuments { get; private set; }

        public static decimal CommunityTaxBase { get; private set; }
        public static decimal CommunityTaxPerThousand { get; private set; }
        public static decimal CommunityTaxCap { get; private set; }

        // ==================================================================
        //  Rules
        // ==================================================================

        /// <summary>The 8:00 AM to 4:00 PM window we agreed on: a request filed
        /// inside it needs no validation and can go straight to "Cleared",
        /// anything filed after 4:01 PM waits as "Pending" for the next day.</summary>
        public static TimeSpan OfficeWindowStart { get; private set; }
        public static TimeSpan OfficeWindowEnd { get; private set; }

        /// <summary>Months of residency before the RA 11261 jobseeker benefit
        /// may be used, and how long a resident is counted as a newcomer.</summary>
        public static int JobseekerResidencyMonths { get; private set; }
        public static int NewcomerMonths { get; private set; }
        public static int PermanentResidencyYears { get; private set; }

        public static int Ra11032WorkingDays { get; private set; }

        // ==================================================================
        //  Security
        // ==================================================================

        /// <summary>Wrong passwords allowed before the account locks.</summary>
        public static int MaxFailedLogins { get; private set; }

        /// <summary>How long the lock lasts, in minutes.</summary>
        public static int LockoutMinutes { get; private set; }

        /// <summary>How long an idle session is allowed to sit before the
        /// app asks for the password again, in minutes. 0 turns it off.</summary>
        public static int SessionTimeoutMinutes { get; private set; }

        /// <summary>Minimum characters I accept for a new password.</summary>
        public static int MinimumPasswordLength { get; private set; }

        /// <summary>Force a new user to change the password an admin gave
        /// them the first time they sign in.</summary>
        public static bool ForcePasswordChangeOnFirstLogin { get; private set; }

        /// <summary>True when the connection string in App.config was sealed
        /// with Windows DPAPI (see ConnectionStringProtector).</summary>
        public static bool ProtectConnectionString { get; private set; }

        /// <summary>
        /// The first administrator. The barangay asked me to take self
        /// registration out, so there has to be one account the program can
        /// create by itself on a brand-new database - otherwise nobody could
        /// ever sign in. The password here is only a starting password: the
        /// account is flagged to change it on the first sign-in.
        /// </summary>
        public static string InitialAdminUsername { get; private set; }
        public static string InitialAdminFullName { get; private set; }
        public static string InitialAdminPassword { get; private set; }

        /// <summary>The first clerk account, also from the settings file. An
        /// empty user name here simply means "do not create a clerk yet" - the
        /// administrator can add staff from the Users screen.</summary>
        public static string InitialClerkUsername { get; private set; }
        public static string InitialClerkFullName { get; private set; }
        public static string InitialClerkPassword { get; private set; }

        // ==================================================================
        //  Reports
        // ==================================================================

        /// <summary>The folder holding the Crystal Reports .rpt files. I keep
        /// it configurable because the barangay may put the templates on a
        /// shared drive.</summary>
        public static string CrystalReportsFolder { get; private set; }

        /// <summary>Set false to force the built-in report viewer even when
        /// Crystal Reports is installed on the machine.</summary>
        public static bool UseCrystalReports { get; private set; }

        /// <summary>Shown on the bottom of every printed report.</summary>
        public static string ReportFooter { get; private set; }

        // ==================================================================
        //  Loading
        // ==================================================================

        /// <summary>
        /// Reads App.config once. I call this first thing in Program.cs so
        /// every screen after it sees real values.
        /// </summary>
        public static void Load()
        {
            if (_loaded) return;

            StorageProvider = ReadString("Storage", "MySQL");
            SeedSampleData = ReadBool("SeedSampleData", true);
            LogFolder = ReadString("LogFolder", "logs");

            BarangayName = ReadString("Barangay.Name", "Barangay Magugpo Poblacion");
            CityName = ReadString("Barangay.City", "City of Tagum");
            ProvinceName = ReadString("Barangay.Province", "Davao del Norte");
            PunongBarangay = ReadString("Barangay.PunongBarangay", "HON. EUGENIA SOLIS HINGPIT, MD");
            OfficeHours = ReadString("Barangay.OfficeHours", "Monday to Friday, 8:00 AM - 5:00 PM");
            LogoFile = ReadString("Barangay.Logo", "Assets\\barangay-logo.png");

            FeeBarangayClearanceLocal = ReadMoney("Fee.Clearance.Local", 100m);
            FeeBarangayClearanceAbroad = ReadMoney("Fee.Clearance.Abroad", 200m);
            FeeCertification = ReadMoney("Fee.Certification", 100m);
            FeeBusinessClearance = ReadMoney("Fee.BusinessClearance.Standard", 200m);
            FeeLuponFiling = ReadMoney("Fee.LuponFiling", 150m);
            FeeFacilityPerHour = ReadMoney("Fee.Facility.Hourly", 200m);

            StudentDiscountEnabled = ReadBool("Fee.Student.Enabled", true);
            StudentDiscountPercent = ReadInt("Fee.Student.DiscountPercent", 50);
            StudentDiscountDocuments = ReadString("Fee.Student.Documents",
                "BarangayClearance,CertificateOfResidency,CertificateOfGoodMoralCharacter,EmploymentCertification");

            CommunityTaxBase = ReadMoney("Fee.CommunityTax.Base", 5m);
            CommunityTaxPerThousand = ReadMoney("Fee.CommunityTax.PerThousand", 1m);
            CommunityTaxCap = ReadMoney("Fee.CommunityTax.Cap", 5000m);

            OfficeWindowStart = ReadTime("Rule.OfficeWindowStart", new TimeSpan(8, 0, 0));
            OfficeWindowEnd = ReadTime("Rule.OfficeWindowEnd", new TimeSpan(16, 0, 0));
            JobseekerResidencyMonths = ReadInt("Rule.JobseekerResidencyMonths", 6);
            NewcomerMonths = ReadInt("Rule.Residency.NewcomerMonths", 6);
            PermanentResidencyYears = ReadInt("Rule.Residency.PermanentYears", 5);
            Ra11032WorkingDays = ReadInt("Rule.RA11032.SimpleWorkingDays", 3);

            MaxFailedLogins = ReadInt("Security.MaxFailedLogins", 5);
            LockoutMinutes = ReadInt("Security.LockoutMinutes", 15);
            SessionTimeoutMinutes = ReadInt("Security.SessionTimeoutMinutes", 30);
            MinimumPasswordLength = ReadInt("Security.MinimumPasswordLength", 8);
            ForcePasswordChangeOnFirstLogin = ReadBool("Security.ForcePasswordChangeOnFirstLogin", true);
            ProtectConnectionString = ReadBool("Security.ProtectConnectionString", false);

            InitialAdminUsername = ReadString("Security.InitialAdmin.Username", "admin");
            InitialAdminFullName = ReadString("Security.InitialAdmin.FullName", "Barangay Administrator");
            InitialAdminPassword = ReadString("Security.InitialAdmin.Password", "Barangay@2026");
            InitialClerkUsername = ReadString("Security.InitialClerk.Username", "clerk");
            InitialClerkFullName = ReadString("Security.InitialClerk.FullName", "Barangay Clerk");
            InitialClerkPassword = ReadString("Security.InitialClerk.Password", "Clerk@2026");

            CrystalReportsFolder = ReadString("Reports.CrystalFolder", "Reports\\CrystalReports");
            UseCrystalReports = ReadBool("Reports.UseCrystalReports", true);
            ReportFooter = ReadString("Reports.Footer", "Generated by the Barangay Document System");

            _loaded = true;
        }

        /// <summary>Forgets the cached values. Only my tests use this, so a
        /// test can change a setting and see the effect immediately.</summary>
        public static void Reset() { _loaded = false; }

        // ==================================================================
        //  Connection strings
        // ==================================================================

        /// <summary>
        /// The connection string for the chosen provider.
        ///
        /// Three places are checked, in this order, and the first one that
        /// has something wins:
        ///   1. the environment variable (BARANGAY_DB_CONNECTION or
        ///      BARANGAY_DB_CONNECTION_SQLSERVER) - this is how a real
        ///      deployment keeps the password out of a public repository;
        ///   2. the DPAPI-sealed value, when Security.ProtectConnectionString
        ///      is on;
        ///   3. the plain entry in App.config.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                string name = IsSqlServer ? "BarangaySqlServer" : "BarangayDb";

                string fromEnvironment = Environment.GetEnvironmentVariable(
                    IsSqlServer ? "BARANGAY_DB_CONNECTION_SQLSERVER" : "BARANGAY_DB_CONNECTION");
                if (!string.IsNullOrWhiteSpace(fromEnvironment))
                    return fromEnvironment;

                string raw = ReadConnectionString(name);

                if (ProtectConnectionString && !string.IsNullOrWhiteSpace(raw))
                {
                    Security.ConnectionStringProtector protector = new Security.ConnectionStringProtector();
                    if (protector.IsProtected(raw)) return protector.Unprotect(raw);
                }

                return raw;
            }
        }

        /// <summary>True when I am talking to SQL Server instead of MySQL.</summary>
        public static bool IsSqlServer
        {
            get { return string.Equals(StorageProvider, "SqlServer", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>True when the app is running on the demo store that
        /// saves nothing at all.</summary>
        public static bool IsMemory
        {
            get { return string.Equals(StorageProvider, "Memory", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>The name of the database, so the initializer can create it
        /// and the status bar can show it.</summary>
        public static string DatabaseName
        {
            get
            {
                foreach (string piece in (ConnectionString ?? string.Empty).Split(';'))
                {
                    string[] pair = piece.Split('=');
                    if (pair.Length != 2) continue;
                    string key = pair[0].Trim();
                    if (string.Equals(key, "Database", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(key, "Initial Catalog", StringComparison.OrdinalIgnoreCase))
                        return pair[1].Trim();
                }
                return string.Empty;
            }
        }

        // ==================================================================
        //  Small readers, so a wrong value never crashes the program
        // ==================================================================

        private static string ReadString(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int ReadInt(string key, int fallback)
        {
            int parsed;
            string value = ConfigurationManager.AppSettings[key];
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed : fallback;
        }

        private static decimal ReadMoney(string key, decimal fallback)
        {
            decimal parsed;
            string value = ConfigurationManager.AppSettings[key];
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed)
                ? parsed : fallback;
        }

        private static bool ReadBool(string key, bool fallback)
        {
            bool parsed;
            string value = ConfigurationManager.AppSettings[key];
            return bool.TryParse(value, out parsed) ? parsed : fallback;
        }

        /// <summary>Times are written as "08:00" in App.config because that is
        /// how the barangay staff read a clock.</summary>
        private static TimeSpan ReadTime(string key, TimeSpan fallback)
        {
            TimeSpan parsed;
            string value = ConfigurationManager.AppSettings[key];
            return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static string ReadConnectionString(string name)
        {
            ConnectionStringSettings entry = ConfigurationManager.ConnectionStrings[name];
            return entry == null ? string.Empty : entry.ConnectionString;
        }

        /// <summary>The folder the .exe is running from - used for the log
        /// folder and for the packaged report templates.</summary>
        public static string ApplicationFolder
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>
        /// The settings file this program is actually reading - the renamed
        /// copy of App.config that sits next to the program once it is built.
        ///
        /// I show it on the About screen because the first question anybody
        /// asks about a barangay system is "where do I change the fees?", and
        /// the answer should be visible from inside the program rather than in
        /// an instruction sheet that went missing.
        /// </summary>
        public static string SettingsFile
        {
            get
            {
                try
                {
                    string path = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                    return string.IsNullOrEmpty(path) ? "App.config" : path;
                }
                catch (Exception)
                {
                    return "App.config";
                }
            }
        }

        /// <summary>Resolves the report template folder to a full path, so a
        /// relative folder in App.config still works wherever the program is
        /// installed (a USB stick, Program Files, or a desktop folder).</summary>
        public static string CrystalReportsPath
        {
            get
            {
                if (Path.IsPathRooted(CrystalReportsFolder)) return CrystalReportsFolder;
                return Path.Combine(ApplicationFolder, CrystalReportsFolder);
            }
        }
    }
}
