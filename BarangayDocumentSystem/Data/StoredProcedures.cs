// ---------------------------------------------------------------------------
//  StoredProcedures.cs - the names of the procedures the system uses.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Collections.Generic;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// Every stored procedure the program calls, named in one place.
    ///
    /// Why stored procedures at all, when I could write the same SELECT from
    /// C#? Two reasons that matter to the barangay:
    ///
    /// 1. The heavy report queries (census by age bracket, collections per
    ///    day, the document register) do their summing and grouping inside the
    ///    database, so the program pulls back a dozen rows instead of the whole
    ///    table. On an office computer that is the difference between an
    ///    instant report and a spinning hourglass.
    ///
    /// 2. The barangay's own IT person can adjust a report without me, without
    ///    Visual Studio and without a rebuild - the procedure is a database
    ///    object.
    ///
    /// I never rely on them blindly: if a procedure is missing on a machine
    /// where the script was not run, the repository notices and runs the same
    /// query as plain SQL instead. The report still comes out; the only
    /// difference is speed.
    /// </summary>
    public static class StoredProcedures
    {
        // ---- reports -------------------------------------------------------
        public const string DashboardSummary = "sp_dashboard_summary";
        public const string PerDayTransactions = "sp_report_per_day_transactions";
        public const string DocumentRegister = "sp_report_document_register";
        public const string Collections = "sp_report_collections";
        public const string BusinessClearances = "sp_report_business_clearances";
        public const string CensusSummary = "sp_report_census_summary";
        public const string PopulationByAge = "sp_report_population_by_age";
        public const string PopulationByPurok = "sp_report_population_by_purok";
        public const string ResidentMasterList = "sp_report_resident_master";
        public const string ActivityLogSearch = "sp_activity_log_search";

        // ---- workflow ------------------------------------------------------
        /// <summary>Moves every pending request that was filed outside office
        /// hours to Cleared, once the office window has opened again. The app
        /// runs it on startup so the queue is honest on a Monday morning.</summary>
        public const string ClearWaitingRequests = "sp_clear_waiting_requests";

        /// <summary>All of them, for the admin screen that shows which
        /// procedure script was run on this machine.</summary>
        public static readonly string[] All =
        {
            DashboardSummary,
            PerDayTransactions,
            DocumentRegister,
            Collections,
            BusinessClearances,
            CensusSummary,
            PopulationByAge,
            PopulationByPurok,
            ResidentMasterList,
            ActivityLogSearch,
            ClearWaitingRequests
        };

        /// <summary>The ones the reports screen needs. I check these when the
        /// reports screen opens, so a missing script is reported once, in
        /// plain words, instead of as a slow screen later.</summary>
        public static IList<string> RequiredByReports()
        {
            return new List<string>
            {
                PerDayTransactions,
                DocumentRegister,
                Collections,
                BusinessClearances,
                CensusSummary,
                PopulationByAge,
                PopulationByPurok,
                ResidentMasterList,
                ActivityLogSearch
            };
        }
    }
}
