// ---------------------------------------------------------------------------
//  AdminViews.cs - the screens that look after the system itself: the staff
//  accounts, the activity log and the official receipts.
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
using BarangayDocumentSystem.UI.Dialogs;
using BarangayDocumentSystem.UI.Forms;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The staff accounts.
    ///
    /// This screen is administrator-only, and it is the only place an account
    /// is created - which is the whole answer to "remove Register" in the
    /// review. Sign-up on a public sign-in window would mean anybody who walked
    /// past the counter could read the residents' records, so it is gone and
    /// this is what replaced it.
    ///
    /// An account is never deleted either. Somebody who leaves the barangay
    /// office has their account deactivated: it stops working that minute, and
    /// the activity log keeps saying what they did while they were here.
    /// Deleting the account would leave the log pointing at nobody.
    /// </summary>
    public class UsersView : ViewBase
    {
        private DataGridView _grid;
        private Label _summary;
        private Label _lockNote;
        private List<UserAccount> _loaded = new List<UserAccount>();

        public UsersView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("User Accounts",
                "Staff accounts only. Accounts are created here, by somebody who is signed in, and their "
                + "name is written into the activity log next to it.");

            Button create = UiFactory.PrimaryButton("New account");
            create.Click += delegate (object sender, EventArgs e) { Open(null); };

            Button edit = UiFactory.SecondaryButton("Open the account");
            edit.Click += delegate (object sender, EventArgs e) { Open(Selected()); };

            Button toggle = UiFactory.SecondaryButton("Deactivate / reactivate");
            toggle.Click += delegate (object sender, EventArgs e) { Toggle(); };

            Button reset = UiFactory.SecondaryButton("Give a new password");
            reset.Click += delegate (object sender, EventArgs e) { ResetPassword(); };

            Button unlock = UiFactory.SecondaryButton("Unlock");
            unlock.Click += delegate (object sender, EventArgs e) { Unlock(); };

            Button refresh = UiFactory.GhostButton("Refresh");
            refresh.Click += delegate (object sender, EventArgs e) { RefreshData(); };

            FlowLayoutPanel actions = UiFactory.Row(create, edit, toggle, reset, unlock, refresh);
            actions.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 66);
            actions.WrapContents = true;
            actions.Width = 1160;
            actions.Height = 44;
            actions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _summary = UiFactory.Body(string.Empty);
            _summary.Font = AppTheme.SmallBold;
            _summary.ForeColor = AppTheme.Primary;
            _summary.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 122);
            _summary.AutoSize = true;

            _lockNote = UiFactory.Hint(string.Empty);
            _lockNote.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 146);
            _lockNote.Size = new Size(1160, 44);
            _lockNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 196);
            _grid.Size = new Size(1160, 430);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) Open(Selected());
            };

            page.Controls.Add(actions);
            page.Controls.Add(_summary);
            page.Controls.Add(_lockNote);
            page.Controls.Add(_grid);
        }

        public override void RefreshData()
        {
            try
            {
                _loaded = new List<UserAccount>();
                foreach (UserAccount user in Users.GetUsers()) _loaded.Add(user);

                DataTable table = new DataTable("users");
                table.Columns.Add("username", typeof(string));
                table.Columns.Add("name", typeof(string));
                table.Columns.Add("role", typeof(string));
                table.Columns.Add("position", typeof(string));
                table.Columns.Add("status", typeof(string));
                table.Columns.Add("locked", typeof(string));
                table.Columns.Add("last_login", typeof(string));
                table.Columns.Add("taken_on", typeof(string));

                DateTime now = Clock.Now();
                int active = 0, locked = 0;

                foreach (UserAccount user in _loaded)
                {
                    if (user.IsActive) active++;
                    if (user.IsLockedOut(now)) locked++;

                    DataRow row = table.NewRow();
                    row["username"] = user.Username;
                    row["name"] = user.FullName;
                    row["role"] = EnumText.Of(user.Role);
                    row["position"] = user.Position;
                    row["status"] = user.IsActive ? "Active" : "Inactive";
                    row["locked"] = user.IsLockedOut(now) ? "Locked out" : string.Empty;
                    row["last_login"] = user.LastLoginOn.HasValue
                        ? user.LastLoginOn.Value.ToString("dd MMM yyyy h:mm tt")
                        : "never";
                    row["taken_on"] = user.CreatedOn.ToString("dd MMM yyyy");
                    table.Rows.Add(row);
                }

                _grid.DataSource = table;
                _grid.AutoGenerateColumns = true;

                if (_grid.Columns.Contains("username")) _grid.Columns["username"].FillWeight = 90;
                if (_grid.Columns.Contains("position")) _grid.Columns["position"].FillWeight = 110;
                if (_grid.Columns.Contains("status")) _grid.Columns["status"].FillWeight = 60;
                if (_grid.Columns.Contains("locked")) _grid.Columns["locked"].FillWeight = 70;

                UiFactory.ColourStatusColumn(_grid, "status");
                UiFactory.ColourStatusColumn(_grid, "locked");

                _summary.Text = _loaded.Count + " account(s), " + active + " active."
                              + "   Roles: Administrator, Clerk, Punong Barangay.";

                _lockNote.Text = "The system locks an account after " + AppConfig.MaxFailedLogins
                    + " wrong passwords for " + AppConfig.LockoutMinutes + " minutes, and the screen locks "
                    + "itself after " + AppConfig.SessionTimeoutMinutes + " minutes without activity. "
                    + "An account that leaves keeps its history: deactivate it, never delete it.";

                Say(_summary.Text);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not load the staff accounts.", error);
            }
        }

        private UserAccount Selected()
        {
            if (_grid.CurrentRow == null) return null;

            string username = Convert.ToString(_grid.CurrentRow.Cells["username"].Value);
            foreach (UserAccount user in _loaded)
                if (string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)) return user;

            return null;
        }

        /// <summary>A null account means a new one - that is how the "New
        /// account" button calls this, and it is also how the form knows to ask
        /// for a first password instead of offering to change one.</summary>
        private void Open(UserAccount account)
        {
            if (account == null && _grid.CurrentRow != null && _grid.Focused
                && _grid.CurrentRow.Cells["username"].Value != DBNull.Value)
            {
                // The "open" button was pressed with no account chosen: say so
                // rather than opening the window for a brand new account.
                Dialog.Warn(this, "Please choose an account from the list first, or use New account.",
                    "No account chosen");
                return;
            }

            using (UserForm form = new UserForm(Users, account))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void Toggle()
        {
            UserAccount account = Selected();

            if (account == null)
            {
                Dialog.Warn(this, "Please choose an account from the list first.", "No account chosen");
                return;
            }

            bool active = !account.IsActive;

            string question = active
                ? "Let " + account.FullName + " sign in again?"
                : "Stop " + account.FullName + " from signing in?" + Environment.NewLine + Environment.NewLine
                  + "The account is kept and everything the person did stays in the activity log. Nothing is "
                  + "deleted.";

            if (!Dialog.ConfirmChange(this, question)) return;

            OperationResult result = Users.SetActive(account, active);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Announce(result.Message);
        }

        private void ResetPassword()
        {
            UserAccount account = Selected();

            if (account == null)
            {
                Dialog.Warn(this, "Please choose an account from the list first.", "No account chosen");
                return;
            }

            string password = TextPromptForm.Ask(this, "New password for " + account.FullName,
                "Type the new password. The person will be asked to change it the next time they sign in.",
                string.Empty, false, true);

            if (password == null) return;

            string confirmation = TextPromptForm.Ask(this, "Type it again",
                "Type the same password once more, so a typing mistake does not lock the person out.",
                password, false, true);

            if (confirmation == null) return;

            OperationResult result = Users.ResetPassword(account, password, confirmation);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Announce(result.Message);
        }

        private void Unlock()
        {
            UserAccount account = Selected();

            if (account == null)
            {
                Dialog.Warn(this, "Please choose an account from the list first.", "No account chosen");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Unlock " + account.FullName + "'s account so they can sign "
                + "in again?")) return;

            OperationResult result = Users.Unlock(account);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Announce(result.Message);
        }
    }

    /// <summary>
    /// The activity log.
    ///
    /// Every change in this program writes a line here in the same breath:
    /// filing a request, clearing it, handing it over, collecting a fee, voiding
    /// a receipt, changing a resident's status, adding an account. The point is
    /// that the barangay can answer "who filed this, and who changed it?"
    /// without asking anybody.
    ///
    /// Nothing on this screen can be edited or deleted - not by the clerk, not
    /// by the administrator, not by me. A log that can be edited is not a log;
    /// it is a story. The database gives the program append-only rights on that
    /// table for exactly the same reason.
    /// </summary>
    public class ActivityLogView : ViewBase
    {
        private ComboBox _range;
        private DateTimePicker _from;
        private DateTimePicker _to;
        private ComboBox _user;
        private ComboBox _module;
        private TextBox _keyword;
        private DataGridView _grid;
        private Label _summary;
        private Label _dayCount;

        public ActivityLogView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Activity Log",
                "What was done, by whom, and when. Nothing here can be edited, and the per-day filter shows "
                + "one day at a time - which is what an audit asks for first.");

            _range = UiFactory.DropDown(new string[]
            {
                "Today", "Yesterday", "This week", "This month", "Choose dates", "Everything"
            }, true);
            _range.Width = 150;
            _range.SelectedIndexChanged += delegate (object sender, EventArgs e)
            {
                ApplyQuickRange(_range.SelectedIndex, _from, _to);
                _from.Enabled = _to.Enabled = _range.SelectedIndex == 4;
                LoadLog();
            };

            _from = UiFactory.DatePicker(Clock.Now().Date);
            _from.Width = 150;
            _from.Enabled = false;
            _from.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 4) LoadLog(); };

            _to = UiFactory.DatePicker(Clock.Now().Date);
            _to.Width = 150;
            _to.Enabled = false;
            _to.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 4) LoadLog(); };

            _user = UiFactory.DropDown(null, true);
            _user.Width = 170;
            _user.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadLog(); };

            _module = UiFactory.DropDown(null, true);
            _module.Width = 170;
            _module.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadLog(); };

            _keyword = UiFactory.TextBox("Search the details...", 100);
            _keyword.Width = 220;
            _keyword.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadLog(); }
            };

            Button search = UiFactory.PrimaryButton("Search");
            search.Click += delegate (object sender, EventArgs e) { LoadLog(); };

            Button export = UiFactory.SecondaryButton("Export this period (CSV)");
            export.Click += delegate (object sender, EventArgs e) { Export(); };

            FlowLayoutPanel filters = UiFactory.Row(_range, _from, _to, _user, _module, _keyword, search, export);
            filters.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 66);
            filters.WrapContents = true;
            filters.Width = 1160;
            filters.Height = 44;
            filters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _summary = UiFactory.Body(string.Empty);
            _summary.Font = AppTheme.Small;
            _summary.ForeColor = AppTheme.Muted;
            _summary.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 122);
            _summary.AutoSize = true;

            _dayCount = UiFactory.Body(string.Empty);
            _dayCount.Font = AppTheme.SmallBold;
            _dayCount.ForeColor = AppTheme.Primary;
            _dayCount.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 144);
            _dayCount.AutoSize = true;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 176);
            _grid.Size = new Size(1160, 450);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            page.Controls.Add(filters);
            page.Controls.Add(_summary);
            page.Controls.Add(_dayCount);
            page.Controls.Add(_grid);

            FillUsers();
            _range.SelectedIndex = 0;
        }

        public override void RefreshData()
        {
            try
            {
                LoadLog();
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not read the activity log.", error);
            }
        }

        private void FillUsers()
        {
            _user.Items.Clear();
            _user.Items.Add("Everybody");

            foreach (string username in Activity.GetUsernames()) _user.Items.Add(username);

            _user.SelectedIndex = 0;

            _module.Items.Clear();
            _module.Items.Add("Everything");

            foreach (ActivityModule module in (ActivityModule[])Enum.GetValues(typeof(ActivityModule)))
                _module.Items.Add(EnumText.Spaced(module.ToString()));

            _module.SelectedIndex = 0;
        }

        private ActivityLogQuery Query()
        {
            ActivityLogQuery query = new ActivityLogQuery();
            query.From = _from.Value.Date;
            query.To = _to.Value.Date;
            query.Keyword = _keyword.Text;
            query.MaximumRows = 2000;

            if (_user.SelectedIndex > 0) query.Username = Convert.ToString(_user.SelectedItem);
            if (_module.SelectedIndex > 0) query.Module = (ActivityModule)(_module.SelectedIndex - 1);

            return query;
        }

        private void LoadLog()
        {
            DataTable table = Activity.GetLogTable(Query());

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("log_id")) _grid.Columns["log_id"].Visible = false;
            if (_grid.Columns.Contains("when")) _grid.Columns["when"].FillWeight = 90;
            if (_grid.Columns.Contains("details")) _grid.Columns["details"].FillWeight = 260;
            if (_grid.Columns.Contains("machine")) _grid.Columns["machine"].FillWeight = 70;

            DateTime today = Clock.Now().Date;
            int todayCount = Activity.CountForDay(today);

            _summary.Text = table.Rows.Count + " entr(ies) shown for "
                          + DescribeRange(_from.Value, _to.Value)
                          + ".   The log is a record, not a workspace: there is no edit and no delete here.";

            _dayCount.Text = "Actions today (" + today.ToString("dd MMMM yyyy") + "): " + todayCount
                           + ".   The per-day filter is the quickest way to answer 'what happened that day?'.";

            Say(_summary.Text);
        }

        private void Export()
        {
            try
            {
                ReportParameters parameters = new ReportParameters();
                parameters.From = _from.Value.Date;
                parameters.To = _to.Value.Date;
                parameters.Username = _user.SelectedIndex > 0 ? Convert.ToString(_user.SelectedItem) : string.Empty;
                if (_module.SelectedIndex > 0) parameters.Module = (ActivityModule)(_module.SelectedIndex - 1);

                ReportResult result = Reports.Run(ReportDefinitions.ActivityLog, parameters);
                string file = Reports.ExportCsv(result, AppConfig.ApplicationFolder);

                Dialog.Info(this, "The activity log was written to:" + Environment.NewLine + Environment.NewLine
                    + file, "Activity log exported");

                Say("Activity log exported to " + file);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not export the activity log.", error);
            }
        }
    }

    /// <summary>
    /// The official receipts, and the booklets they come from.
    ///
    /// Two things live here and they belong together: the register of every
    /// collection written in the period, and the booklets the barangay holds,
    /// with the control numbers each one covers.
    ///
    /// A receipt is never deleted. If one was written wrongly it is voided, with
    /// a reason: the number stays in the register exactly the way a cancelled
    /// receipt stays in a paper booklet. That is what makes the register
    /// auditable, and it is why voiding is administrator-only.
    /// </summary>
    public class ReceiptsView : ViewBase
    {
        private ComboBox _range;
        private DateTimePicker _from;
        private DateTimePicker _to;
        private TextBox _keyword;
        private CheckBox _includeVoid;
        private DataGridView _grid;
        private DataGridView _booklets;
        private Label _summary;
        private Label _bookletNote;
        private Button _void;
        private Button _closeBooklet;

        private List<OfficialReceipt> _loaded = new List<OfficialReceipt>();

        public ReceiptsView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Official Receipts",
                "Every collection written, and the booklets the barangay holds. A receipt is never deleted: "
                + "a wrong one is voided with a reason and stays in the register.");

            _range = UiFactory.DropDown(new string[]
            {
                "Today", "Yesterday", "This week", "This month", "Choose dates", "Everything"
            }, true);
            _range.Width = 150;
            _range.SelectedIndexChanged += delegate (object sender, EventArgs e)
            {
                ApplyQuickRange(_range.SelectedIndex, _from, _to);
                _from.Enabled = _to.Enabled = _range.SelectedIndex == 4;
                LoadReceipts();
            };

            _from = UiFactory.DatePicker(Clock.Now().Date);
            _from.Width = 150;
            _from.Enabled = false;
            _from.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 4) LoadReceipts(); };

            _to = UiFactory.DatePicker(Clock.Now().Date);
            _to.Width = 150;
            _to.Enabled = false;
            _to.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 4) LoadReceipts(); };

            _keyword = UiFactory.TextBox("Receipt number, control number or payor...", 100);
            _keyword.Width = 280;
            _keyword.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadReceipts(); }
            };

            _includeVoid = UiFactory.CheckBox("Include voided receipts", true);
            _includeVoid.CheckedChanged += delegate (object sender, EventArgs e) { LoadReceipts(); };

            Button search = UiFactory.PrimaryButton("Search");
            search.Click += delegate (object sender, EventArgs e) { LoadReceipts(); };

            Button counter = UiFactory.SecondaryButton("Collect at the counter (no request)");
            counter.Click += delegate (object sender, EventArgs e) { CollectAtCounter(); };

            _void = UiFactory.SecondaryButton("Void a receipt");
            _void.Click += delegate (object sender, EventArgs e) { VoidReceipt(); };

            Button export = UiFactory.GhostButton("Export the collections report");
            export.Click += delegate (object sender, EventArgs e) { ExportCollections(); };

            FlowLayoutPanel filters = UiFactory.Row(_range, _from, _to, _keyword, _includeVoid);
            filters.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 66);
            filters.WrapContents = true;
            filters.Width = 1160;
            filters.Height = 44;
            filters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filters.Controls.Add(search);

            FlowLayoutPanel actions = UiFactory.Row(counter, _void, export);
            actions.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 118);
            actions.WrapContents = true;
            actions.Width = 1160;
            actions.Height = 44;
            actions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _summary = UiFactory.Body(string.Empty);
            _summary.Font = AppTheme.SmallBold;
            _summary.ForeColor = AppTheme.Primary;
            _summary.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 174);
            _summary.AutoSize = true;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 204);
            _grid.Size = new Size(1160, 240);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            SectionPanel bookletCard = Card("The receipt booklets",
                "A collection has to fall inside a booklet the barangay holds, or it cannot be traced back.");
            bookletCard.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 460);
            bookletCard.Size = new Size(1160, 240);
            bookletCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _booklets = UiFactory.Grid();
            _booklets.Location = new Point(AppTheme.Gap4, 58);
            _booklets.Size = new Size(740, 166);
            _booklets.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            bookletCard.Controls.Add(_booklets);

            Button add = UiFactory.PrimaryButton("Record a new booklet");
            add.Location = new Point(776, 58);
            add.Click += delegate (object sender, EventArgs e) { AddBooklet(); };

            _closeBooklet = UiFactory.SecondaryButton("Close the booklet");
            _closeBooklet.Location = new Point(776, 100);
            _closeBooklet.Click += delegate (object sender, EventArgs e) { CloseBooklet(); };

            _bookletNote = UiFactory.Hint(string.Empty);
            _bookletNote.Location = new Point(776, 142);
            _bookletNote.Size = new Size(370, 84);

            bookletCard.Controls.Add(add);
            bookletCard.Controls.Add(_closeBooklet);
            bookletCard.Controls.Add(_bookletNote);

            page.Controls.Add(filters);
            page.Controls.Add(actions);
            page.Controls.Add(_summary);
            page.Controls.Add(_grid);
            page.Controls.Add(bookletCard);

            _range.SelectedIndex = 0;
        }

        public override void RefreshData()
        {
            try
            {
                LoadReceipts();
                LoadBooklets();
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not load the receipts.", error);
            }
        }

        // ==================================================================
        //  The register
        // ==================================================================

        private void LoadReceipts()
        {
            ReceiptQuery query = new ReceiptQuery();
            query.From = _from.Value.Date;
            query.To = _to.Value.Date;
            query.Keyword = _keyword.Text;
            query.IncludeVoid = _includeVoid.Checked;

            _loaded = new List<OfficialReceipt>();
            foreach (OfficialReceipt receipt in Receipts.GetReceipts(query)) _loaded.Add(receipt);

            DataTable table = new DataTable("receipts");
            table.Columns.Add("or_number", typeof(string));
            table.Columns.Add("series", typeof(string));
            table.Columns.Add("control", typeof(string));
            table.Columns.Add("date", typeof(string));
            table.Columns.Add("payor", typeof(string));
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("method", typeof(string));
            table.Columns.Add("state", typeof(string));
            table.Columns.Add("collected_by", typeof(string));

            decimal collected = 0m;
            int voided = 0;

            foreach (OfficialReceipt receipt in _loaded)
            {
                if (receipt.IsVoid) voided++; else collected += receipt.Amount;

                DataRow row = table.NewRow();
                row["or_number"] = receipt.OrNumber;
                row["series"] = receipt.SeriesCode;
                row["control"] = receipt.ControlNumber;
                row["date"] = receipt.OrDate.ToString("dd MMM yyyy");
                row["payor"] = receipt.PayerName;
                row["amount"] = receipt.Amount;
                row["method"] = receipt.GetMethodText();
                row["state"] = receipt.IsVoid ? "Void" : "Valid";
                row["collected_by"] = receipt.CollectedBy;
                table.Rows.Add(row);
            }

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("amount")) _grid.Columns["amount"].DefaultCellStyle.Format = "#,##0.00";
            if (_grid.Columns.Contains("payor")) _grid.Columns["payor"].FillWeight = 130;
            if (_grid.Columns.Contains("state")) _grid.Columns["state"].FillWeight = 50;

            UiFactory.ColourStatusColumn(_grid, "state");

            _summary.Text = "Collected in " + DescribeRange(_from.Value, _to.Value) + ": "
                          + Money(collected) + "   |   " + _loaded.Count + " receipt(s)"
                          + (voided > 0 ? "   |   " + voided + " voided, not counted" : string.Empty);

            Say(_summary.Text);
        }

        private void LoadBooklets()
        {
            IList<ReceiptSeries> series = Receipts.GetSeries(false);

            DataTable table = new DataTable("booklets");
            table.Columns.Add("series_id", typeof(int));
            table.Columns.Add("series", typeof(string));
            table.Columns.Add("control_numbers", typeof(string));
            table.Columns.Add("issued_to", typeof(string));
            table.Columns.Add("issued_on", typeof(string));
            table.Columns.Add("state", typeof(string));

            int active = 0;

            foreach (ReceiptSeries booklet in series)
            {
                if (booklet.IsActive) active++;

                DataRow row = table.NewRow();
                row["series_id"] = booklet.SeriesId;
                row["series"] = booklet.SeriesCode;
                row["control_numbers"] = booklet.RangeText();
                row["issued_to"] = booklet.IssuedTo;
                row["issued_on"] = booklet.IssuedOn.ToString("dd MMM yyyy");
                row["state"] = booklet.IsActive ? "Active" : "Closed";
                table.Rows.Add(row);
            }

            _booklets.DataSource = table;
            _booklets.AutoGenerateColumns = true;

            if (_booklets.Columns.Contains("series_id")) _booklets.Columns["series_id"].Visible = false;

            UiFactory.ColourStatusColumn(_booklets, "state");

            _bookletNote.Text = series.Count == 0
                ? "No booklet is recorded yet. Record the booklet the barangay holds before collecting "
                  + "money - a collection has to point at a real receipt form."
                : active + " active booklet(s) among " + series.Count + " recorded.";
        }

        private OfficialReceipt SelectedReceipt()
        {
            if (_grid.CurrentRow == null) return null;

            int index = _grid.CurrentRow.Index;
            return index >= 0 && index < _loaded.Count ? _loaded[index] : null;
        }

        private ReceiptSeries SelectedBooklet()
        {
            if (_booklets.CurrentRow == null) return null;

            object id = _booklets.CurrentRow.Cells["series_id"].Value;
            if (id == null || id == DBNull.Value) return null;

            int seriesId = Convert.ToInt32(id);

            foreach (ReceiptSeries booklet in Receipts.GetSeries(false))
                if (booklet.SeriesId == seriesId) return booklet;

            return null;
        }

        // ==================================================================
        //  Actions
        // ==================================================================

        private void CollectAtCounter()
        {
            string payor = TextPromptForm.Ask(this, "Collect at the counter",
                "Whose payment is this? Type the name exactly as it should read on the receipt.",
                string.Empty, false, true);

            if (payor == null) return;

            using (PaymentForm form = new PaymentForm(Receipts, payor, 0m, Clock))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void VoidReceipt()
        {
            OfficialReceipt receipt = SelectedReceipt();

            if (receipt == null)
            {
                Dialog.Warn(this, "Please choose a receipt from the register first.", "No receipt chosen");
                return;
            }

            if (receipt.IsVoid)
            {
                Dialog.Info(this, "OR " + receipt.OrNumber + " was already voided: " + receipt.VoidReason,
                    "Already voided");
                return;
            }

            string reason = TextPromptForm.Ask(this, "Void OR " + receipt.OrNumber,
                "Why is this receipt being cancelled? The reason stays on the register for good, and the "
                + "number is never reused.", string.Empty, false, true);

            if (reason == null) return;

            if (!Dialog.ConfirmChange(this, "Void OR " + receipt.OrNumber + " for "
                + Money(receipt.Amount) + " (" + receipt.PayerName + ")?" + Environment.NewLine
                + Environment.NewLine + "Reason: " + reason)) return;

            OperationResult result = Receipts.Void(receipt, reason);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Announce(result.Message);
        }

        private void AddBooklet()
        {
            using (ReceiptSeriesForm form = new ReceiptSeriesForm(Receipts))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void CloseBooklet()
        {
            ReceiptSeries booklet = SelectedBooklet();

            if (booklet == null)
            {
                Dialog.Warn(this, "Please choose a booklet from the list first.", "No booklet chosen");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Close booklet " + booklet.SeriesCode + " ("
                + booklet.RangeText() + ")?" + Environment.NewLine + Environment.NewLine
                + "A closed booklet takes no more collections. Everything already written stays.")) return;

            OperationResult result = Receipts.CloseSeries(booklet);

            if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

            Announce(result.Message);
        }

        private void ExportCollections()
        {
            try
            {
                ReportParameters parameters = new ReportParameters();
                parameters.From = _from.Value.Date;
                parameters.To = _to.Value.Date;

                ReportResult result = Reports.Run(ReportDefinitions.Collections, parameters);
                string file = Reports.ExportCsv(result, AppConfig.ApplicationFolder);

                Dialog.Info(this, "The collections report was written to:" + Environment.NewLine
                    + Environment.NewLine + file, "Collections exported");

                Say("Collections exported to " + file);
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not export the collections.", error);
            }
        }
    }
}
