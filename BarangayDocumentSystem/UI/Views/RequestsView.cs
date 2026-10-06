// ---------------------------------------------------------------------------
//  RequestsView.cs - the queue, the per-day filter, and handing the paper over.
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
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.Services.Reports;
using BarangayDocumentSystem.UI.Controls;
using BarangayDocumentSystem.UI.Dialogs;
using BarangayDocumentSystem.UI.Forms;

namespace BarangayDocumentSystem.UI.Views
{
    /// <summary>
    /// The request queue.
    ///
    /// This is the screen the barangay asked for in one line - "New Request
    /// instead of New Document" - so the only button that creates anything is
    /// called New request, and what it creates is a request: a reference
    /// number, a document to be issued, a fee, a status and a history.
    ///
    /// The other half of the screen is the per-day filter: today, yesterday,
    /// this week, this month, or two dates of the clerk's choosing, with the
    /// figures for that period printed underneath. That filter is the same one
    /// the daily transaction report uses, and it is deliberately the same code.
    ///
    /// The moves a request can make are:
    ///   Pending -> Processing -> Cleared -> Ready for release -> Released,
    /// with Rejected off to one side and Reopen bringing it back. A request that
    /// needed no checking goes straight to Cleared when it is filed inside the
    /// office window, which is the time-shift rule the barangay asked for.
    /// </summary>
    public class RequestsView : ViewBase
    {
        private ComboBox _range;
        private DateTimePicker _from;
        private DateTimePicker _to;
        private ComboBox _status;
        private ComboBox _document;
        private ComboBox _purok;
        private TextBox _keyword;
        private DataGridView _grid;
        private Label _figures;
        private Label _cutOff;
        private Label _historyLabel;
        private DataGridView _history;

        private List<DocumentRequest> _loaded = new List<DocumentRequest>();

        public RequestsView(IBarangayRepository repository, SessionManager session, IClock clock)
            : base(repository, session, clock)
        {
        }

        public override void Build()
        {
            Panel page = BuildPage("Document Requests",
                "Filing and the queue. A request that needs no checking is cleared between "
                + AppConfig.OfficeWindowStart.ToString(@"h\:mm") + " and "
                + AppConfig.OfficeWindowEnd.ToString(@"h\:mm") + "; outside those hours it waits as Pending "
                + "and is cleared when the window opens.");

            _range = UiFactory.DropDown(new string[]
            {
                "Today", "Yesterday", "This week", "This month", "Last 30 days", "Choose dates", "Everything"
            }, true);
            _range.Width = 150;
            _range.SelectedIndexChanged += delegate (object sender, EventArgs e)
            {
                ApplyQuickRange(_range.SelectedIndex, _from, _to);
                _from.Enabled = _to.Enabled = _range.SelectedIndex == 5;
                LoadRequests();
            };

            _from = UiFactory.DatePicker(Clock.Now().Date);
            _from.Width = 150;
            _from.Enabled = false;
            _from.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 5) LoadRequests(); };

            _to = UiFactory.DatePicker(Clock.Now().Date);
            _to.Width = 150;
            _to.Enabled = false;
            _to.ValueChanged += delegate (object sender, EventArgs e) { if (_range.SelectedIndex == 5) LoadRequests(); };

            _status = UiFactory.DropDown(new string[]
            {
                "Any status", "Pending", "Processing", "Cleared", "Ready for release", "Released", "Rejected"
            }, true);
            _status.Width = 160;
            _status.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadRequests(); };

            _document = UiFactory.DropDown(null, true);
            FillDocuments(_document, "All documents");
            _document.Width = 200;
            _document.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadRequests(); };

            _purok = UiFactory.DropDown(null, true);
            FillPuroks(_purok, "All puroks");
            _purok.Width = 160;
            _purok.SelectedIndexChanged += delegate (object sender, EventArgs e) { LoadRequests(); };

            _keyword = UiFactory.TextBox("Reference, name, business...", 100);
            _keyword.Width = 220;
            _keyword.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadRequests(); }
            };

            Button newRequest = UiFactory.PrimaryButton("New request");
            newRequest.Click += delegate (object sender, EventArgs e) { NewRequest(); };

            Button open = UiFactory.SecondaryButton("Open and move it along");
            open.Click += delegate (object sender, EventArgs e) { Open(SelectedRequest()); };

            Button payment = UiFactory.SecondaryButton("Collect the fee (OR)");
            payment.Click += delegate (object sender, EventArgs e) { CollectFee(); };

            Button print = UiFactory.SecondaryButton("Preview / print the document");
            print.Click += delegate (object sender, EventArgs e) { PrintDocument(); };

            Button clearFilters = UiFactory.GhostButton("Clear filters");
            clearFilters.Click += delegate (object sender, EventArgs e)
            {
                _range.SelectedIndex = 0;
                _status.SelectedIndex = 0;
                _document.SelectedIndex = 0;
                _purok.SelectedIndex = 0;
                _keyword.Text = string.Empty;
                LoadRequests();
            };

            FlowLayoutPanel filters = UiFactory.Row(_range, _from, _to, _status, _document, _purok, _keyword);
            filters.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 70);
            filters.WrapContents = true;
            filters.Width = 1160;
            filters.Height = 44;
            filters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            FlowLayoutPanel actions = UiFactory.Row(newRequest, open, payment, print, clearFilters);
            actions.Location = new Point(AppTheme.PageMargin - 4, AppTheme.PageMargin + 122);
            actions.WrapContents = true;
            actions.Width = 1160;
            actions.Height = 44;
            actions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _cutOff = UiFactory.Body(string.Empty);
            _cutOff.Font = AppTheme.SmallBold;
            _cutOff.ForeColor = AppTheme.Primary;
            _cutOff.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 174);
            _cutOff.AutoSize = true;

            _figures = UiFactory.Body(string.Empty);
            _figures.Font = AppTheme.Small;
            _figures.ForeColor = AppTheme.Muted;
            _figures.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 196);
            _figures.AutoSize = true;

            _grid = UiFactory.Grid();
            _grid.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 226);
            _grid.Size = new Size(1160, 250);
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _grid.SelectionChanged += delegate (object sender, EventArgs e) { LoadHistory(); };
            _grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0) Open(SelectedRequest());
            };

            SectionPanel historyCard = Card("History of the selected request",
                "Every move, with the person who made it and the reason they wrote.");
            historyCard.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 486);
            historyCard.Size = new Size(1160, 210);
            historyCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _historyLabel = UiFactory.Hint("Choose a request above to see what has happened to it.");
            _historyLabel.Location = new Point(AppTheme.Gap4, 56);
            _historyLabel.Size = new Size(1128, 20);
            historyCard.Controls.Add(_historyLabel);

            _history = UiFactory.Grid();
            _history.Location = new Point(AppTheme.Gap4, 80);
            _history.Size = new Size(1128, 116);
            _history.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            historyCard.Controls.Add(_history);

            page.Controls.Add(filters);
            page.Controls.Add(actions);
            page.Controls.Add(_cutOff);
            page.Controls.Add(_figures);
            page.Controls.Add(_grid);
            page.Controls.Add(historyCard);

            _range.SelectedIndex = 0;
        }

        public override void RefreshData()
        {
            try
            {
                _cutOff.Text = "Now " + Clock.Now().ToString("h:mm tt") + " - " + Window.GetCutOffText();
                LoadRequests();
                LoadHistory();
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not load the requests.", error);
            }
        }

        // ==================================================================
        //  The per-day filter
        // ==================================================================

        private RequestQuery Query()
        {
            RequestQuery query = new RequestQuery();
            query.From = _from.Value.Date;
            query.To = _to.Value.Date;
            query.Keyword = _keyword.Text;
            query.Purok = PurokFrom(_purok);
            query.DocumentType = DocumentFrom(_document);

            if (_status.SelectedIndex > 0)
                query.Status = (RequestStatus)(_status.SelectedIndex - 1);

            return query;
        }

        private void LoadRequests()
        {
            _loaded = new List<DocumentRequest>();

            foreach (DocumentRequest request in Requests.GetRequests(Query())) _loaded.Add(request);

            DataTable table = new DataTable("requests");
            table.Columns.Add("request_id", typeof(int));
            table.Columns.Add("reference", typeof(string));
            table.Columns.Add("filed", typeof(string));
            table.Columns.Add("resident", typeof(string));
            table.Columns.Add("document", typeof(string));
            table.Columns.Add("purpose", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("fee", typeof(string));
            table.Columns.Add("paid", typeof(string));
            table.Columns.Add("waiting", typeof(int));

            int waiting = 0, released = 0, rejected = 0;
            decimal collected = 0m, outstanding = 0m;

            foreach (DocumentRequest request in _loaded)
            {
                if (!request.IsClosed) waiting++;
                if (request.Status == RequestStatus.Released) released++;
                if (request.Status == RequestStatus.Rejected) rejected++;

                // Collections and outstanding amounts always come from the
                // request's own paid state, never from a total somebody typed.
                if (request.IsPaid) collected += request.Fee;
                else outstanding += request.OutstandingAmount;

                DataRow row = table.NewRow();
                row["request_id"] = request.RequestId;
                row["reference"] = request.ReferenceNumber;
                row["filed"] = request.DateRequested.ToString("dd MMM yyyy h:mm tt");
                row["resident"] = request.ResidentName;
                row["document"] = request.GetDocumentName();
                row["purpose"] = request.Purpose;
                row["status"] = request.GetStatusText();
                row["fee"] = request.GetFeeText();
                row["paid"] = request.IsPaid ? "Paid" : (request.Fee > 0m ? "Not paid" : "No fee");
                row["waiting"] = request.IsClosed ? 0 : request.GetWaitingDays(DateTime.Today);
                table.Rows.Add(row);
            }

            _grid.DataSource = table;
            _grid.AutoGenerateColumns = true;

            if (_grid.Columns.Contains("request_id")) _grid.Columns["request_id"].Visible = false;
            if (_grid.Columns.Contains("document")) _grid.Columns["document"].FillWeight = 140;
            if (_grid.Columns.Contains("purpose")) _grid.Columns["purpose"].FillWeight = 130;
            if (_grid.Columns.Contains("paid")) _grid.Columns["paid"].FillWeight = 60;
            if (_grid.Columns.Contains("waiting")) _grid.Columns["waiting"].FillWeight = 50;

            UiFactory.ColourStatusColumn(_grid, "status");
            UiFactory.ColourStatusColumn(_grid, "paid");

            _figures.Text = DescribeRange(_from.Value, _to.Value) + ":  " + _loaded.Count + " request(s)"
                          + "   |   waiting " + waiting
                          + "   |   released " + released
                          + "   |   rejected " + rejected
                          + "   |   collected " + Money(collected)
                          + "   |   still to collect " + Money(outstanding);

            Say(_loaded.Count + " request(s) listed for " + DescribeRange(_from.Value, _to.Value) + ".");
        }

        private void LoadHistory()
        {
            DocumentRequest request = SelectedRequest();

            DataTable table = new DataTable("history");
            table.Columns.Add("when", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("by", typeof(string));
            table.Columns.Add("reason", typeof(string));

            if (request != null)
            {
                foreach (RequestStatusChange change in request.History)
                {
                    DataRow row = table.NewRow();
                    row["when"] = change.ChangedOn.ToString("dd MMM yyyy h:mm tt");
                    row["status"] = EnumText.Of(change.Status);
                    row["by"] = change.ChangedBy;
                    row["reason"] = change.Reason;
                    table.Rows.Add(row);
                }

                _historyLabel.Text = request.ReferenceNumber + " - " + request.GetDocumentName()
                                   + " - " + request.GetStatusText()
                                   + " - fee " + request.GetFeeText()
                                   + (request.IsPaid ? " (paid, OR " + request.OfficialReceiptNumber + ")" : string.Empty);
            }
            else
            {
                _historyLabel.Text = "Choose a request above to see what has happened to it.";
            }

            _history.DataSource = table;
            _history.AutoGenerateColumns = true;

            if (_history.Columns.Contains("reason")) _history.Columns["reason"].FillWeight = 220;
        }

        private DocumentRequest SelectedRequest()
        {
            if (_grid.CurrentRow == null) return null;

            int index = _grid.CurrentRow.Index;
            if (index < 0 || index >= _loaded.Count) return null;

            return _loaded[index];
        }

        // ==================================================================
        //  Actions
        // ==================================================================

        private void NewRequest()
        {
            using (NewRequestForm form = new NewRequestForm(Requests, Residents, Clock))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void Open(DocumentRequest request)
        {
            if (request == null)
            {
                Dialog.Warn(this, "Please choose a request from the list first.", "No request chosen");
                return;
            }

            using (RequestWorkflowForm form = new RequestWorkflowForm(Requests, Receipts, request, Clock))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void CollectFee()
        {
            DocumentRequest request = SelectedRequest();

            if (request == null)
            {
                Dialog.Warn(this, "Please choose a request from the list first.", "No request chosen");
                return;
            }

            if (!request.HasUnsettledFee)
            {
                Dialog.Info(this, request.GetFeeText() == "Free"
                    ? "There is nothing to collect on " + request.ReferenceNumber + " - it is free."
                    : "The fee on " + request.ReferenceNumber + " is already settled against OR "
                      + request.OfficialReceiptNumber + ".",
                    "Nothing to collect");
                return;
            }

            using (PaymentForm form = new PaymentForm(Receipts, request, Clock))
            {
                if (form.ShowDialog(this) == DialogResult.OK) Announce(form.Message);
            }
        }

        private void PrintDocument()
        {
            DocumentRequest request = SelectedRequest();

            if (request == null)
            {
                Dialog.Warn(this, "Please choose a request from the list first.", "No request chosen");
                return;
            }

            Resident resident = Residents.Get(request.ResidentId);

            if (resident == null)
            {
                Dialog.Warn(this, "The resident on this request is not on file any more, so the document "
                    + "cannot be prepared.", "Cannot prepare the document");
                return;
            }

            using (DocumentPreviewForm form = new DocumentPreviewForm(request, resident,
                Residents.GetDependents(resident.ResidentId)))
            {
                form.ShowDialog(this);

                if (!string.IsNullOrEmpty(form.Message)) Say(form.Message);
            }
        }
    }
}
