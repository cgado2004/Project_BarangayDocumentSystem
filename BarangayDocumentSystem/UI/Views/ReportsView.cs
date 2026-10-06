// ---------------------------------------------------------------------------
//  ReportsView.cs - every report, with Crystal Reports when it is there.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services.Reports;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// Every report the barangay uses, in one place.
    ///
    /// How this screen is put together, because it is the answer to "Crystal
    /// Report" and to "will it work on my computer?" at the same time:
    ///
    ///  * the rows come from the database, from a stored procedure per report,
    ///    and they are shown in the table below whatever else happens;
    ///  * if Crystal Reports is installed on this computer, a layout file lays
    ///    those same rows out and the report prints the way the office is used
    ///    to seeing it;
    ///  * if it is not installed, the table is still there, the report can be
    ///    exported as a CSV and opened in Excel, and it can be printed on plain
    ///    paper.
    ///
    /// That is deliberate. A barangay office should not lose its reports
    /// because a runtime is missing - and the screen says plainly which of the
    /// three is happening, so nobody is left guessing why a button is grey.
    /// </summary>
    public class ReportsView : ViewBase
    {
        private ListBox _list;
        private DateTimePicker _from;
        private DateTimePicker _to;
        private ComboBox _purok;
        private Label _description;
        private Label _crystalNote;
        private DataGridView _grid;
        private Label _totals;
        private Button _run;
        private Button _export;
        private Button _print;
        private Button _crystal;

        private ReportResult _last;
        private ReportDefinition _selected;

        public ReportsView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Reports",
                "Choose a report, set the period, then run it. Crystal Reports is used when it is installed; "
                + "the table below, the CSV export and plain printing work everywhere.");

            SectionPanel choice = Card("What to run", null);
            choice.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 66);
            choice.Size = new Size(360, 250);
            choice.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _list = new ListBox();
            _list.Font = AppTheme.Body;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.Location = new Point(AppTheme.Gap4, 56);
            _list.Size = new Size(328, 178);
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _list.SelectedIndexChanged += delegate (object sender, EventArgs e) { ChoiceChanged(); };
            choice.Controls.Add(_list);

            SectionPanel period = Card("Period and filters", null);
            period.Location = new Point(AppTheme.PageMargin + 380, AppTheme.PageMargin + 66);
            period.Size = new Size(780, 250);
            period.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label fromLabel = UiFactory.FieldLabel("From");
            fromLabel.Location = new Point(AppTheme.Gap4, 56);
            fromLabel.Width = 170;

            _from = UiFactory.DatePicker(FirstDayOfMonth());
            _from.Location = new Point(AppTheme.Gap4, 74);
            _from.Width = 170;
            _from.ValueChanged += delegate (object sender, EventArgs e) { RangeChanged(); };

            Label toLabel = UiFactory.FieldLabel("To");
            toLabel.Location = new Point(AppTheme.Gap4 + 190, 56);
            toLabel.Width = 170;

            _to = UiFactory.DatePicker(Clock.Now().Date);
            _to.Location = new Point(AppTheme.Gap4 + 190, 74);
            _to.Width = 170;
            _to.ValueChanged += delegate (object sender, EventArgs e) { RangeChanged(); };

            Label purokLabel = UiFactory.FieldLabel("Purok");
            purokLabel.Location = new Point(AppTheme.Gap4 + 380, 56);
            purokLabel.Width = 190;

            _purok = UiFactory.DropDown(null, true);
            FillPuroks(_purok, "Whole barangay");
            _purok.Location = new Point(AppTheme.Gap4 + 380, 74);
            _purok.Width = 190;

            _description = UiFactory.Hint(string.Empty);
            _description.Location = new Point(AppTheme.Gap4, 114);
            _description.Size = new Size(730, 34);

            _crystalNote = UiFactory.Hint(string.Empty);
            _crystalNote.Location = new Point(AppTheme.Gap4, 146);
            _crystalNote.Size = new Size(730, 34);

            _run = UiFactory.PrimaryButton("Run this report");
            _run.Location = new Point(AppTheme.Gap4, 186);
            _run.Click += delegate (object sender, EventArgs e) { Run(); };

            _export = UiFactory.SecondaryButton("Export as CSV");
            _export.Location = new Point(AppTheme.Gap4 + 190, 186);
            _export.Click += delegate (object sender, EventArgs e) { Export(); };

            _print = UiFactory.SecondaryButton("Print on plain paper");
            _print.Location = new Point(AppTheme.Gap4 + 380, 186);
            _print.Click += delegate (object sender, EventArgs e) { PrintPlain(); };

            _crystal = UiFactory.SecondaryButton("Print with Crystal");
            _crystal.Location = new Point(AppTheme.Gap4 + 600, 186);
            _crystal.Click += delegate (object sender, EventArgs e) { PrintWithCrystal(); };

            period.Controls.Add(fromLabel);
            period.Controls.Add(_from);
            period.Controls.Add(toLabel);
            period.Controls.Add(_to);
            period.Controls.Add(purokLabel);
            period.Controls.Add(_purok);
            period.Controls.Add(_description);
            period.Controls.Add(_crystalNote);
            period.Controls.Add(_run);
            period.Controls.Add(_export);
            period.Controls.Add(_print);
            period.Controls.Add(_crystal);

            SectionPanel result = Card("The report",
                "The rows come from the database; the layout is the viewer's job.");
            result.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 332);
            result.Size = new Size(1160, 380);
            result.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.Gap4, 58);
            _grid.Size = new Size(1128, 306);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            result.Controls.Add(_grid);

            _totals = UiFactory.Body(string.Empty);
            _totals.Font = AppTheme.SmallBold;
            _totals.ForeColor = AppTheme.Primary;
            _totals.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 722);
            _totals.AutoSize = true;

            page.Controls.Add(choice);
            page.Controls.Add(period);
            page.Controls.Add(result);
            page.Controls.Add(_totals);

            LoadDefinitions();
        }

        private DateTime FirstDayOfMonth()
        {
            DateTime today = Clock.Now().Date;
            return new DateTime(today.Year, today.Month, 1);
        }

        public override void RefreshData()
        {
            UpdateCrystalState();
        }

        // ==================================================================
        //  The list of reports
        // ==================================================================

        private void LoadDefinitions()
        {
            _list.Items.Clear();

            foreach (ReportDefinition definition in Reports.GetDefinitions())
                _list.Items.Add(definition.Title);

            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        }

        private void ChoiceChanged()
        {
            _selected = _list.SelectedIndex < 0
                ? null
                : Reports.GetDefinitions()[_list.SelectedIndex];

            if (_selected == null) return;

            _description.Text = _selected.Description
                + (_selected.NeedsDateRange ? string.Empty : " (This report is not tied to a period.)");

            _from.Enabled = _to.Enabled = _selected.NeedsDateRange;
            _purok.Enabled = _selected.NeedsPurok;

            UpdateCrystalState();
        }

        private void UpdateCrystalState()
        {
            if (!AppConfig.UseCrystalReports)
            {
                _crystalNote.Text = "Crystal Reports is switched off in the settings file. The table below, "
                    + "the CSV export and plain printing are being used - that is the setting, not a fault.";
                _crystal.Enabled = false;
                return;
            }

            if (!Reports.IsCrystalAvailable())
            {
                _crystalNote.Text = "Crystal Reports is not installed on this computer, so the layout cannot "
                    + "be drawn by it. Everything below still works, and the report can be printed on plain "
                    + "paper or exported as a CSV. Ask the administrator to install the Crystal Reports "
                    + "runtime if the barangay wants the printed layout.";
                _crystal.Enabled = false;
                return;
            }

            if (_selected != null && !Reports.HasCrystalTemplate(_selected.Key))
            {
                _crystalNote.Text = "Crystal Reports is installed, but the layout file for this report has "
                    + "not been copied into " + AppConfig.CrystalReportsFolder + " yet.";
                _crystal.Enabled = false;
                return;
            }

            _crystalNote.Text = "Crystal Reports is installed. Printing lays the same rows out with the "
                + "barangay's layout file from " + AppConfig.CrystalReportsFolder + ".";
            _crystal.Enabled = true;
        }

        private void RangeChanged()
        {
            if (_from.Value > _to.Value)
            {
                Dialog.Warn(this, "The 'From' date is after the 'To' date, so the period was corrected "
                    + "to start at the 'To' date.", "Please check the dates");
                _from.Value = _to.Value;
            }
        }

        // ==================================================================
        //  Running
        // ==================================================================

        private ReportParameters Parameters()
        {
            ReportParameters parameters = new ReportParameters();
            parameters.From = _from.Value.Date;
            parameters.To = _to.Value.Date;
            parameters.Purok = PurokFrom(_purok);
            parameters.ActiveResidentsOnly = true;
            return parameters;
        }

        private bool Run()
        {
            if (_selected == null)
            {
                Dialog.Warn(this, "Please choose which report to run.", "No report chosen");
                return false;
            }

            Cursor = Cursors.WaitCursor;
            _run.Enabled = false;

            try
            {
                _last = Reports.Run(_selected.Key, Parameters());

                _grid.DataSource = _last.Table;
                _grid.AutoGenerateColumns = true;

                int rows = _last.Table == null ? 0 : _last.Table.Rows.Count;

                _totals.Text = _last.Title + "   |   " + _last.Subtitle + "   |   " + rows + " row(s)"
                             + (string.IsNullOrWhiteSpace(_last.Totals) ? string.Empty : "   |   " + _last.Totals);

                Say(_selected.Title + " ran for " + Parameters().GetRangeText() + ".");
                return true;
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not run that report.", error);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
                _run.Enabled = true;
            }
        }

        private void Export()
        {
            if (_last == null && !Run()) return;
            if (_last == null) return;

            try
            {
                string file = Reports.ExportCsv(_last, AppConfig.ApplicationFolder);

                Dialog.Info(this, "The report was written to:" + Environment.NewLine + Environment.NewLine
                    + file + Environment.NewLine + Environment.NewLine
                    + "The file opens in Excel or any spreadsheet.", "Report exported");

                Say("Exported to " + file);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not export the report.", error);
            }
        }

        private void PrintPlain()
        {
            if (_last == null && !Run()) return;
            if (_last == null) return;

            try
            {
                ReportPrinter printer = new ReportPrinter(_last, Session == null ? "-" : Session.Username);

                printer.Print(true, this);
                Say(printer.Describe() + " sent to the printer.");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not print the report.", error);
            }
        }

        /// <summary>
        /// Hands the same rows to Crystal Reports and shows the viewer it
        /// produces. The gateway writes its messages for the clerk: when
        /// Crystal is not there it says so in a sentence and points at the
        /// other two buttons - it never shows an assembly-loading error.
        /// </summary>
        private void PrintWithCrystal()
        {
            if (_last == null && !Run()) return;
            if (_last == null) return;

            object report = null;

            try
            {
                IDictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["From"] = _from.Value.Date;
                parameters["To"] = _to.Value.Date;
                parameters["Purok"] = PurokFrom(_purok);
                parameters["PrintedBy"] = Session == null ? string.Empty : Session.Username;
                parameters["PrintedOn"] = Clock.Now();

                report = CrystalReportGateway.BuildReport(_selected.CrystalTemplate, _last.Table, parameters);

                using (Form viewer = new FramedViewer(
                    CrystalReportGateway.CreateViewer(report), _selected.Title))
                    viewer.ShowDialog(this);

                Say("Crystal Report: " + _selected.Title + ".");
            }
            catch (InvalidOperationException error)
            {
                Dialog.Warn(this, error.Message + Environment.NewLine + Environment.NewLine
                    + "The report itself is complete below, and it can be printed on plain paper or exported "
                    + "as a CSV.", "Crystal Reports is not available here");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not show the Crystal Report.", error);
            }
            finally
            {
                if (report != null) CrystalReportGateway.CloseReport(report);
            }
        }

        /// <summary>
        /// A plain window to hold the Crystal viewer, because the viewer comes
        /// back as a control rather than as a window of its own.
        /// </summary>
        private class FramedViewer : Form
        {
            public FramedViewer(Control viewer, string title)
            {
                Text = title + " - Crystal Reports";
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(1000, 720);
                MinimumSize = new Size(720, 520);
                BackColor = AppTheme.Canvas;
                Font = AppTheme.Body;

                if (viewer == null)
                {
                    Label message = UiFactory.Body("The report viewer could not be created on this computer.");
                    message.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);
                    Controls.Add(message);
                    return;
                }

                viewer.Dock = DockStyle.Fill;
                Controls.Add(viewer);
            }
        }
    }
}
