// ---------------------------------------------------------------------------
//  DatabaseInitializer.cs - builds the database on the first start.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Database
{
    /// <summary>
    /// Everything that has to be true about the database before the login
    /// screen appears.
    ///
    /// On a brand-new computer the clerk double-clicks the program and this
    /// runs: it creates the database if the server does not have it, creates
    /// the tables, installs the stored procedures, creates the first
    /// administrator account, and (if asked) loads the sample residents so the
    /// screens are not empty. Nothing has to be pasted into phpMyAdmin or
    /// SSMS, which was the whole point - the barangay has one computer and
    /// nobody there wants to learn a database tool.
    ///
    /// It also repairs an older database: if the barangay already has data
    /// from the version before this revamp, the missing columns are added and
    /// the rows are kept. I never drop a table here.
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly DBContext _context;

        public DatabaseInitializer(DBContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            _context = context;
        }

        /// <summary>What it did, in one sentence, for the startup log and the
        /// status bar.</summary>
        public string Run()
        {
            if (_context.Provider == null || !_context.Provider.SupportsDatabaseCreation)
                return _context.Description + " - nothing to set up.";

            List<string> notes = new List<string>();

            // 1. The database itself.
            string createMessage;
            if (!_context.CreateDatabase(out createMessage) && !string.IsNullOrWhiteSpace(createMessage))
                throw new RepositoryException("creating the database", createMessage);
            if (!string.IsNullOrWhiteSpace(createMessage)) notes.Add(createMessage);

            // 2. The tables.
            if (!TableExists("residents"))
            {
                RunScript(ScriptFor("01-schema.sql"));
                notes.Add("created the tables");
            }
            else
            {
                string upgrades = UpgradeSchema();
                if (!string.IsNullOrWhiteSpace(upgrades)) notes.Add(upgrades);
            }

            // 3. The stored procedures.
            if (!ProcedureExists(StoredProcedures.PerDayTransactions))
            {
                try
                {
                    RunScript(ScriptFor("02-procedures.sql"));
                    notes.Add("installed the stored procedures");
                }
                catch (RepositoryException error)
                {
                    // Not fatal: every report has a plain-SQL fallback. I log it
                    // and let the program start, because a missing procedure
                    // must never stop the barangay from working.
                    AppLog.Warn("The stored procedures could not be installed: " + error.Message);
                    notes.Add("could not install the stored procedures (the built-in queries are used instead)");
                }
            }

            // 4. The first accounts, and the sample data.
            if (CountRows("user_accounts") == 0)
            {
                SeedAccounts();
                notes.Add("created the first accounts");
            }

            if (AppConfig.SeedSampleData && CountRows("residents") == 0)
            {
                SampleData.Seed(_context, new SystemClock());
                notes.Add("loaded the sample data");
            }

            string summary = notes.Count == 0
                ? "The database was already ready."
                : string.Join(", ", notes.ToArray());

            AppLog.Info("Database check: " + summary);
            return summary;
        }

        // ==================================================================
        //  Accounts
        // ==================================================================

        /// <summary>
        /// The first sign-in anyone gets.
        ///
        /// There is no "register" screen in this system - the barangay asked
        /// for it to be removed, and it is the right call anyway: anyone who
        /// could create their own account could read the residents' records.
        /// So the program creates an administrator from App.config, and that
        /// person creates the real staff accounts from the Users screen.
        ///
        /// Both accounts are flagged "change the password at first sign-in",
        /// which is the only safe way to hand over a password that is written
        /// in a file.
        /// </summary>
        private void SeedAccounts()
        {
            PasswordHasher hasher = new PasswordHasher();

            UserAccount administrator = new UserAccount();
            administrator.Username = AppConfig.InitialAdminUsername;
            administrator.FullName = AppConfig.InitialAdminFullName;
            administrator.Role = UserRole.Administrator;
            administrator.Position = "System administrator";
            administrator.CreatedBy = "system";
            administrator.MustChangePassword = AppConfig.ForcePasswordChangeOnFirstLogin;
            administrator.IsActive = true;
            ApplyPassword(administrator, AppConfig.InitialAdminPassword, hasher);

            using (DBCommand insert = _context.Helper.BuildOn(null,
                "INSERT INTO user_accounts (username, full_name, role, position, password_hash, password_salt, " +
                "hash_iterations, must_change_password, is_active, failed_attempts, locked_until, last_login_on, " +
                "last_login_machine, created_on, created_by) VALUES (@username, @full_name, @role, @position, " +
                "@hash, @salt, @iterations, @must_change, @active, @failed, @locked, @last_login, @machine, @created, @by)",
                new SqlArguments()
                    .Add("@username", administrator.Username)
                    .Add("@full_name", administrator.FullName)
                    .Add("@role", administrator.Role.ToString())
                    .Add("@position", administrator.Position)
                    .Add("@hash", administrator.PasswordHash)
                    .Add("@salt", administrator.PasswordSalt)
                    .Add("@iterations", administrator.HashIterations)
                    .Add("@must_change", administrator.MustChangePassword)
                    .Add("@active", administrator.IsActive)
                    .Add("@failed", 0)
                    .Add("@locked", DBNull.Value)
                    .Add("@last_login", DBNull.Value)
                    .Add("@machine", string.Empty)
                    .Add("@created", DateTime.Now)
                    .Add("@by", administrator.CreatedBy), CommandType.Text))
            {
                insert.ExecuteNonQuery();
            }

            UserAccount clerk = new UserAccount();
            clerk.Username = AppConfig.InitialClerkUsername;
            clerk.FullName = AppConfig.InitialClerkFullName;
            clerk.Role = UserRole.Clerk;
            clerk.Position = "Barangay clerk";
            clerk.CreatedBy = "system";
            clerk.MustChangePassword = AppConfig.ForcePasswordChangeOnFirstLogin;
            clerk.IsActive = true;
            ApplyPassword(clerk, AppConfig.InitialClerkPassword, hasher);

            using (DBCommand insert = _context.Helper.BuildOn(null,
                "INSERT INTO user_accounts (username, full_name, role, position, password_hash, password_salt, " +
                "hash_iterations, must_change_password, is_active, failed_attempts, locked_until, last_login_on, " +
                "last_login_machine, created_on, created_by) VALUES (@username, @full_name, @role, @position, " +
                "@hash, @salt, @iterations, @must_change, @active, @failed, @locked, @last_login, @machine, @created, @by)",
                new SqlArguments()
                    .Add("@username", clerk.Username)
                    .Add("@full_name", clerk.FullName)
                    .Add("@role", clerk.Role.ToString())
                    .Add("@position", clerk.Position)
                    .Add("@hash", clerk.PasswordHash)
                    .Add("@salt", clerk.PasswordSalt)
                    .Add("@iterations", clerk.HashIterations)
                    .Add("@must_change", clerk.MustChangePassword)
                    .Add("@active", clerk.IsActive)
                    .Add("@failed", 0)
                    .Add("@locked", DBNull.Value)
                    .Add("@last_login", DBNull.Value)
                    .Add("@machine", string.Empty)
                    .Add("@created", DateTime.Now)
                    .Add("@by", clerk.CreatedBy), CommandType.Text))
            {
                insert.ExecuteNonQuery();
            }
        }

        private void ApplyPassword(UserAccount user, string plainPassword, PasswordHasher hasher)
        {
            string salt;
            user.PasswordHash = hasher.Hash(plainPassword, out salt);
            user.PasswordSalt = salt;
            user.HashIterations = hasher.Iterations;
        }

        // ==================================================================
        //  The schema
        // ==================================================================

        private bool TableExists(string tableName)
        {
            object count = _context.Helper.ExecuteScalar("checking for the " + tableName + " table",
                _context.Provider.Key == "SqlServer"
                    ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name"
                    : "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name",
                new SqlArguments().Add("@name", tableName));

            return count != null && Convert.ToInt32(count) > 0;
        }

        private bool ProcedureExists(string procedureName)
        {
            object count = _context.Helper.ExecuteScalar("checking for the " + procedureName + " procedure",
                _context.Provider.Key == "SqlServer"
                    ? "SELECT COUNT(*) FROM sys.procedures WHERE name = @name"
                    : "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() AND routine_name = @name AND routine_type = 'PROCEDURE'",
                new SqlArguments().Add("@name", procedureName));

            return count != null && Convert.ToInt32(count) > 0;
        }

        private int CountRows(string tableName)
        {
            object count = _context.Helper.ExecuteScalar("counting the rows in " + tableName,
                "SELECT COUNT(*) FROM " + tableName, null);
            return count == null ? 0 : Convert.ToInt32(count);
        }

        // ==================================================================
        //  Upgrading an older database
        // ==================================================================

        /// <summary>
        /// Adds any column an older database is missing, without touching the
        /// rows that are already there. I never drop a table or a column here:
        /// a column the new code no longer reads is simply left alone.
        /// </summary>
        private string UpgradeSchema()
        {
            List<string> added = new List<string>();

            AddColumnIfMissing(added, "residents", "state_reason",
                "ALTER TABLE residents ADD COLUMN state_reason VARCHAR(255) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "residents", "state_changed_on",
                "ALTER TABLE residents ADD COLUMN state_changed_on DATETIME NULL");
            AddColumnIfMissing(added, "residents", "state_changed_by",
                "ALTER TABLE residents ADD COLUMN state_changed_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "residents", "has_availed_jobseeker",
                "ALTER TABLE residents ADD COLUMN has_availed_jobseeker TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "residents", "is_student_fee_category",
                "ALTER TABLE residents ADD COLUMN is_student_fee_category TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "residents", "is_business_owner",
                "ALTER TABLE residents ADD COLUMN is_business_owner TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "residents", "is_head_of_family",
                "ALTER TABLE residents ADD COLUMN is_head_of_family TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "residents", "residency_status",
                "ALTER TABLE residents ADD COLUMN residency_status VARCHAR(20) NOT NULL DEFAULT 'Newcomer'");
            AddColumnIfMissing(added, "residents", "record_state",
                "ALTER TABLE residents ADD COLUMN record_state VARCHAR(20) NOT NULL DEFAULT 'Active'");
            AddColumnIfMissing(added, "residents", "date_of_residency",
                "ALTER TABLE residents ADD COLUMN date_of_residency DATE NOT NULL DEFAULT '2000-01-01'");
            AddColumnIfMissing(added, "residents", "is_registered_voter",
                "ALTER TABLE residents ADD COLUMN is_registered_voter TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "residents", "classification",
                "ALTER TABLE residents ADD COLUMN classification INT NOT NULL DEFAULT 0");

            AddColumnIfMissing(added, "document_requests", "fee_basis",
                "ALTER TABLE document_requests ADD COLUMN fee_basis VARCHAR(500) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "or_control_number",
                "ALTER TABLE document_requests ADD COLUMN or_control_number VARCHAR(30) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "payment_date",
                "ALTER TABLE document_requests ADD COLUMN payment_date DATETIME NULL");
            AddColumnIfMissing(added, "document_requests", "collected_by",
                "ALTER TABLE document_requests ADD COLUMN collected_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "scope",
                "ALTER TABLE document_requests ADD COLUMN scope VARCHAR(10) NOT NULL DEFAULT 'Local'");
            AddColumnIfMissing(added, "document_requests", "assessed_amount",
                "ALTER TABLE document_requests ADD COLUMN assessed_amount DECIMAL(10,2) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "hours",
                "ALTER TABLE document_requests ADD COLUMN hours DECIMAL(6,2) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "gross_annual_income",
                "ALTER TABLE document_requests ADD COLUMN gross_annual_income DECIMAL(14,2) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "detail",
                "ALTER TABLE document_requests ADD COLUMN detail VARCHAR(255) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "apply_jobseeker_waiver",
                "ALTER TABLE document_requests ADD COLUMN apply_jobseeker_waiver TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "availed_under_jobseeker_act",
                "ALTER TABLE document_requests ADD COLUMN availed_under_jobseeker_act TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "requires_validation",
                "ALTER TABLE document_requests ADD COLUMN requires_validation TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "filed_during_office_window",
                "ALTER TABLE document_requests ADD COLUMN filed_during_office_window TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "business_name",
                "ALTER TABLE document_requests ADD COLUMN business_name VARCHAR(150) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_nature",
                "ALTER TABLE document_requests ADD COLUMN business_nature VARCHAR(150) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_purok",
                "ALTER TABLE document_requests ADD COLUMN business_purok VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_location",
                "ALTER TABLE document_requests ADD COLUMN business_location VARCHAR(150) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_ownership",
                "ALTER TABLE document_requests ADD COLUMN business_ownership VARCHAR(60) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_registration",
                "ALTER TABLE document_requests ADD COLUMN business_registration VARCHAR(50) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_previous_permit",
                "ALTER TABLE document_requests ADD COLUMN business_previous_permit VARCHAR(50) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "business_employees",
                "ALTER TABLE document_requests ADD COLUMN business_employees INT NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "business_is_renewal",
                "ALTER TABLE document_requests ADD COLUMN business_is_renewal TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "document_requests", "date_released",
                "ALTER TABLE document_requests ADD COLUMN date_released DATETIME NULL");
            AddColumnIfMissing(added, "document_requests", "released_by",
                "ALTER TABLE document_requests ADD COLUMN released_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "received_by",
                "ALTER TABLE document_requests ADD COLUMN received_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "rejection_reason",
                "ALTER TABLE document_requests ADD COLUMN rejection_reason VARCHAR(500) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "remarks",
                "ALTER TABLE document_requests ADD COLUMN remarks VARCHAR(500) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "document_requests", "last_status_on",
                "ALTER TABLE document_requests ADD COLUMN last_status_on DATETIME NULL");
            AddColumnIfMissing(added, "document_requests", "last_status_by",
                "ALTER TABLE document_requests ADD COLUMN last_status_by VARCHAR(100) NOT NULL DEFAULT ''");

            AddColumnIfMissing(added, "official_receipts", "series_code",
                "ALTER TABLE official_receipts ADD COLUMN series_code VARCHAR(20) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "control_number",
                "ALTER TABLE official_receipts ADD COLUMN control_number VARCHAR(30) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "request_id",
                "ALTER TABLE official_receipts ADD COLUMN request_id INT NULL");
            AddColumnIfMissing(added, "official_receipts", "collected_by",
                "ALTER TABLE official_receipts ADD COLUMN collected_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "remarks",
                "ALTER TABLE official_receipts ADD COLUMN remarks VARCHAR(255) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "is_void",
                "ALTER TABLE official_receipts ADD COLUMN is_void TINYINT(1) NOT NULL DEFAULT 0");
            AddColumnIfMissing(added, "official_receipts", "void_reason",
                "ALTER TABLE official_receipts ADD COLUMN void_reason VARCHAR(255) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "voided_on",
                "ALTER TABLE official_receipts ADD COLUMN voided_on DATETIME NULL");
            AddColumnIfMissing(added, "official_receipts", "voided_by",
                "ALTER TABLE official_receipts ADD COLUMN voided_by VARCHAR(100) NOT NULL DEFAULT ''");
            AddColumnIfMissing(added, "official_receipts", "created_on",
                "ALTER TABLE official_receipts ADD COLUMN created_on DATETIME NOT NULL DEFAULT '2000-01-01'");

            if (added.Count > 0)
                AppLog.Info("Added the missing columns: " + string.Join(", ", added.ToArray()) + ".");

            return added.Count == 0 ? string.Empty : "added " + added.Count + " missing column(s)";
        }

        private void AddColumnIfMissing(List<string> added, string tableName, string columnName, string alterSql)
        {
            object count = _context.Helper.ExecuteScalar("checking for the " + columnName + " column",
                _context.Provider.Key == "SqlServer"
                    ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column"
                    : "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column",
                new SqlArguments().Add("@table", tableName).Add("@column", columnName));

            if (count != null && Convert.ToInt32(count) == 0)
            {
                _context.Helper.ExecuteNonQuery("adding the " + columnName + " column", alterSql, null);
                added.Add(tableName + "." + columnName);
            }
        }

        // ==================================================================
        //  Running the embedded scripts
        // ==================================================================

        private string ScriptFor(string fileName)
        {
            string resourceName = "BarangayDocumentSystem.Database.Scripts."
                                + _context.Provider.Key + "." + fileName;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new RepositoryException("reading the " + fileName + " script",
                        "The " + fileName + " script for " + _context.Provider.Key +
                        " is not embedded in this program. The project file should list it as an EmbeddedResource.");

                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private void RunScript(string script)
        {
            foreach (string statement in SplitStatements(script))
            {
                if (string.IsNullOrWhiteSpace(statement)) continue;

                try
                {
                    _context.Helper.ExecuteNonQuery("running a script statement", statement, null);
                }
                catch (RepositoryException error)
                {
                    AppLog.Warn("A script statement failed: " + error.Message + " Statement: " +
                        (statement.Length > 120 ? statement.Substring(0, 120) + "..." : statement));
                    throw;
                }
            }
        }

        private static List<string> SplitStatements(string script)
        {
            List<string> statements = new List<string>();
            string current = string.Empty;
            string delimiter = ";";

            foreach (string line in script.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("--") || string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
                {
                    delimiter = trimmed.Substring("DELIMITER".Length).Trim();
                    continue;
                }

                current += line + Environment.NewLine;

                if (trimmed.EndsWith(delimiter, StringComparison.Ordinal))
                {
                    string statement = current.Trim();
                    if (statement.EndsWith(delimiter, StringComparison.Ordinal))
                        statement = statement.Substring(0, statement.Length - delimiter.Length).Trim();

                    if (!string.IsNullOrWhiteSpace(statement)) statements.Add(statement);
                    current = string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(current.Trim())) statements.Add(current.Trim());
            return statements;
        }
    }
}
