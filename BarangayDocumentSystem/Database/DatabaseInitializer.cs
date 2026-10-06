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
                return _context.Describe() + " - nothing to set up.";

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

            using (SqlWork work = new SqlWork(_context))
            {
                work.InsertUser(administrator);

                if (!string.IsNullOrWhiteSpace(AppConfig.InitialClerkUsername))
                {
                    UserAccount clerk = new UserAccount();
                    clerk.Username = AppConfig.InitialClerkUsername;
                    clerk.FullName = AppConfig.InitialClerkFullName;
                    clerk.Role = UserRole.Clerk;
                    clerk.Position = "Barangay clerk";
                    clerk.CreatedBy = "system";
                    clerk.MustChangePassword = AppConfig.ForcePasswordChangeOnFirstLogin;
                    clerk.IsActive = true;
                    ApplyPassword(clerk, AppConfig.InitialClerkPassword, hasher);

                    work.InsertUser(clerk);
                }
            }

            AppLog.Info("Created the first accounts: " + administrator.Username);
        }

        private static void ApplyPassword(UserAccount account, string password, PasswordHasher hasher)
        {
            string salt;
            string hash = hasher.Hash(password, out salt);
            account.SetPassword(hash, salt, hasher.Iterations, true);
        }

        // ==================================================================
        //  Scripts
        // ==================================================================

        /// <summary>
        /// Runs a whole .sql file, one statement at a time.
        ///
        /// The two engines need different rules here, and getting this wrong is
        /// how a schema script half-executes, so I will spell it out:
        ///
        ///  * MySQL splits on the semicolon, but a stored procedure body is
        ///    full of semicolons, so my scripts change the delimiter with a
        ///    DELIMITER line and this method follows that.
        ///  * SQL Server splits on its own GO batch separator and ignores the
        ///    semicolons, because a T-SQL procedure body needs them.
        /// </summary>
        private void RunScript(string scriptText)
        {
            if (string.IsNullOrWhiteSpace(scriptText)) return;

            bool sqlServer = _context.Provider.Key == "SqlServer";
            List<string> statements = SplitScript(scriptText, sqlServer);

            int done = 0;
            foreach (string statement in statements)
            {
                string sql = statement.Trim();
                if (sql.Length == 0) continue;

                try
                {
                    _context.Helper.ExecuteNonQuery("running the database script", sql, null);
                    done++;
                }
                catch (RepositoryException error)
                {
                    // Two situations I deliberately tolerate, because both mean
                    // "somebody already did this" rather than "something is
                    // broken": an index or table that already exists.
                    if (error.InnerException != null && AlreadyThere(error.InnerException.Message))
                    {
                        AppLog.Warn("Skipped a statement that was already applied: " + FirstLine(sql));
                        continue;
                    }

                    throw new RepositoryException("running the database script",
                        "I could not build the database. The statement that failed was: " + FirstLine(sql), error);
                }
            }

            AppLog.Info("Ran " + done + " database statements.");
        }

        private static bool AlreadyThere(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            return message.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Duplicate key name", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Duplicate index", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("There is already an object named", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FirstLine(string sql)
        {
            int newline = sql.IndexOf('\n');
            string first = newline < 0 ? sql : sql.Substring(0, newline);
            return first.Length > 120 ? first.Substring(0, 120) + "..." : first;
        }

        /// <summary>
        /// Breaks a script into statements.
        ///
        /// I wrote this by hand instead of using a library because the two
        /// engines disagree and I wanted the rule visible: drop the comment
        /// lines, watch for a DELIMITER change, and treat a GO line on its own
        /// as the end of a batch.
        /// </summary>
        public static List<string> SplitScript(string scriptText, bool sqlServer)
        {
            List<string> statements = new List<string>();
            if (string.IsNullOrWhiteSpace(scriptText)) return statements;

            string delimiter = ";";
            System.Text.StringBuilder current = new System.Text.StringBuilder();

            string[] lines = scriptText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string rawLine in lines)
            {
                string line = rawLine;
                string trimmed = line.Trim();

                if (trimmed.StartsWith("--", StringComparison.Ordinal)) continue;
                if (trimmed.Length == 0)
                {
                    current.Append('\n');
                    continue;
                }

                if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
                {
                    delimiter = trimmed.Substring("DELIMITER ".Length).Trim();
                    continue;
                }

                if (sqlServer && trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    Flush(statements, current);
                    continue;
                }

                current.Append(line).Append('\n');

                if (sqlServer) continue;

                // A statement ends where the delimiter appears at the end of a
                // line. I do not look for it in the middle, because a MySQL
                // procedure body contains ordinary semicolons.
                string withoutComment = trimmed;
                int commentStart = withoutComment.IndexOf("--", StringComparison.Ordinal);
                if (commentStart >= 0) withoutComment = withoutComment.Substring(0, commentStart).TrimEnd();

                if (withoutComment.EndsWith(delimiter, StringComparison.Ordinal))
                {
                    string body = current.ToString();
                    int cut = body.LastIndexOf(delimiter, StringComparison.Ordinal);
                    if (cut >= 0) body = body.Substring(0, cut);

                    statements.Add(body);
                    current.Length = 0;
                }
            }

            Flush(statements, current);
            return statements;
        }

        private static void Flush(List<string> statements, System.Text.StringBuilder current)
        {
            string text = current.ToString().Trim();
            current.Length = 0;
            if (text.Length > 0) statements.Add(text);
        }

        /// <summary>Finds the script inside the .exe. The scripts are embedded
        /// resources rather than loose files on purpose: there is one copy, it
        /// cannot be lost or half-edited on a copy of the program, and the
        /// program always installs the schema that matches its own code.</summary>
        private string ScriptFor(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string engine = _context.Provider.Key == "SqlServer" ? "SqlServer" : "MySql";
            string wanted = "." + engine + "." + fileName;

            foreach (string name in assembly.GetManifestResourceNames())
                if (name.EndsWith(wanted, StringComparison.OrdinalIgnoreCase))
                    return ReadResource(assembly, name);

            throw new RepositoryException("setting up the database",
                "I cannot find the database script " + fileName + " inside the program. "
                + "The build did not include the Database\\Scripts folder.");
        }

        private static string ReadResource(Assembly assembly, string resourceName)
        {
            using (System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return string.Empty;

                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        // ==================================================================
        //  Upgrading a database that already has data
        // ==================================================================

        /// <summary>
        /// Brings an older database up to date without losing a single row.
        ///
        /// The version before this revamp had no reference number, no
        /// dependents table, no receipt booklet control and no active / inactive
        /// state. I add what is missing and backfill it, because a barangay
        /// that has been using the old build for a month cannot be asked to
        /// retype its residents.
        /// </summary>
        private string UpgradeSchema()
        {
            List<string> changed = new List<string>();

            EnsureColumn("residents", "classification", "INT NOT NULL DEFAULT 0", changed);
            EnsureColumn("residents", "is_student_fee_category", BoolColumn(), changed);
            EnsureColumn("residents", "is_business_owner", BoolColumn(), changed);
            EnsureColumn("residents", "is_head_of_family", BoolColumn(), changed);
            EnsureColumn("residents", "residency_status", "VARCHAR(20) NOT NULL DEFAULT 'Newcomer'", changed);
            EnsureColumn("residents", "record_state", "VARCHAR(20) NOT NULL DEFAULT 'Active'", changed);
            EnsureColumn("residents", "state_reason", "VARCHAR(255) NOT NULL DEFAULT ''", changed);
            EnsureColumn("residents", "state_changed_on", DateColumn(true), changed);
            EnsureColumn("residents", "state_changed_by", "VARCHAR(100) NOT NULL DEFAULT ''", changed);
            EnsureColumn("residents", "has_availed_jobseeker", BoolColumn(), changed);

            EnsureColumn("document_requests", "reference_number", "VARCHAR(30) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "or_control_number", "VARCHAR(30) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "requires_validation", BoolColumn(), changed);
            EnsureColumn("document_requests", "filed_during_office_window", BoolColumn(), changed);
            EnsureColumn("document_requests", "fee_basis", "VARCHAR(500) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_name", "VARCHAR(150) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_nature", "VARCHAR(150) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_purok", "VARCHAR(100) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_location", "VARCHAR(150) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_ownership", "VARCHAR(60) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_registration", "VARCHAR(50) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_previous_permit", "VARCHAR(50) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "business_employees", "INT NOT NULL DEFAULT 0", changed);
            EnsureColumn("document_requests", "business_is_renewal", BoolColumn(), changed);
            EnsureColumn("document_requests", "last_status_on", DateColumn(true), changed);
            EnsureColumn("document_requests", "last_status_by", "VARCHAR(100) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "assessed_amount", "DECIMAL(10,2) NOT NULL DEFAULT 0", changed);
            EnsureColumn("document_requests", "hours", "DECIMAL(6,2) NOT NULL DEFAULT 0", changed);
            EnsureColumn("document_requests", "gross_annual_income", "DECIMAL(14,2) NOT NULL DEFAULT 0", changed);
            EnsureColumn("document_requests", "detail", "VARCHAR(255) NOT NULL DEFAULT ''", changed);
            EnsureColumn("document_requests", "apply_jobseeker_waiver", BoolColumn(), changed);
            EnsureColumn("document_requests", "availed_under_jobseeker_act", BoolColumn(), changed);
            EnsureColumn("document_requests", "scope", "VARCHAR(10) NOT NULL DEFAULT 'Local'", changed);

            if (changed.Count == 0) return string.Empty;

            // The one piece of backfill that matters: an old request has no
            // reference number, so I give it one made from its year and id.
            // Without this the request would open with a blank number on the
            // counter slip.
            try
            {
                _context.Helper.ExecuteNonQuery("numbering the older requests",
                    "UPDATE document_requests SET reference_number = " +
                    BuildReferenceBackfillExpression() +
                    " WHERE reference_number = ''", null);
            }
            catch (RepositoryException error)
            {
                AppLog.Warn("Could not backfill the reference numbers: " + error.Message);
            }

            return "upgraded an older database (" + changed.Count + " column(s) added)";
        }

        private string BuildReferenceBackfillExpression()
        {
            // MySQL and SQL Server each need a different way of writing "the
            // year of the request" as text, and of padding a number to six
            // digits, so I build the expression for the engine in use.
            if (_context.Provider.Key == "SqlServer")
                return "CONVERT(VARCHAR(4), YEAR(date_requested)) + '-' + RIGHT('000000' + CONVERT(VARCHAR(6), request_id), 6)";

            return "CONCAT(YEAR(date_requested), '-', LPAD(request_id, 6, '0'))";
        }

        private string BoolColumn()
        {
            return _context.Provider.Key == "SqlServer" ? "BIT NOT NULL DEFAULT 0" : "TINYINT(1) NOT NULL DEFAULT 0";
        }

        private string DateColumn(bool nullable)
        {
            string type = _context.Provider.Key == "SqlServer" ? "DATETIME2(0)" : "DATETIME";
            return type + (nullable ? " NULL" : " NOT NULL");
        }

        private void EnsureColumn(string tableName, string columnName, string definition, List<string> changed)
        {
            if (ColumnExists(tableName, columnName)) return;

            try
            {
                _context.Helper.ExecuteNonQuery("upgrading the database",
                    "ALTER TABLE " + tableName + " ADD " + columnName + " " + definition, null);

                changed.Add(tableName + "." + columnName);
            }
            catch (RepositoryException error)
            {
                AppLog.Warn("Could not add " + tableName + "." + columnName + ": " + error.Message);
            }
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            string sql = _context.Provider.Key == "SqlServer"
                ? "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name = @column"
                : "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() " +
                  "AND table_name = @table AND column_name = @column";

            SqlArguments arguments = new SqlArguments()
                .Add("@table", _context.Provider.Key == "SqlServer" ? "dbo." + tableName : tableName)
                .Add("@column", columnName);

            object value = _context.Helper.ExecuteScalar("checking the database structure", sql, arguments);
            return value != null && Convert.ToInt32(value) > 0;
        }

        // ==================================================================
        //  Small questions to the database
        // ==================================================================

        private bool TableExists(string tableName)
        {
            try
            {
                _context.Helper.ExecuteScalar("checking the tables",
                    "SELECT COUNT(*) FROM " + tableName, null);
                return true;
            }
            catch (RepositoryException)
            {
                return false;
            }
        }

        private bool ProcedureExists(string procedureName)
        {
            string sql = _context.Provider.Key == "SqlServer"
                ? "SELECT COUNT(*) FROM sys.objects WHERE type = 'P' AND name = @name"
                : "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() " +
                  "AND routine_type = 'PROCEDURE' AND routine_name = @name";

            object value = _context.Helper.ExecuteScalar("checking the stored procedures", sql,
                new SqlArguments().Add("@name", procedureName));

            return value != null && Convert.ToInt32(value) > 0;
        }

        private int CountRows(string tableName)
        {
            try
            {
                object value = _context.Helper.ExecuteScalar("counting the rows",
                    "SELECT COUNT(*) FROM " + tableName, null);
                return value == null ? 0 : Convert.ToInt32(value);
            }
            catch (RepositoryException)
            {
                return 0;
            }
        }

        /// <summary>
        /// A tiny helper so the account seeding can use the same repository
        /// code path the rest of the program uses, instead of its own INSERT.
        /// </summary>
        private class SqlWork : IDisposable
        {
            private readonly DBContext _context;

            public SqlWork(DBContext context)
            {
                _context = context;
            }

            public void InsertUser(UserAccount account)
            {
                Data.Sql.SqlBarangayRepository repository = new Data.Sql.SqlBarangayRepository(_context);
                repository.InsertUser(account);
            }

            public void Dispose()
            {
            }
        }
    }
}
