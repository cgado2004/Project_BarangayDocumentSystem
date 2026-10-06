// ---------------------------------------------------------------------------
//  DashboardView.cs - what the barangay looks like today.
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
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The first screen after signing in.
    ///
    /// It answers the questions the barangay actually asks at eight in the
    /// morning: how many people are on the active list, how many requests are
    /// waiting and how long the oldest has been waiting, what was collected
    /// today, and which purok has the most people.
    ///
    /// Everything on it comes from one call to the store, so opening the
    /// program stays quick on the office computer - and the date line at the
    /// top always says exactly what "today" means, which matters when the
    /// screen is open past midnight.
    /// </summary>
    public class DashboardView : ViewBase
    {
        private FlowLayoutPanel _cards;
        private DataGridView _queue;
        private DataGridView _byPurok;
        private Label _headline;
        private Label _windowNote;
        private Label _reminders;

        public DashboardView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Dashboard",
                "Counted from the records on file. It refreshes every time you open it, and after every "
                + "change you make.");

            _headline = UiFactory.Body(string.Empty);
            _headline.Font = AppTheme.Small;
            _headline.ForeColor = AppTheme.Muted;
            _headline.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 62);
            _headline.AutoSize = true;

            _windowNote = UiFactory.Body(string.Empty);
            _windowNote.Font = AppTheme.SmallBold;
            _windowNote.ForeColor = AppTheme.Primary;
            _windowNote.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 84);
            _windowNote.AutoSize = true;

            _cards = new FlowLayoutPanel();
            _cards.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 118);
            _cards.Size = new Size(1140, 130);
            _cards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _cards.WrapContents = true;

            SectionPanel queueCard = Card("Waiting on the counter",
                "Requests that still need something done to them, longest wait first.");
            queueCard.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 264);
            queueCard.Size = new Size(560, 320);
            queueCard.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _queue = UiFactory.Grid();
            _queue.Location = new Point(AppTheme.Gap4, 58);
            _queue.Size = new Size(528, 246);
            _queue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _queue.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) GoTo("requests");
            };
            queueCard.Controls.Add(_queue);

            SectionPanel purokCard = Card("People by purok",
                "Active residents only - the same numbers the census screen uses.");
            purokCard.Location = new Point(AppTheme.PageMargin + 580, AppTheme.PageMargin + 264);
            purokCard.Size = new Size(560, 320);
            purokCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _byPurok = UiFactory.Grid();
            _byPurok.Location = new Point(AppTheme.Gap4, 58);
            _byPurok.Size = new Size(528, 246);
            _byPurok.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            purokCard.Controls.Add(_byPurok);

            _reminders = UiFactory.Hint(string.Empty);
            _reminders.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 600);
            _reminders.Size = new Size(1140, 60);
            _reminders.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            page.Controls.Add(_headline);
            page.Controls.Add(_windowNote);
            page.Controls.Add(_cards);
            page.Controls.Add(queueCard);
            page.Controls.Add(purokCard);
            page.Controls.Add(_reminders);
        }

        public override void RefreshData()
        {
            try
            {
                DateTime today = Clock.Now().Date;

                DataTable summary = Repository.GetDashboardSummary(today, today);
                Dictionary<string, decimal> numbers = new Dictionary<string, decimal>();

                foreach (DataRow row in summary.Rows)
                    numbers[Convert.ToString(row["metric"])] = Convert.ToDecimal(row["value"]);

                BuildCards(numbers);

                _headline.Text = "As of " + Clock.Now().ToString("dddd, dd MMMM yyyy, h:mm tt")
                               + "   |   " + Money(Get(numbers, "Collected today")) + " collected today"
                               + "   |   " + Get(numbers, "Requests filed today").ToString("#,##0")
                               + " request(s) filed today";

                _windowNote.Text = Window.GetCutOffText();

                LoadQueue();
                LoadBreakdown();
                LoadReminders(numbers, today);

                Say(Get(numbers, "Active residents").ToString("#,##0") + " active resident(s), "
                    + Get(numbers, "Requests waiting").ToString("#,##0") + " request(s) waiting.");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not load the dashboard.", error);
            }
        }

        private void BuildCards(Dictionary<string, decimal> numbers)
        {
            _cards.Controls.Clear();

            _cards.Controls.Add(new SummaryCard("Active residents",
                Get(numbers, "Active residents").ToString("#,##0"),
                "Inactive " + Get(numbers, "Inactive residents").ToString("#,##0")
                + "   archived " + Get(numbers, "Archived records").ToString("#,##0"),
                AppTheme.Primary));

            _cards.Controls.Add(new SummaryCard("Households",
                Get(numbers, "Households").ToString("#,##0"),
                "Dependents on file: " + Get(numbers, "Dependents on file").ToString("#,##0"),
                AppTheme.Primary));

            _cards.Controls.Add(new SummaryCard("Waiting",
                Get(numbers, "Requests waiting").ToString("#,##0"),
                "Being processed " + Get(numbers, "Requests being processed").ToString("#,##0")
                + "   cleared " + Get(numbers, "Cleared requests").ToString("#,##0"),
                AppTheme.Gold));

            _cards.Controls.Add(new SummaryCard("Ready for release",
                Get(numbers, "Ready for release").ToString("#,##0"),
                "Released today: " + Get(numbers, "Released today").ToString("#,##0"),
                AppTheme.Success));

            _cards.Controls.Add(new SummaryCard("Collected today",
                Money(Get(numbers, "Collected today")),
                "Given free today: " + Get(numbers, "Issued free in the period").ToString("#,##0"),
                AppTheme.Success));

            _cards.Controls.Add(new SummaryCard("Actions logged today",
                Get(numbers, "Activity entries today").ToString("#,##0"),
                "Every change is written to the activity log", AppTheme.Muted));
        }

        /// <summary>
        /// The queue: everything not finished, oldest first.
        ///
        /// Oldest first matters. A stack of paper hides the request that has
        /// been waiting longest at the bottom; a list cannot, and the number in
        /// the "waiting" column is deliberately shown in days rather than as a
        /// date, because "six days" is the thing somebody acts on.
        /// </summary>
        private void LoadQueue()
        {
            RequestQuery query = new RequestQuery();
            IList<DocumentRequest> requests = Repository.GetRequests(query);

            DataTable table = new DataTable("queue");
            table.Columns.Add("reference", typeof(string));
            table.Columns.Add("resident", typeof(string));
            table.Columns.Add("document", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("waiting", typeof(string));

            List<DocumentRequest> open = new List<DocumentRequest>();

            foreach (DocumentRequest request in requests)
                if (!request.IsClosed) open.Add(request);

            open.Sort(delegate (DocumentRequest left, DocumentRequest right)
            {
                return left.DateRequested.CompareTo(right.DateRequested);
            });

            int shown = 0;

            foreach (DocumentRequest request in open)
            {
                if (shown++ >= 25) break;

                DataRow row = table.NewRow();
                row["reference"] = request.ReferenceNumber;
                row["resident"] = request.ResidentName;
                row["document"] = request.GetDocumentName();
                row["status"] = request.GetStatusText();
                row["waiting"] = request.GetWaitingDays(DateTime.Today) + " day(s)";
                table.Rows.Add(row);
            }

            _queue.DataSource = table;
            _queue.AutoGenerateColumns = true;

            if (_queue.Columns.Contains("document")) _queue.Columns["document"].FillWeight = 130;
            if (_queue.Columns.Contains("waiting")) _queue.Columns["waiting"].FillWeight = 55;

            UiFactory.ColourStatusColumn(_queue, "status");
        }

        private void LoadBreakdown()
        {
            DataTable table = Repository.GetPopulationByPurok(true);

            _byPurok.DataSource = table;
            _byPurok.AutoGenerateColumns = true;

            if (_byPurok.Columns.Contains("residents")) _byPurok.Columns["residents"].FillWeight = 40;
            if (_byPurok.Columns.Contains("households")) _byPurok.Columns["households"].FillWeight = 50;
        }

        /// <summary>The two or three things worth saying on a dashboard: what
        /// the office clock is doing to today's filings, and anything that has
        /// been waiting too long.</summary>
        private void LoadReminders(Dictionary<string, decimal> numbers, DateTime today)
        {
            List<string> lines = new List<string>();

            DateTime now = Clock.Now();
            bool inside = Window.IsInsideOfficeWindow(now);

            lines.Add(inside
                ? "The window is open (" + Window.GetWindowText() + "), so a request that needs no checking is "
                  + "cleared the moment it is filed."
                : "The window is closed. A request that needs no checking is filed as Pending and is cleared "
                  + "automatically at " + Window.NextWindowOpening().ToString("dd MMMM, h:mm tt") + ".");

            if (Get(numbers, "Requests waiting") > 0)
                lines.Add(Get(numbers, "Requests waiting").ToString("#,##0")
                    + " request(s) are in the queue. The oldest are at the top of the list on the right.");

            if (Get(numbers, "Ready for release") > 0)
                lines.Add(Get(numbers, "Ready for release").ToString("#,##0")
                    + " document(s) are prepared and waiting to be handed over.");

            if (Get(numbers, "Inactive residents") + Get(numbers, "Archived records") > 0)
                lines.Add(Get(numbers, "Inactive residents").ToString("#,##0") + " inactive and "
                    + Get(numbers, "Archived records").ToString("#,##0") + " archived record(s) are kept for "
                    + "the history of documents already issued - that is why they are not deleted.");

            _reminders.Text = string.Join(Environment.NewLine, lines.ToArray());
        }

        private static decimal Get(Dictionary<string, decimal> numbers, string key)
        {
            decimal value;
            return numbers.TryGetValue(key, out value) ? value : 0m;
        }

        private void GoTo(string key)
        {
            Forms.ShellForm shell = FindForm() as Forms.ShellForm;
            if (shell != null) shell.GoTo(key);
        }
    }
}
