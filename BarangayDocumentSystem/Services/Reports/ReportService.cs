// ---------------------------------------------------------------------------
//  ReportService.cs - runs a report and brings back the rows.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Reports
{
    /// <summary>
    /// Builds every report the system offers.
    ///
    /// The important decision here is that a report is just a table of rows
    /// with a title. I do not build a Crystal report object and I do not build
    /// a printed page in this class - I produce the rows, and three different
    /// things can then show them:
    ///
    ///   1. the Crystal Reports viewer, when the runtime is installed and the
    ///      .rpt template exists (that is the FR-17 requirement);
    ///   2. the built-in viewer and printer, which always work;
    ///   3. the CSV export, for whoever wants to open it in Excel.
    ///
    /// That is why the report screen never says "report unavailable": the worst
    /// case is that it opens in the built-in viewer instead of Crystal.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly SessionManagerAccess _session;
        private readonly IClock _clock;

        /// <summary>A tiny wrapper so this class does not need the whole
        /// session type just to know who to credit the report to.</summary>
        public class SessionManagerAccess
        {
            public string Username { get; set; }
            public SessionManagerAccess(string username) { Username = username; }
        }

        public ReportService(IBarangayRepository repository, ActivityLogService log, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _clock = clock == null ? new SystemClock() : clock;
            _session = new SessionManagerAccess(string.Empty);
        }

        public ReportService(IBarangayRepository repository, ActivityLogService log, IClock clock, string username)
            : this(repository, log, clock)
        {
            _session = new SessionManagerAccess(username ?? string.Empty);
        }

        // ==================================================================
        //  The menu
        // ==================================================================

        public IList<ReportDefinition> GetDefinitions()
        {
            return ReportDefinitions.All();
        }

        public ReportDefinition GetDefinition(string key)
        {
            return ReportDefinitions.Find(key);
        }

        // ==================================================================
        //  Running one
        // ==================================================================

        public ReportResult Run(string reportKey, ReportParameters parameters)
        {
            if (parameters == null) parameters = new ReportParameters();

            ReportDefinition definition = ReportDefinitions.Find(reportKey);
            if (definition == null)
                throw new InvalidOperationException("I do not know a report called \"" + reportKey + "\".");

            ReportResult result = new ReportResult();
            result.Key = definition.Key;
            result.Title = definition.Title;
            result.Subtitle = definition.NeedsDateRange
                ? parameters.GetRangeText()
                : (string.IsNullOrWhiteSpace(parameters.Purok) ? "Whole barangay" : parameters.Purok);

            switch (reportKey)
            {
                case ReportDefinitions.DailyTransactions:
                    result.Table = _repository.GetPerDayTransactions(parameters.From, parameters.To,
                        parameters.Status, parameters.Purok);
                    result.Columns = Names("Date", "Filed", "Released", "Rejected", "Waiting", "Free", "Collected");
                    result.Totals = TotalOf(result.Table, "collected", "Collected in the period");
                    break;

                case ReportDefinitions.DocumentRegister:
                    result.Table = _repository.GetDocumentRegister(parameters.From, parameters.To, parameters.Status);
                    result.Columns = Names("Reference", "Filed", "First name", "Last name", "Purok", "Document",
                        "Purpose", "Status", "Fee", "Paid", "OR number", "Released");
                    result.Totals = "Rows: " + result.Table.Rows.Count;
                    break;

                case ReportDefinitions.Collections:
                    result.Table = _repository.GetCollections(parameters.From, parameters.To);
                    result.Columns = Names("Date", "OR number", "Series", "Control", "Payer", "Amount",
                        "Method", "Collected by", "Void", "Reason");
                    result.Totals = TotalOf(result.Table, "amount", "Total collected");
                    break;

                case ReportDefinitions.BusinessClearances:
                    result.Table = _repository.GetBusinessClearances(parameters.From, parameters.To);
                    result.Columns = Names("Reference", "Filed", "Business", "Nature", "Purok", "Ownership",
                        "Employees", "Status", "Fee", "Paid", "OR number", "Owner first", "Owner last");
                    result.Totals = TotalOf(result.Table, "fee", "Fees assessed");
                    break;

                case ReportDefinitions.CensusSummary:
                    result.Table = _repository.GetCensusSummary(parameters.Purok);
                    result.Columns = Names("Purok", "Residents", "Households", "Male", "Female", "Seniors",
                        "PWD", "Indigent", "Solo parent", "4Ps", "Students", "Newcomers", "Temporary", "Permanent");
                    result.Totals = TotalOf(result.Table, "residents", "Total residents");
                    break;

                case ReportDefinitions.PopulationByAge:
                    result.Table = _repository.GetPopulationByAgeBracket(parameters.Purok, parameters.ActiveResidentsOnly);
                    result.Columns = Names("Age group", "Male", "Female", "Total");
                    result.Totals = TotalOf(result.Table, "total", "Total population");
                    break;

                case ReportDefinitions.PopulationByPurok:
                    result.Table = _repository.GetPopulationByPurok(parameters.ActiveResidentsOnly);
                    result.Columns = Names("Purok", "Residents", "Households", "Male", "Female");
                    result.Totals = TotalOf(result.Table, "residents", "Total residents");
                    break;

                case ReportDefinitions.ResidentsMasterList:
                    result.Table = _repository.GetResidentMasterList(parameters.Purok, null, null);
                    result.Columns = Names("Last name", "First name", "Middle name", "Suffix", "Purok",
                        "Date of birth", "Sex", "Civil status", "Contact", "Occupation", "Classification",
                        "Student", "Business", "Head of family", "Residency", "Record state", "Resident since");
                    result.Totals = "Rows: " + result.Table.Rows.Count;
                    break;

                case ReportDefinitions.HouseholdsAndDependents:
                    result.Table = BuildHouseholdTable(parameters.Purok);
                    result.Columns = Names("Purok", "Head of the family", "Classification", "Dependents",
                        "Household size", "Dependents (name, relation, age)");
                    result.Totals = TotalOf(result.Table, "household_size", "Total people in these households");
                    break;

                case ReportDefinitions.ActivityLog:
                    result.Table = _repository.GetActivityLogTable(new ActivityLogQuery
                    {
                        From = parameters.From,
                        To = parameters.To,
                        Username = parameters.Username,
                        Module = parameters.Module,
                        MaximumRows = 500
                    });
                    result.Columns = Names("When", "User", "Role", "Module", "Action", "Target", "Reference", "Details");
                    result.Totals = "Rows: " + result.Table.Rows.Count;
                    break;

                case ReportDefinitions.FeeSchedule:
                    result.Table = BuildFeeScheduleTable();
                    result.Columns = Names("Document or service", "Amount", "Basis");
                    result.Totals = "Amounts come from App.config, so the barangay changes them without a rebuild.";
                    break;

                default:
                    throw new InvalidOperationException("Report \"" + reportKey + "\" has no query yet.");
            }

            result.Table.TableName = definition.Key;

            // Running a report reads data, and reading the registry or the
            // collection register is worth a line in the log.
            if (_log != null)
                _log.RecordAs(_session.Username, UserRole.Clerk, ActivityModule.Reports, "Ran report",
                    "Report", definition.Title,
                    "Ran the " + definition.Title + " report for " + result.Subtitle + ".");

            AppLog.Info("Ran the report: " + definition.Title + " (" + result.Subtitle + ").");
            return result;
        }

        // ==================================================================
        //  Reports built from the plain lists
        // ==================================================================

        /// <summary>
        /// The households report is built here rather than in SQL, because the
        /// household is a relationship between two tables that the barangay
        /// models in its own head - head of the family plus dependents - and
        /// writing it in C# keeps it readable for whoever maintains this next.
        /// </summary>
        private DataTable BuildHouseholdTable(string purok)
        {
            DataTable table = new DataTable("households");
            table.Columns.Add("purok", typeof(string));
            table.Columns.Add("head_of_family", typeof(string));
            table.Columns.Add("classification", typeof(string));
            table.Columns.Add("dependents", typeof(int));
            table.Columns.Add("household_size", typeof(int));
            table.Columns.Add("dependents_list", typeof(string));

            ResidentQuery query = new ResidentQuery();
            query.IncludeInactive = false;
            query.Purok = purok ?? string.Empty;

            foreach (Resident resident in _repository.GetResidents(query))
            {
                if (!resident.IsHeadOfFamily) continue;

                IList<Dependent> dependents = _repository.GetDependents(resident.ResidentId);

                List<string> names = new List<string>();
                foreach (Dependent dependent in dependents)
                    names.Add(dependent.FullName + " (" + dependent.GetRelationText() + ", " + dependent.GetAge() + ")");

                DataRow row = table.NewRow();
                row["purok"] = resident.Purok;
                row["head_of_family"] = resident.GetFullName();
                row["classification"] = resident.GetClassificationText();
                row["dependents"] = dependents.Count;
                row["household_size"] = dependents.Count + 1;
                row["dependents_list"] = string.Join("; ", names.ToArray());
                table.Rows.Add(row);
            }

            return table;
        }

        private static DataTable BuildFeeScheduleTable()
        {
            DataTable table = new DataTable("fee_schedule");
            table.Columns.Add("item", typeof(string));
            table.Columns.Add("amount", typeof(string));
            table.Columns.Add("basis", typeof(string));

            foreach (string[] row in new FeeSchedule().GetScheduleTable())
            {
                DataRow created = table.NewRow();
                created["item"] = row[0];
                created["amount"] = row[1];
                created["basis"] = row[2];
                table.Rows.Add(created);
            }

            return table;
        }

        // ==================================================================
        //  Crystal Reports
        // ==================================================================

        public bool IsCrystalAvailable()
        {
            return CrystalReportGateway.IsRuntimeInstalled();
        }

        public bool HasCrystalTemplate(string reportKey)
        {
            ReportDefinition definition = ReportDefinitions.Find(reportKey);
            if (definition == null || string.IsNullOrWhiteSpace(definition.CrystalTemplate)) return false;

            return CrystalReportGateway.TemplateExists(definition.CrystalTemplate);
        }

        // ==================================================================
        //  Export
        // ==================================================================

        /// <summary>
        /// Writes the rows out as a CSV file, which is what the barangay uses
        /// when somebody asks for the numbers in Excel. Excel opens it directly
        /// on this machine, and it needs no library and no installation.
        /// </summary>
        public string ExportCsv(ReportResult result, string folder)
        {
            if (result == null || result.Table == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(folder)) folder = AppConfig.ApplicationFolder;

            string fileName = result.Key + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
            string path = System.IO.Path.Combine(folder, fileName);

            System.Text.StringBuilder text = new System.Text.StringBuilder();

            for (int i = 0; i < result.Table.Columns.Count; i++)
            {
                if (i > 0) text.Append(',');
                text.Append(Quote(result.Table.Columns[i].ColumnName));
            }
            text.AppendLine();

            foreach (DataRow row in result.Table.Rows)
            {
                for (int i = 0; i < result.Table.Columns.Count; i++)
                {
                    if (i > 0) text.Append(',');
                    text.Append(Quote(Convert.ToString(row[i])));
                }
                text.AppendLine();
            }

            System.IO.File.WriteAllText(path, text.ToString(), System.Text.Encoding.UTF8);
            return path;
        }

        private static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // ==================================================================
        //  Small helpers
        // ==================================================================

        private static List<string> Names(params string[] names)
        {
            return new List<string>(names);
        }

        /// <summary>Adds up one column for the totals line. A report without a
        /// total is a report somebody will add up by hand, and get wrong.</summary>
        private static string TotalOf(DataTable table, string columnName, string label)
        {
            if (table == null || !table.Columns.Contains(columnName)) return string.Empty;

            decimal total = 0m;
            foreach (DataRow row in table.Rows)
            {
                object value = row[columnName];
                if (value == null || value == DBNull.Value) continue;

                decimal parsed;
                if (decimal.TryParse(Convert.ToString(value), out parsed)) total += parsed;
            }

            string formatted = columnName == "collected" || columnName == "amount" || columnName == "fee"
                ? "P" + total.ToString("#,##0.00")
                : total.ToString("#,##0");

            return label + ": " + formatted;
        }
    }
}
