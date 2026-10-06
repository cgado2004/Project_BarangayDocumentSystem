// ---------------------------------------------------------------------------
//  CensusView.cs - the barangay's counting screen.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.Services.Reports;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The census.
    ///
    /// The barangay asked for this screen, and this is what it needs to say:
    /// how many people, how many households, how many in each purok, in each age
    /// group, and in each of the groups the barangay serves - senior citizens,
    /// persons with disability, indigent households, solo parents, 4Ps
    /// beneficiaries and students.
    ///
    /// Two decisions carry through the whole screen. Only ACTIVE residents are
    /// counted, which is the practical reason the deactivate switch exists: a
    /// family that moved to another city must not still be counted next year,
    /// but their record must still be there for the certificate somebody was
    /// issued. And the household size counts dependents, because the barangay
    /// counts people rather than registry rows.
    /// </summary>
    public class CensusView : ViewBase
    {
        private ComboBox _purok;
        private Label _headline;
        private FlowLayoutPanel _cards;
        private DataGridView _byPurok;
        private DataGridView _byAge;
        private DataGridView _summary;
        private DataGridView _households;
        private Label _householdNote;

        public CensusView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Census",
                "Active residents only, counted with the dependents registered under them. Change the purok "
                + "to see one part of the barangay at a time.");

            _purok = UiFactory.DropDown(null, true);
            FillPuroks(_purok, "Whole barangay");
            _purok.Width = 220;
            _purok.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 66);
            _purok.SelectedIndexChanged += delegate (object sender, EventArgs e) { RefreshData(); };

            Button recount = UiFactory.SecondaryButton("Count again");
            recount.Location = new Point(AppTheme.PageMargin + 232, AppTheme.PageMargin + 66);
            recount.Click += delegate (object sender, EventArgs e) { RefreshData(); };

            Button export = UiFactory.SecondaryButton("Export the census (CSV)");
            export.Location = new Point(AppTheme.PageMargin + 374, AppTheme.PageMargin + 66);
            export.Click += delegate (object sender, EventArgs e) { Export(); };

            Button print = UiFactory.GhostButton("Print the census");
            print.Location = new Point(AppTheme.PageMargin + 566, AppTheme.PageMargin + 66);
            print.Click += delegate (object sender, EventArgs e) { Print(); };

            _headline = UiFactory.Body(string.Empty);
            _headline.Font = AppTheme.SmallBold;
            _headline.ForeColor = AppTheme.Primary;
            _headline.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 110);
            _headline.AutoSize = true;

            _cards = new FlowLayoutPanel();
            _cards.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 140);
            _cards.Size = new Size(1160, 130);
            _cards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _cards.WrapContents = true;

            SectionPanel purokCard = Card("People by purok", "Where the barangay's people live.");
            purokCard.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 286);
            purokCard.Size = new Size(570, 240);
            purokCard.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _byPurok = UiFactory.Grid();
            _byPurok.Location = new Point(AppTheme.Gap4, 58);
            _byPurok.Size = new Size(538, 166);
            _byPurok.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            purokCard.Controls.Add(_byPurok);

            SectionPanel ageCard = Card("People by age group",
                "The brackets come from the resident record, so the sheet and the screen agree.");
            ageCard.Location = new Point(AppTheme.PageMargin + 590, AppTheme.PageMargin + 286);
            ageCard.Size = new Size(570, 240);
            ageCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _byAge = UiFactory.Grid();
            _byAge.Location = new Point(AppTheme.Gap4, 58);
            _byAge.Size = new Size(538, 166);
            _byAge.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            ageCard.Controls.Add(_byAge);

            SectionPanel summaryCard = Card("The groups the barangay serves",
                "Senior citizens, persons with disability, indigent households, solo parents, 4Ps "
                + "beneficiaries, students, voters and business owners.");
            summaryCard.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 542);
            summaryCard.Size = new Size(570, 220);
            summaryCard.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _summary = UiFactory.Grid();
            _summary.Location = new Point(AppTheme.Gap4, 58);
            _summary.Size = new Size(538, 146);
            _summary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            summaryCard.Controls.Add(_summary);

            SectionPanel householdCard = Card("Households",
                "One row per head of the family, with everyone living in that house.");
            householdCard.Location = new Point(AppTheme.PageMargin + 590, AppTheme.PageMargin + 542);
            householdCard.Size = new Size(570, 220);
            householdCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _households = UiFactory.Grid();
            _households.Location = new Point(AppTheme.Gap4, 58);
            _households.Size = new Size(538, 146);
            _households.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            householdCard.Controls.Add(_households);

            _householdNote = UiFactory.Hint(string.Empty);
            _householdNote.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 776);
            _householdNote.Size = new Size(1160, 40);
            _householdNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            page.Controls.Add(_purok);
            page.Controls.Add(recount);
            page.Controls.Add(export);
            page.Controls.Add(print);
            page.Controls.Add(_headline);
            page.Controls.Add(_cards);
            page.Controls.Add(purokCard);
            page.Controls.Add(ageCard);
            page.Controls.Add(summaryCard);
            page.Controls.Add(householdCard);
            page.Controls.Add(_householdNote);
        }

        public override void RefreshData()
        {
            try
            {
                string purok = PurokFrom(_purok);

                CensusSnapshot snapshot = Census.GetSnapshot(purok);

                _headline.Text = Census.Describe(purok, snapshot);

                BuildCards(snapshot);

                _byPurok.DataSource = Census.GetPopulationByPurok(true);
                _byPurok.AutoGenerateColumns = true;

                _byAge.DataSource = Census.GetPopulationByAge(purok, true);
                _byAge.AutoGenerateColumns = true;

                _summary.DataSource = Census.GetSummary(purok);
                _summary.AutoGenerateColumns = true;

                _households.DataSource = Census.GetHouseholds(purok);
                _households.AutoGenerateColumns = true;

                _householdNote.Text = "Average household size: "
                    + snapshot.AverageHouseholdSize.ToString("0.00") + " person(s) per house. "
                    + "Every dependents' list is kept on the head of the family's record, on the Residents "
                    + "screen - a dependent is counted here but does not file requests of their own.";

                Say(_headline.Text);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not count the census.", error);
            }
        }

        private void BuildCards(CensusSnapshot snapshot)
        {
            _cards.Controls.Clear();

            _cards.Controls.Add(new SummaryCard("Population", snapshot.TotalPopulation.ToString("#,##0"),
                snapshot.ActiveResidents.ToString("#,##0") + " residents + "
                + snapshot.Dependents.ToString("#,##0") + " dependents", AppTheme.Primary));

            _cards.Controls.Add(new SummaryCard("Households", snapshot.Households.ToString("#,##0"),
                "Average size " + snapshot.AverageHouseholdSize.ToString("0.00"), AppTheme.Primary));

            _cards.Controls.Add(new SummaryCard("Senior citizens", snapshot.Seniors.ToString("#,##0"),
                "Also " + snapshot.PersonsWithDisability.ToString("#,##0") + " person(s) with disability",
                AppTheme.Gold));

            _cards.Controls.Add(new SummaryCard("Students", snapshot.Students.ToString("#,##0"),
                "Student fee category - not a classification", AppTheme.Gold));

            _cards.Controls.Add(new SummaryCard("Waiver holders",
                (snapshot.Indigent + snapshot.SoloParents + snapshot.FourPsBeneficiaries).ToString("#,##0"),
                "Indigent " + snapshot.Indigent + "   solo parent " + snapshot.SoloParents
                + "   4Ps " + snapshot.FourPsBeneficiaries, AppTheme.Success));

            _cards.Controls.Add(new SummaryCard("Not counted",
                (snapshot.InactiveResidents + snapshot.ArchivedRecords).ToString("#,##0"),
                "Inactive " + snapshot.InactiveResidents + "   archived " + snapshot.ArchivedRecords,
                AppTheme.Muted));
        }

        private void Export()
        {
            try
            {
                ReportParameters parameters = new ReportParameters();
                parameters.Purok = PurokFrom(_purok);

                ReportResult result = Reports.Run(ReportDefinitions.CensusSummary, parameters);
                string file = Reports.ExportCsv(result, BarangayDocumentSystem.Config.AppConfig.ApplicationFolder);

                Dialog.Info(this, "The census was written to:" + Environment.NewLine + Environment.NewLine
                    + file + Environment.NewLine + Environment.NewLine
                    + "The file opens in Excel or any spreadsheet.", "Census exported");

                Say("Census exported to " + file);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not export the census.", error);
            }
        }

        /// <summary>Prints the census on plain paper, whether or not Crystal
        /// Reports is installed. The rows are the ones on screen, so nobody has
        /// to check that two reports agree.</summary>
        private void Print()
        {
            try
            {
                ReportParameters parameters = new ReportParameters();
                parameters.Purok = PurokFrom(_purok);

                ReportResult result = Reports.Run(ReportDefinitions.CensusSummary, parameters);
                ReportPrinter printer = new ReportPrinter(result, Session == null ? "-" : Session.Username);

                printer.Print(true, this);
                Say(printer.Describe() + " sent to the printer.");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not print the census.", error);
            }
        }
    }
}
