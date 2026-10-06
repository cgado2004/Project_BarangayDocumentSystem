// ---------------------------------------------------------------------------
//  ShellForm.cs - the main window: the sidebar, the top bar, the pages and
//  the status strip.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;
using BarangayDocumentSystem.UI.Views;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The window everything else opens inside.
    ///
    /// It owns four things and nothing else:
    ///
    ///  1. the sidebar, which is built from the permission list, so a clerk
    ///     never sees the accounts screen and the punong barangay never sees
    ///     the collection screen;
    ///  2. the top bar, which says where you are, who you are, and whether the
    ///     office window is open right now - the 8:00 AM to 4:00 PM rule the
    ///     barangay asked for;
    ///  3. the pages, created the first time they are opened and refreshed
    ///     every time after that;
    ///  4. the idle lock, which asks for the password again when somebody has
    ///     walked away from the counter.
    ///
    /// At startup it also sweeps yesterday's queue: every request filed after
    /// 4:00 PM that needed no checking becomes Cleared, because the window has
    /// opened again. That is the rule in one sentence - file it after four and
    /// it is waiting for you in the morning - and this is where it happens
    /// without anybody pressing anything.
    /// </summary>
    public class ShellForm : Form
    {
        private readonly IBarangayRepository _repository;
        private readonly SessionManager _session;
        private readonly IClock _clock;
        private readonly AuthenticationService _authentication;
        private readonly ActivityLogService _activity;

        private NavigationSidebar _sidebar;
        private Panel _content;
        private StatusStrip _status;
        private ToolStripStatusLabel _statusDatabase;
        private ToolStripStatusLabel _statusWindow;
        private ToolStripStatusLabel _statusMessage;
        private ToolStripStatusLabel _statusUser;

        private readonly Dictionary<string, UserControl> _pages = new Dictionary<string, UserControl>();
        private readonly Timer _idleTimer = new Timer();
        private bool _locking;

        public ShellForm(IBarangayRepository repository, SessionManager session, IClock clock,
                         AuthenticationService authentication)
        {
            _repository = repository;
            _session = session;
            _clock = clock;
            _authentication = authentication;
            _activity = new ActivityLogService(repository, session, clock);

            BuildWindow();
            StartIdleWatch();
            SweepWaitingRequests();
            Open("dashboard");
        }

        // ==================================================================
        //  Building the window
        // ==================================================================

        private void BuildWindow()
        {
            Text = AppConfig.BarangayName + " - Document System";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1180, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;
            KeyPreview = true;

            _status = new StatusStrip();
            _status.BackColor = AppTheme.Surface;
            _status.ForeColor = AppTheme.Muted;
            _status.SizingGrip = false;
            _status.Font = AppTheme.Small;

            _statusDatabase = UiFactory.StatusLabel(string.Empty);
            _statusWindow = UiFactory.StatusLabel(string.Empty);
            _statusMessage = UiFactory.StatusLabel(string.Empty);
            _statusMessage.Spring = true;
            _statusMessage.ForeColor = AppTheme.Primary;
            _statusUser = UiFactory.StatusLabel(string.Empty);

            _status.Items.Add(_statusDatabase);
            _status.Items.Add(new ToolStripSeparator());
            _status.Items.Add(_statusWindow);
            _status.Items.Add(_statusMessage);
            _status.Items.Add(_statusUser);

            _content = new Panel();
            _content.Dock = DockStyle.Fill;
            _content.BackColor = AppTheme.Canvas;

            _sidebar = new NavigationSidebar();
            _sidebar.Destination += delegate (object sender, string key) { Open(key); };

            Panel sidebarHost = new Panel();
            sidebarHost.Dock = DockStyle.Left;
            sidebarHost.Width = AppTheme.SidebarWidth;
            sidebarHost.BackColor = AppTheme.Navy;

            BrandHeader header = new BrandHeader();
            sidebarHost.Controls.Add(_sidebar);
            sidebarHost.Controls.Add(header);
            header.BringToFront();

            Controls.Add(_content);
            Controls.Add(sidebarHost);
            Controls.Add(_status);

            _sidebar.Bind(_session, Destinations());
            UpdateStatusStrip();
        }

        /// <summary>
        /// The destinations, each with the permission it needs.
        ///
        /// Writing the permission next to the destination - rather than inside
        /// the sidebar's drawing code - is what keeps this list honest. Adding
        /// a screen for the clerk only means adding one line here.
        /// </summary>
        private static IEnumerable<NavigationItem> Destinations()
        {
            List<NavigationItem> items = new List<NavigationItem>();

            items.Add(new NavigationItem("dashboard", "Dashboard", "Everyday", Permission.ViewRequests));
            items.Add(new NavigationItem("residents", "Residents", "Everyday", Permission.ViewResidents));
            items.Add(new NavigationItem("requests", "Requests", "Everyday", Permission.ViewRequests));

            items.Add(new NavigationItem("receipts", "Official Receipts", "Money", Permission.CollectPayments));
            items.Add(new NavigationItem("census", "Census", "Records", Permission.ViewCensus));
            items.Add(new NavigationItem("reports", "Reports", "Records", Permission.ViewReports));
            items.Add(new NavigationItem("activity", "Activity Log", "Records", Permission.ViewActivityLog));

            items.Add(new NavigationItem("users", "User Accounts", "System", Permission.ManageUsers));
            items.Add(new NavigationItem("about", "About", "System", Permission.ViewRequests));

            return items;
        }

        // ==================================================================
        //  Moving between the pages
        // ==================================================================

        private void Open(string key)
        {
            UserControl page;

            if (!_pages.TryGetValue(key, out page))
            {
                page = CreatePage(key);
                if (page == null) return;

                _pages[key] = page;
                _content.Controls.Add(page);
            }

            foreach (Control control in _content.Controls) control.Visible = control == page;

            ViewBase view = page as ViewBase;
            if (view != null) view.RefreshData();

            _sidebar.Select(key);
            UpdateStatusStrip();
        }

        private UserControl CreatePage(string key)
        {
            switch (key)
            {
                case "dashboard": return new DashboardView(_repository, _session, _clock);
                case "residents": return new ResidentsView(_repository, _session, _clock);
                case "requests": return new RequestsView(_repository, _session, _clock);
                case "receipts": return new ReceiptsView(_repository, _session, _clock);
                case "census": return new CensusView(_repository, _session, _clock);
                case "reports": return new ReportsView(_repository, _session, _clock);
                case "activity": return new ActivityLogView(_repository, _session, _clock);
                case "users": return new UsersView(_repository, _session, _clock);
                case "about": return new AboutView(_repository, _session, _clock);
                default: return null;
            }
        }

        /// <summary>Lets a screen send the person somewhere else - the census
        /// screen sends them to the reports, for instance.</summary>
        public void GoTo(string key)
        {
            Open(key);
        }

        // ==================================================================
        //  The status strip
        // ==================================================================

        public void UpdateStatusStrip()
        {
            _statusDatabase.Text = _repository.Describe();

            TimeWindowPolicy window = new TimeWindowPolicy(_clock);
            bool inside = window.IsInsideOfficeWindow(_clock.Now());

            _statusWindow.Text = "Office window " + window.GetWindowText()
                               + (inside ? " - documents needing no check are cleared now"
                                         : " - closed, so such requests wait as Pending");
            _statusWindow.ForeColor = inside ? AppTheme.Success : AppTheme.Warning;

            _statusUser.Text = _session.DisplayName + " (" + EnumText.Of(_session.Role) + ")";
        }

        public void Say(string message)
        {
            _statusMessage.Text = message;
        }

        // ==================================================================
        //  The overnight queue
        // ==================================================================

        /// <summary>
        /// Clears the requests that were filed after 4:00 PM, now that the
        /// window has opened again.
        ///
        /// This runs once, when the shell is built, and it only ever touches
        /// requests that needed no checking - a business clearance still goes
        /// through the queue like everything else that needs a person.
        /// </summary>
        private void SweepWaitingRequests()
        {
            try
            {
                RequestService requests = new RequestService(_repository, _activity, _session, _clock,
                    new FeeSchedule(), new TimeWindowPolicy(_clock));

                int cleared = requests.ClearWaitingRequests();

                Say(cleared > 0
                    ? cleared + " request(s) filed outside office hours were cleared now that the window "
                      + "is open."
                    : "Ready. Nothing was waiting from outside office hours.");
            }
            catch (Exception error)
            {
                AppLog.Error("The overnight queue could not be cleared.", error);
                Say("The overnight queue could not be checked - the details are in the log file.");
            }
        }

        // ==================================================================
        //  The idle lock
        // ==================================================================

        private void StartIdleWatch()
        {
            if (AppConfig.SessionTimeoutMinutes <= 0) return;

            // Twice a minute is plenty, and it costs nothing on the office
            // computer while the clerk is out at the counter.
            _idleTimer.Interval = 30000;
            _idleTimer.Tick += delegate (object sender, EventArgs e)
            {
                if (_locking) return;
                if (!_session.IsExpired) return;

                Lock();
            };
            _idleTimer.Start();

            MouseMove += delegate (object sender, MouseEventArgs e) { _session.Touch(); };
            KeyDown += delegate (object sender, KeyEventArgs e) { _session.Touch(); };
            MouseClick += delegate (object sender, MouseEventArgs e) { _session.Touch(); };
        }

        /// <summary>
        /// Locks the screen and asks for the password again.
        ///
        /// It shows the real sign-in window rather than a quick box of its own,
        /// for a simple reason: the clerk should never have to wonder whether
        /// the window asking for a password is really part of the program.
        /// </summary>
        private void Lock()
        {
            _locking = true;
            _idleTimer.Stop();

            try
            {
                _activity.Record(ActivityModule.Security, "Screen locked", "Session", _session.Username,
                    "The screen was locked after " + _session.IdleMinutes + " minutes without activity.");

                using (LoginForm login = new LoginForm(_authentication, _repository))
                {
                    login.Text = "The screen was locked - sign in again";

                    if (login.ShowDialog(this) != DialogResult.OK)
                    {
                        _session.SignOut();
                        Application.Restart();
                        return;
                    }

                    // It has to be the same person: a locked screen is not an
                    // invitation for whoever walks past the counter.
                    if (login.SignedInUser != null && login.SignedInUser.UserId != _session.User.UserId)
                    {
                        _activity.Record(ActivityModule.Security, "Different person signed in", "Session",
                            login.SignedInUser.Username,
                            "A different account was used to unlock the screen, so the session changed.");
                        _session.SignIn(login.SignedInUser);
                    }

                    UpdateStatusStrip();
                    Say("Welcome back, " + _session.DisplayName + ".");
                }
            }
            catch (Exception error)
            {
                AppLog.Error("The screen lock ran into trouble.", error);
            }
            finally
            {
                _locking = false;
                _session.Touch();
                _idleTimer.Start();
            }
        }

        // ==================================================================
        //  Leaving
        // ==================================================================

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _idleTimer.Stop();

            if (_session.IsSignedIn && e.CloseReason == CloseReason.UserClosing)
            {
                bool leave = Dialog.Confirm(this, "Sign out of " + _session.DisplayName
                    + "'s account and close the system?", "Sign out", "Sign out");

                if (!leave) { e.Cancel = true; return; }

                try
                {
                    _authentication.SignOut(_session.User);
                }
                catch (Exception error)
                {
                    AppLog.Warn("The sign-out could not be written to the log: " + error.Message);
                }
            }

            AppTheme.Release();
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// The last page in the sidebar: what this program is, which version, where
    /// the settings live, and who to ask. I put it in because the first
    /// question at any barangay is "where does the thing I am looking at come
    /// from?" - and the answer should not need me in the room.
    /// </summary>
    public class AboutView : ViewBase
    {
        public AboutView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("About this system",
                "What it is, where its settings live, and how the money side is worked out.");

            SectionPanel about = Card("Barangay Document System",
                "Version 4.0 - the revamped build, in C# on .NET Framework.");
            about.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 60);
            about.Size = new Size(880, 300);
            about.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            Label text = UiFactory.Body(string.Empty);
            text.Location = new Point(AppTheme.Gap4, 58);
            text.Size = new Size(840, 230);
            text.Text =
                "Where the settings live:" + Environment.NewLine
                + "   " + AppConfig.SettingsFile
                + Environment.NewLine + Environment.NewLine
                + "Where the log lives:" + Environment.NewLine
                + "   " + AppConfig.LogFolder
                + Environment.NewLine + Environment.NewLine
                + "Storage in use:" + Environment.NewLine
                + "   " + Repository.Describe()
                + Environment.NewLine + Environment.NewLine
                + "Crystal Reports:" + Environment.NewLine
                + "   " + (AppConfig.UseCrystalReports
                    ? "switched on; the layout files come from " + AppConfig.CrystalReportsFolder
                    : "switched off in the settings file")
                + Environment.NewLine + Environment.NewLine
                + "The office window:" + Environment.NewLine
                + "   " + Window.GetCutOffText();

            about.Controls.Add(text);
            page.Controls.Add(about);
        }
    }
}
