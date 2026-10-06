// ---------------------------------------------------------------------------
//  IReportService.cs - the contract behind the reports screen.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces
{
    /// <summary>
    /// The choices a report can be run with. Every report shows the same small
    /// panel (date range, purok, status, document type) and reads what it
    /// needs from here, so the screen has one parameter bar instead of eight.
    /// </summary>
    public class ReportParameters
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Purok { get; set; }
        public RequestStatus? Status { get; set; }
        public DocumentType? DocumentType { get; set; }
        public string Username { get; set; }
        public ActivityModule? Module { get; set; }
        public bool ActiveResidentsOnly { get; set; }

        public ReportParameters()
        {
            Purok = string.Empty;
            Username = string.Empty;
            ActiveResidentsOnly = true;
            To = DateTime.Today;
            From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        /// <summary>The last instant of the day, so a range ending today still
        /// catches a request filed at four in the afternoon.</summary>
        public DateTime EndOfDay
        {
            get { return To.Date.AddDays(1).AddSeconds(-1); }
        }

        public string GetRangeText()
        {
            if (From.Date == To.Date) return From.ToString("dd MMMM yyyy");
            return From.ToString("dd MMM yyyy") + " to " + To.ToString("dd MMM yyyy");
        }
    }

    /// <summary>
    /// One report, ready to look at: a title, the rows, the columns I want
    /// printed, and the totals line.
    /// </summary>
    public class ReportResult
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public DataTable Table { get; set; }

        /// <summary>The column headings to print, in order. Crystal Reports
        /// uses its own layout in the .rpt file; this list is what the
        /// built-in viewer and the printer use, so both look the same.</summary>
        public List<string> Columns { get; set; }

        /// <summary>A closing sentence, e.g. the total collected.</summary>
        public string Totals { get; set; }

        public ReportResult()
        {
            Key = string.Empty;
            Title = string.Empty;
            Subtitle = string.Empty;
            Columns = new List<string>();
            Totals = string.Empty;
        }
    }

    /// <summary>
    /// One entry in the reports menu.
    ///
    /// The Crystal Reports .rpt template is named here too. If the barangay
    /// has the SAP Crystal Reports runtime installed and the template file is
    /// in the configured folder, the report opens in the Crystal viewer;
    /// otherwise the same rows open in the built-in viewer, so the report is
    /// never simply unavailable.
    /// </summary>
    public class ReportDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        /// <summary>The .rpt file name, without the folder. Empty when there
        /// is no Crystal template (the built-in viewer is then the only way
        /// to see it).</summary>
        public string CrystalTemplate { get; set; }

        /// <summary>True when this report wants a date range.</summary>
        public bool NeedsDateRange { get; set; }

        /// <summary>True when this report wants a purok.</summary>
        public bool NeedsPurok { get; set; }

        /// <summary>True when this report wants a request status.</summary>
        public bool NeedsStatus { get; set; }

        public ReportDefinition()
        {
            Key = string.Empty;
            Title = string.Empty;
            Description = string.Empty;
            CrystalTemplate = string.Empty;
            NeedsDateRange = true;
        }
    }

    /// <summary>What the reports screen and the test checks talk to.</summary>
    public interface IReportService
    {
        /// <summary>The reports menu, in the order I want them listed.</summary>
        IList<ReportDefinition> GetDefinitions();

        ReportDefinition GetDefinition(string key);

        /// <summary>Runs one report and brings back the rows.</summary>
        ReportResult Run(string reportKey, ReportParameters parameters);

        /// <summary>True when the SAP Crystal Reports runtime is installed on
        /// this machine, so the screen can say so instead of failing mysteriously.</summary>
        bool IsCrystalAvailable();

        /// <summary>True when the .rpt template for that report can be found.</summary>
        bool HasCrystalTemplate(string reportKey);
    }
}
