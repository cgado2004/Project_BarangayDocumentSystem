// ---------------------------------------------------------------------------
//  ViewBase.cs - what every screen in the program starts from.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.Services.Reports;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The shared starting point of every screen.
    ///
    /// Each screen is handed the store and the services it needs through its
    /// constructor - never by reaching for a global. That is the one design
    /// decision behind most of the good in this program: it is why the same
    /// screens can run against the in-memory store while I test the rules, why
    /// no screen ever names a database class, and why swapping MySQL for SQL
    /// Server touched no screen at all.
    ///
    /// The base class also owns the two things every screen must do the same
    /// way: laying itself out on the 8-point grid, and saying out loud what it
    /// just did. A screen that silently changes a record is a screen nobody
    /// trusts.
    /// </summary>
    public abstract class ViewBase : UserControl
    {
        protected IBarangayRepository Repository { get; private set; }
        protected SessionManager Session { get; private set; }
        protected IClock Clock { get; private set; }

        protected ResidentService Residents { get; private set; }
        protected RequestService Requests { get; private set; }
        protected ReceiptService Receipts { get; private set; }
        protected CensusService Census { get; private set; }
        protected UserService Users { get; private set; }
        protected ActivityLogService Activity { get; private set; }
        protected IReportService Reports { get; private set; }
        protected FeeSchedule Fees { get; private set; }
        protected TimeWindowPolicy Window { get; private set; }

        /// <summary>The line at the bottom of the screen that says what just
        /// happened. Set by the actions, not by the layout.</summary>
        protected Label StatusLine { get; private set; }

        public event EventHandler<string> StatusChanged;

        protected ViewBase(IBarangayRepository repository, SessionManager session, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            Repository = repository;
            Session = session;
            Clock = clock == null ? new SystemClock() : clock;

            Activity = new ActivityLogService(repository, session, Clock);
            Fees = new FeeSchedule();
            Window = new TimeWindowPolicy(Clock);
            Residents = new ResidentService(repository, Activity, session, Clock);
            Requests = new RequestService(repository, Activity, session, Clock, Fees, Window);
            Receipts = new ReceiptService(repository, Activity, session, Clock);
            Census = new CensusService(repository);
            Users = new UserService(repository, Activity, session, Clock);
            Reports = new ReportService(repository, Activity, Clock,
                session == null ? string.Empty : session.Username);

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;
        }

        /// <summary>Builds the screen. Called once, by the shell, the first time
        /// the destination is opened.</summary>
        public abstract void Build();

        /// <summary>Redraws the data. Called every time the destination is
        /// opened, so a screen never shows yesterday's list.</summary>
        public virtual void RefreshData()
        {
        }

        // ==================================================================
        //  Shared pieces of layout
        // ==================================================================

        /// <summary>The scrollable page every view draws on, with the heading
        /// and the sentence under it already placed.</summary>
        protected Panel BuildPage(string heading, string caption)
        {
            Panel page = UiFactory.Page();

            Label title = UiFactory.PageHeading(heading);
            title.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label subtitle = UiFactory.Caption(caption);
            subtitle.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            subtitle.Size = new Size(1160, 34);
            subtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            page.Controls.Add(subtitle);
            page.Controls.Add(title);

            StatusLine = new Label();
            StatusLine.Dock = DockStyle.Bottom;
            StatusLine.Height = 26;
            StatusLine.Font = AppTheme.Small;
            StatusLine.ForeColor = AppTheme.Muted;
            StatusLine.Padding = new Padding(AppTheme.PageMargin, 0, AppTheme.PageMargin, 0);
            StatusLine.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(StatusLine);

            Controls.Add(page);
            return page;
        }

        /// <summary>Writes a sentence into the status line and tells the shell
        /// about it, so a save is never silent.</summary>
        protected void Say(string message)
        {
            if (StatusLine != null) StatusLine.Text = message;
            if (StatusChanged != null) StatusChanged(this, message);
        }

        /// <summary>The usual shape of a successful action: say what happened,
        /// then redraw the data so the screen agrees with the store.</summary>
        protected void Announce(string message)
        {
            Say(message);
            RefreshData();
        }

        /// <summary>A titled card, on the grid, sized by the caller.</summary>
        protected static Controls.SectionPanel Card(string title, string caption)
        {
            return new Controls.SectionPanel(title, caption);
        }

        // ==================================================================
        //  Small shared behaviour
        // ==================================================================

        /// <summary>Fills a drop-down with the real puroks of the barangay, with
        /// a first entry that means "all of them" on a filter or "choose one" on
        /// a form.</summary>
        protected static void FillPuroks(ComboBox box, string firstEntry)
        {
            box.Items.Clear();
            if (firstEntry != null) box.Items.Add(firstEntry);

            foreach (string purok in PurokList.All) box.Items.Add(purok);
            box.SelectedIndex = 0;
        }

        /// <summary>Fills a drop-down with every document that has wording
        /// written for it, so a document can never be chosen and then not
        /// printed.</summary>
        protected static void FillDocuments(ComboBox box, string firstEntry)
        {
            box.Items.Clear();
            if (firstEntry != null) box.Items.Add(firstEntry);

            foreach (string name in Services.Documents.DocumentTemplateRegistry.Default.GetDocumentNames())
                box.Items.Add(name);

            box.SelectedIndex = 0;
        }

        /// <summary>Turns the words in a purok drop-down back into a filter
        /// value: the "all" entry becomes an empty filter.</summary>
        protected static string PurokFrom(ComboBox box)
        {
            string text = Convert.ToString(box.SelectedItem);
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (text.StartsWith("All", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("Any", StringComparison.OrdinalIgnoreCase)) return string.Empty;

            return text;
        }

        /// <summary>Reads a document type back out of a drop-down by matching
        /// the name the drop-down showed.</summary>
        protected static DocumentType? DocumentFrom(ComboBox box)
        {
            string text = Convert.ToString(box.SelectedItem);
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("All", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("Any", StringComparison.OrdinalIgnoreCase)) return null;

            foreach (DocumentType type in (DocumentType[])Enum.GetValues(typeof(DocumentType)))
                if (string.Equals(EnumText.Spaced(type.ToString()), text, StringComparison.OrdinalIgnoreCase))
                    return type;

            return null;
        }

        /// <summary>Sets the two date boxes from a "today / yesterday / this
        /// week / this month" choice. The per-day filter the barangay asked for
        /// is built on this, once, so every screen understands a period the same
        /// way.</summary>
        protected void ApplyQuickRange(int choice, DateTimePicker from, DateTimePicker to)
        {
            DateTime today = Clock.Now().Date;

            switch (choice)
            {
                case 0: from.Value = today; to.Value = today; break;                       // today
                case 1: from.Value = today.AddDays(-1); to.Value = today.AddDays(-1); break; // yesterday
                case 2: from.Value = today.AddDays(-(int)today.DayOfWeek); to.Value = today; break;
                case 3: from.Value = new DateTime(today.Year, today.Month, 1); to.Value = today; break;
                case 4: from.Value = today.AddDays(-30); to.Value = today; break;
                case 5: break;                                                              // custom
                default: from.Value = new DateTime(today.Year, 1, 1); to.Value = today; break;
            }
        }

        /// <summary>The heading every list of dates sits under, so the clerk
        /// always sees which period the numbers below belong to.</summary>
        protected static string DescribeRange(DateTime from, DateTime to)
        {
            if (from.Date == to.Date) return from.ToString("dd MMMM yyyy");
            return from.ToString("dd MMM yyyy") + " to " + to.ToString("dd MMM yyyy");
        }

        /// <summary>Money, written the way it is written on the receipt.</summary>
        protected static string Money(decimal amount)
        {
            return "P" + amount.ToString("#,##0.00");
        }
    }
}
