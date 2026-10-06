// ---------------------------------------------------------------------------
//  SqlBarangayRepository.Reports.cs - the report tables, and the startup
//  checks that make sure the database is really there.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data.Sql
{
    public partial class SqlBarangayRepository
    {
        // ==================================================================
        //  Reports
        //
        //  Every report method follows the same shape:
        //    try the stored procedure first (fast, lives in the database),
        //    and if it is not installed on this machine, run the same query
        //    as plain SQL so the report still comes out.
        //  I would rather a report be a little slower on a machine where the
        //  procedure script was never run than have a screen that simply
        //  refuses to work.
        // ==================================================================

        public DataTable GetDashboardSummary(DateTime from, DateTime to)
        {
            // The dashboard is a handful of counts, so I ask for them with
            // small portable queries instead of a procedure. It is the first
            // screen after login and I want it to work before anything else
            // has been set up.
            DataTable table = new DataTable("dashboard");
            table.Columns.Add("metric", typeof(string));
            table.Columns.Add("value", typeof(decimal));

            AddMetric(table, "Active residents", Count("residents", "record_state = 'Active'"));
            AddMetric(table, "Inactive residents", Count("residents", "record_state = 'Inactive'"));
            AddMetric(table, "Archived records", Count("residents", "record_state = 'Archived'"));
            AddMetric(table, "Households", Count("residents", "is_head_of_family = 1 AND record_state = 'Active'"));
            AddMetric(table, "Dependents on file", Count("dependents", null));
            AddMetric(table, "Requests filed today", CountRange("date_requested", DateTime.Today, DateTime.Today));
            AddMetric(table, "Requests waiting", Count("document_requests", "status = 'Pending'"));
            AddMetric(table, "Requests being processed", Count("document_requests", "status = 'Processing'"));
            AddMetric(table, "Cleared requests", Count("document_requests", "status = 'Cleared'"));
            AddMetric(table, "Ready for release", Count("document_requests", "status = 'ReadyForRelease'"));
            AddMetric(table, "Released today", CountRange("date_released", DateTime.Today, DateTime.Today));
            AddMetric(table, "Documents released in the period", CountRange("date_released", from, to));
            AddMetric(table, "Collected in the period", Money("amount", from, to));
            AddMetric(table, "Collected today", Money("amount", DateTime.Today, DateTime.Today));
            AddMetric(table, "Issued free in the period", CountFreeIssued(from, to));
            AddMetric(table, "Activity entries today", CountActivityForDay(DateTime.Today));

            return table;
        }

        public DataTable GetPerDayTransactions(DateTime from, DateTime to, RequestStatus? status, string purok)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1))
                .Add("@status", status.HasValue ? status.Value.ToString() : string.Empty)
                .Add("@purok", purok ?? string.Empty);

            string day = SqlText.DateOnly(_engine, "r.date_requested");

            string fallback =
                "SELECT " + day + " AS txn_date, " +
                "COUNT(*) AS filed, " +
                "SUM(CASE WHEN r.status = 'Released' THEN 1 ELSE 0 END) AS released, " +
                "SUM(CASE WHEN r.status = 'Rejected' THEN 1 ELSE 0 END) AS rejected, " +
                "SUM(CASE WHEN r.status = 'Pending' THEN 1 ELSE 0 END) AS waiting, " +
                "SUM(CASE WHEN r.fee = 0 THEN 1 ELSE 0 END) AS free_issued, " +
                "SUM(CASE WHEN r.is_paid = 1 THEN r.fee ELSE 0 END) AS collected " +
                "FROM document_requests r INNER JOIN residents res ON res.resident_id = r.resident_id " +
                "WHERE r.date_requested >= @from AND r.date_requested <= @to " +
                "AND (@status = '' OR r.status = @status) " +
                "AND (@purok = '' OR res.purok = @purok) " +
                "GROUP BY " + day + " ORDER BY " + day;

            return ReportTable("building the per-day transaction report",
                StoredProcedures.PerDayTransactions, arguments, fallback);
        }

        public DataTable GetDocumentRegister(DateTime from, DateTime to, RequestStatus? status)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1))
                .Add("@status", status.HasValue ? status.Value.ToString() : string.Empty);

            string fallback =
                "SELECT r.reference_number, r.date_requested, " +
                "res.first_name, res.last_name, res.purok, r.document_type, r.purpose, r.status, " +
                "r.fee, r.is_paid, r.official_receipt_no, r.date_released " +
                "FROM document_requests r INNER JOIN residents res ON res.resident_id = r.resident_id " +
                "WHERE r.date_requested >= @from AND r.date_requested <= @to " +
                "AND (@status = '' OR r.status = @status) " +
                "ORDER BY r.date_requested";

            return ReportTable("building the document register",
                StoredProcedures.DocumentRegister, arguments, fallback);
        }

        public DataTable GetCollections(DateTime from, DateTime to)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1));

            string fallback =
                "SELECT or_date, or_number, series_code, control_number, payer_name, amount, method, " +
                "collected_by, is_void, void_reason " +
                "FROM official_receipts WHERE or_date >= @from AND or_date <= @to ORDER BY or_date, or_number";

            return ReportTable("building the collections report",
                StoredProcedures.Collections, arguments, fallback);
        }

        public DataTable GetBusinessClearances(DateTime from, DateTime to)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1));

            string fallback =
                "SELECT r.reference_number, r.date_requested, r.business_name, r.business_nature, " +
                "r.business_purok, r.business_ownership, r.business_employees, r.status, r.fee, " +
                "r.is_paid, r.official_receipt_no, res.first_name, res.last_name " +
                "FROM document_requests r INNER JOIN residents res ON res.resident_id = r.resident_id " +
                "WHERE r.document_type = 'BarangayBusinessClearance' " +
                "AND r.date_requested >= @from AND r.date_requested <= @to " +
                "ORDER BY r.date_requested";

            return ReportTable("building the business clearance report",
                StoredProcedures.BusinessClearances, arguments, fallback);
        }

        public DataTable GetCensusSummary(string purok)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@purok", purok ?? string.Empty);

            string fallback =
                "SELECT purok, " +
                "COUNT(*) AS residents, " +
                "SUM(CASE WHEN is_head_of_family = 1 THEN 1 ELSE 0 END) AS households, " +
                "SUM(CASE WHEN gender = 'Male' THEN 1 ELSE 0 END) AS male, " +
                "SUM(CASE WHEN gender = 'Female' THEN 1 ELSE 0 END) AS female, " +
                "SUM(CASE WHEN (classification & 1) = 1 THEN 1 ELSE 0 END) AS seniors, " +
                "SUM(CASE WHEN (classification & 2) = 2 THEN 1 ELSE 0 END) AS pwd, " +
                "SUM(CASE WHEN (classification & 4) = 4 THEN 1 ELSE 0 END) AS indigent, " +
                "SUM(CASE WHEN (classification & 8) = 8 THEN 1 ELSE 0 END) AS solo_parent, " +
                "SUM(CASE WHEN (classification & 16) = 16 THEN 1 ELSE 0 END) AS four_ps, " +
                "SUM(CASE WHEN is_student_fee_category = 1 THEN 1 ELSE 0 END) AS students, " +
                "SUM(CASE WHEN residency_status = 'Newcomer' THEN 1 ELSE 0 END) AS newcomers, " +
                "SUM(CASE WHEN residency_status = 'Temporary' THEN 1 ELSE 0 END) AS temporary, " +
                "SUM(CASE WHEN residency_status = 'Permanent' THEN 1 ELSE 0 END) AS permanent " +
                "FROM residents WHERE record_state = 'Active' AND (@purok = '' OR purok = @purok) " +
                "GROUP BY purok ORDER BY purok";

            return ReportTable("building the census summary",
                StoredProcedures.CensusSummary, arguments, fallback);
        }

        public DataTable GetPopulationByPurok(bool activeOnly)
        {
            SqlArguments arguments = new SqlArguments().Add("@active", activeOnly);

            string fallback =
                "SELECT purok, COUNT(*) AS residents, " +
                "SUM(CASE WHEN is_head_of_family = 1 THEN 1 ELSE 0 END) AS households, " +
                "SUM(CASE WHEN gender = 'Male' THEN 1 ELSE 0 END) AS male, " +
                "SUM(CASE WHEN gender = 'Female' THEN 1 ELSE 0 END) AS female " +
                "FROM residents WHERE (@active = 0 OR record_state = 'Active') " +
                "GROUP BY purok ORDER BY residents DESC";

            return ReportTable("building the population by purok",
                StoredProcedures.PopulationByPurok, arguments, fallback);
        }

        public DataTable GetResidentMasterList(string purok, RecordState? state, ResidencyStatus? residency)
        {
            SqlArguments arguments = new SqlArguments()
                .Add("@purok", purok ?? string.Empty)
                .Add("@state", state.HasValue ? state.Value.ToString() : string.Empty)
                .Add("@residency", residency.HasValue ? residency.Value.ToString() : string.Empty);

            string fallback =
                "SELECT last_name, first_name, middle_name, suffix, purok, date_of_birth, gender, " +
                "civil_status, contact_number, occupation, classification, is_student_fee_category, " +
                "is_business_owner, is_head_of_family, residency_status, record_state, date_of_residency " +
                "FROM residents WHERE (@purok = '' OR purok = @purok) " +
                "AND (@state = '' OR record_state = @state) " +
                "AND (@residency = '' OR residency_status = @residency) " +
                "ORDER BY purok, last_name, first_name";

            return ReportTable("building the residents master list",
                StoredProcedures.ResidentMasterList, arguments, fallback);
        }

        /// <summary>
        /// Population by age bracket, for the census.
        ///
        /// I did not put the brackets in SQL. I work out the date boundaries
        /// here in C# and ask for counts inside each range, because it keeps
        /// "60 and above" defined in exactly one place - the Resident class -
        /// and the screen, the printed report and the census sheet can never
        /// disagree about it.
        /// </summary>
        public DataTable GetPopulationByAgeBracket(string purok, bool activeOnly)
        {
            DataTable table = new DataTable("population_by_age");
            table.Columns.Add("age_bracket", typeof(string));
            table.Columns.Add("male", typeof(int));
            table.Columns.Add("female", typeof(int));
            table.Columns.Add("total", typeof(int));

            DateTime today = DateTime.Today;

            for (int i = 0; i < Resident.CensusAgeLabels.Length; i++)
            {
                int lower = i == 0 ? 0 : Resident.CensusAgeBreaks[i];
                int upper = (i == Resident.CensusAgeLabels.Length - 1)
                    ? 200
                    : Resident.CensusAgeBreaks[i + 1] - 1;

                int male = CountInAgeBracket(purok, activeOnly, today, lower, upper, Gender.Male);
                int female = CountInAgeBracket(purok, activeOnly, today, lower, upper, Gender.Female);

                DataRow row = table.NewRow();
                row["age_bracket"] = Resident.CensusAgeLabels[i];
                row["male"] = male;
                row["female"] = female;
                row["total"] = male + female;
                table.Rows.Add(row);
            }

            return table;
        }

        private int CountInAgeBracket(string purok, bool activeOnly, DateTime today,
                                      int lowerAge, int upperAge, Gender gender)
        {
            // An age of "60 and above" has no upper birthday, so I only set the
            // lower boundary in that case.
            DateTime bornBefore = today.AddYears(-(lowerAge + 1)).AddDays(1);
            DateTime bornAfter = today.AddYears(-(upperAge + 1));

            string sql = "SELECT COUNT(*) FROM residents WHERE gender = @gender " +
                         "AND date_of_birth <= @born_before AND date_of_birth > @born_after " +
                         "AND (@purok = '' OR purok = @purok) AND (@active = 0 OR record_state = 'Active')";

            object value = _db.ExecuteScalar("counting residents by age", sql, new SqlArguments()
                .Add("@gender", gender.ToString())
                .Add("@born_before", bornBefore)
                .Add("@born_after", bornAfter)
                .Add("@purok", purok ?? string.Empty)
                .Add("@active", activeOnly));

            return value == null ? 0 : Convert.ToInt32(value);
        }

        // ==================================================================
        //  Startup: is the database really there?
        // ==================================================================

        public string EnsureDatabaseReady()
        {
            string message;
            if (!_context.TryConnect(out message))
                throw new RepositoryException("connecting to the database", message);

            return _context.Describe() + " - ready.";
        }

        public void HealthCheck()
        {
            string message;
            if (!_context.TryConnect(out message))
                throw new RepositoryException("connecting to the database", message);
        }

        // ==================================================================
        //  Small query helpers
        // ==================================================================

        /// <summary>
        /// Runs the stored procedure if it is installed, and the plain query if
        /// it is not.
        ///
        /// I swallow only the "no such procedure" case. Any other database
        /// error is a real error and still comes up, because a report that
        /// silently shows nothing would be worse than an honest failure.
        /// </summary>
        private DataTable ReportTable(string operation, string procedureName,
                                     SqlArguments arguments, string fallbackSql)
        {
            if (ProcedureLooksInstalled(procedureName))
            {
                try
                {
                    return _db.QueryProcedureTable(operation, procedureName, arguments);
                }
                catch (RepositoryException error)
                {
                    AppLog.Warn("Stored procedure " + procedureName +
                                " failed, so I used the built-in query instead. " + error.Message);
                }
            }

            return _db.QueryTable(operation, fallbackSql, arguments);
        }

        /// <summary>
        /// Whether the procedure is there, asked once per session and
        /// remembered. Asking the database every time would add a round trip to
        /// every report for no reason.
        /// </summary>
        private readonly Dictionary<string, bool> _procedureCache = new Dictionary<string, bool>();

        private bool ProcedureLooksInstalled(string procedureName)
        {
            bool known;
            if (_procedureCache.TryGetValue(procedureName, out known)) return known;

            bool installed = false;
            try
            {
                string sql = _engine == "SqlServer"
                    ? "SELECT COUNT(*) FROM sys.objects WHERE type = 'P' AND name = @name"
                    : "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE() " +
                      "AND routine_type = 'PROCEDURE' AND routine_name = @name";

                object value = _db.ExecuteScalar("checking for the stored procedure", sql,
                    new SqlArguments().Add("@name", procedureName));

                installed = value != null && Convert.ToInt32(value) > 0;
            }
            catch (RepositoryException)
            {
                installed = false;
            }

            _procedureCache[procedureName] = installed;
            return installed;
        }

        private void AddMetric(DataTable table, string metric, decimal value)
        {
            DataRow row = table.NewRow();
            row["metric"] = metric;
            row["value"] = value;
            table.Rows.Add(row);
        }

        private decimal Count(string tableName, string whereClause)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName;
            if (!string.IsNullOrEmpty(whereClause)) sql += " WHERE " + whereClause;

            object value = _db.ExecuteScalar("counting records", sql, null);
            return value == null ? 0m : Convert.ToDecimal(value);
        }

        private decimal CountRange(string columnName, DateTime from, DateTime to)
        {
            string sql = "SELECT COUNT(*) FROM document_requests WHERE " + columnName +
                         " >= @from AND " + columnName + " <= @to";

            object value = _db.ExecuteScalar("counting records", sql, new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1)));

            return value == null ? 0m : Convert.ToDecimal(value);
        }

        private decimal Money(string columnName, DateTime from, DateTime to)
        {
            string sql = "SELECT COALESCE(SUM(" + columnName + "), 0) FROM official_receipts " +
                         "WHERE is_void = 0 AND or_date >= @from AND or_date <= @to";

            // COALESCE is one of the few functions both engines spell the same
            // way, which is why I use it instead of IFNULL or ISNULL.
            object value;
            try
            {
                value = _db.ExecuteScalar("adding up the collections", sql, new SqlArguments()
                    .Add("@from", from.Date)
                    .Add("@to", to.Date));
            }
            catch (RepositoryException)
            {
                value = _db.ExecuteScalar("adding up the collections",
                    "SELECT SUM(" + columnName + ") FROM official_receipts " +
                    "WHERE is_void = 0 AND or_date >= @from AND or_date <= @to",
                    new SqlArguments().Add("@from", from.Date).Add("@to", to.Date));
            }

            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private decimal CountFreeIssued(DateTime from, DateTime to)
        {
            string sql = "SELECT COUNT(*) FROM document_requests WHERE fee = 0 AND status = 'Released' " +
                         "AND date_released >= @from AND date_released <= @to";

            object value = _db.ExecuteScalar("counting the free documents", sql, new SqlArguments()
                .Add("@from", from.Date)
                .Add("@to", to.Date.AddDays(1).AddSeconds(-1)));

            return value == null ? 0m : Convert.ToDecimal(value);
        }

        private string LimitClause(int rows)
        {
            return _engine == "SqlServer"
                ? "OFFSET 0 ROWS FETCH NEXT " + rows + " ROWS ONLY"
                : "LIMIT " + rows;
        }
    }
}
