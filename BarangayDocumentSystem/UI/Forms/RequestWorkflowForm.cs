// ---------------------------------------------------------------------------
//  RequestWorkflowForm.cs - moving one request along.
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
using BarangayDocumentSystem.UI.Dialogs;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The screen that moves a request from one status to the next.
    ///
    /// The path through a request is the barangay's actual process, in order:
    ///
    ///   Pending          filed, waiting - outside office hours, or it needs
    ///                    something checked first
    ///   Processing       somebody has to look at the record, the business or
    ///                    the supporting paper before it can be cleared
    ///   Cleared          ready to be prepared; this is where a request that
    ///                    needed no validation lands when it is filed between
    ///                    8:00 AM and 4:00 PM
    ///   Ready for release prepared, signed and waiting for the resident
    ///   Released         handed over, and the fee settled if there is one
    ///
    /// Two things are enforced here rather than trusted to the clerk's memory:
    /// a request with an unsettled fee cannot be released - the OR must be
    /// issued first - and a business clearance can never skip validation,
    /// because the barangay inspects the business before it signs.
    /// </summary>
    public class RequestWorkflowForm : Form
    {
        private readonly RequestService _requests;
        private readonly ReceiptService _receipts;
        private readonly DocumentRequest _request;
        private readonly IClock _clock;

        private NameGroupBox _residentBox;
        private Label _details;
        private Label _feeNote;
        private Label _stateNote;
        private DataGridView _history;

        private Button _process;
        private Button _clear;
        private Button _ready;
        private Button _release;
        private Button _reject;
        private Button _reopen;
        private Button _payment;
        private Button _close;

        public string Message { get; private set; }

        public RequestWorkflowForm(RequestService requests, ReceiptService receipts,
                                   DocumentRequest request, IClock clock)
        {
            _requests = requests;
            _receipts = receipts;
            _request = request;
            _clock = clock;

            BuildWindow();
            RefreshScreen();
        }

        private void BuildWindow()
        {
            Text = "Request " + _request.ReferenceNumber;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(820, 700);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.AutoScroll = true;
            page.Padding = new Padding(AppTheme.PageMargin, AppTheme.Gap4, AppTheme.PageMargin, AppTheme.Gap4);
            page.BackColor = AppTheme.Canvas;

            _residentBox = new NameGroupBox("Resident");
            _residentBox.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);
            _residentBox.Size = new Size(756, 88);

            _details = new Label();
            _details.Font = AppTheme.Body;
            _details.ForeColor = AppTheme.Ink;
            _details.AutoSize = false;
            _details.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 100);
            _details.Size = new Size(756, 66);

            _feeNote = new Label();
            _feeNote.Font = AppTheme.SmallBold;
            _feeNote.ForeColor = AppTheme.Primary;
            _feeNote.AutoSize = false;
            _feeNote.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 172);
            _feeNote.Size = new Size(756, 22);

            _stateNote = new Label();
            _stateNote.Font = AppTheme.Small;
            _stateNote.ForeColor = AppTheme.Muted;
            _stateNote.AutoSize = false;
            _stateNote.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 196);
            _stateNote.Size = new Size(756, 44);

            FlatGroupBox next = new FlatGroupBox("What happens next");
            next.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 250);
            next.Size = new Size(756, 140);

            _process = UiFactory.SecondaryButton("Send for processing");
            _clear = UiFactory.PrimaryButton("Clear this request");
            _ready = UiFactory.PrimaryButton("Prepare for release");
            _release = UiFactory.PrimaryButton("Release to the resident");
            _reject = UiFactory.DangerButton("Reject");
            _reopen = UiFactory.GhostButton("Reopen");
            _payment = UiFactory.SecondaryButton("Collect the fee (OR)");

            UiFactory.AlignButtons(_process, _clear, _ready, _release, _reject, _reopen, _payment);

            _process.Click += delegate (object sender, EventArgs e) { Process(); };
            _clear.Click += delegate (object sender, EventArgs e) { Clear(); };
            _ready.Click += delegate (object sender, EventArgs e) { Ready(); };
            _release.Click += delegate (object sender, EventArgs e) { Release(); };
            _reject.Click += delegate (object sender, EventArgs e) { Reject(); };
            _reopen.Click += delegate (object sender, EventArgs e) { Reopen(); };
            _payment.Click += delegate (object sender, EventArgs e) { Collect(); };

            FlowLayoutPanel rowOne = UiFactory.ButtonRow(_process, _clear, _ready, _release);
            rowOne.Location = new Point(12, 30);

            FlowLayoutPanel rowTwo = UiFactory.ButtonRow(_payment, _reject, _reopen);
            rowTwo.Location = new Point(12, 74);

            next.Controls.Add(rowOne);
            next.Controls.Add(rowTwo);

            SectionPanel historyCard = new SectionPanel("History",
                "Who moved this request, when, and why.");
            historyCard.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 406);
            historyCard.Size = new Size(756, 200);

            _history = UiFactory.Grid();
            _history.Location = new Point(AppTheme.Gap4, AppTheme.Gap5 + 34);
            _history.Size = new Size(historyCard.Width - (AppTheme.Gap4 * 2), historyCard.Height - AppTheme.Gap5 - 48);
            historyCard.Controls.Add(_history);

            page.Controls.Add(_residentBox);
            page.Controls.Add(_details);
            page.Controls.Add(_feeNote);
            page.Controls.Add(_stateNote);
            page.Controls.Add(next);
            page.Controls.Add(historyCard);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = AppTheme.Surface;

            _close = UiFactory.SecondaryButton("Close");
            _close.Width = 110;
            _close.Location = new Point(ClientSize.Width - 110 - AppTheme.PageMargin, 15);
            _close.Click += delegate (object sender, EventArgs e)
            {
                DialogResult = string.IsNullOrEmpty(Message) ? DialogResult.Cancel : DialogResult.OK;
                Close();
            };

            footer.Controls.Add(_close);

            Controls.Add(page);
            Controls.Add(footer);

            CancelButton = _close;
        }

        // ==================================================================
        //  Drawing the current state
        // ==================================================================

        private void RefreshScreen()
        {
            _residentBox.NameText = (_request.ResidentName ?? string.Empty).ToUpperInvariant();
            _residentBox.NoteText = _request.ReferenceNumber + "  |  filed "
                                  + _request.DateRequested.ToString("dd MMMM yyyy, h:mm tt");

            _details.Text =
                "Document: " + _request.GetDocumentName() + Environment.NewLine
                + "Purpose: " + (string.IsNullOrWhiteSpace(_request.Purpose) ? "(not stated)" : _request.Purpose)
                + Environment.NewLine
                + "Status now: " + _request.GetStatusText()
                + "   |   " + (string.IsNullOrWhiteSpace(_request.OfficialReceiptNumber)
                    ? "no receipt yet"
                    : "OR " + _request.OfficialReceiptNumber + " (control " + _request.OrControlNumber + ")");

            // Amount only - never the sentence of law behind it. That is the
            // "erase the basis" change the barangay asked for.
            _feeNote.Text = "Fee: " + _request.GetFeeText()
                          + (_request.HasUnsettledFee ? "   - not settled yet" : "   - settled");

            string windowText = string.IsNullOrWhiteSpace(_request.Remarks)
                ? string.Empty
                : "Remarks: " + _request.Remarks + "   ";

            _stateNote.Text = windowText
                + (string.IsNullOrWhiteSpace(_request.LastStatusChangeBy)
                    ? string.Empty
                    : "Last moved by " + _request.LastStatusChangeBy + " on "
                      + _request.LastStatusChangeOn.ToString("dd MMM yyyy h:mm tt") + ".");

            // Only the buttons that make sense for this status are shown. A
            // button that cannot work is worse than no button - it invites a
            // click that ends in a refusal.
            bool closed = _request.IsClosed;
            bool pending = _request.Status == RequestStatus.Pending;
            bool processing = _request.Status == RequestStatus.Processing;
            bool cleared = _request.Status == RequestStatus.Cleared;
            bool ready = _request.Status == RequestStatus.ReadyForRelease;

            _process.Visible = pending || processing;
            _clear.Visible = pending || processing;
            _ready.Visible = cleared;
            _release.Visible = ready;
            _reject.Visible = !closed;
            _reopen.Visible = _request.Status == RequestStatus.Rejected;
            _payment.Visible = _request.HasUnsettledFee && !closed;

            LoadHistory();
        }

        private void LoadHistory()
        {
            System.Data.DataTable table = new System.Data.DataTable("history");
            table.Columns.Add("when", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("by", typeof(string));
            table.Columns.Add("reason", typeof(string));

            foreach (RequestStatusChange change in _request.History)
            {
                System.Data.DataRow row = table.NewRow();
                row["when"] = change.ChangedOn.ToString("dd MMM yyyy h:mm tt");
                row["status"] = EnumText.Of(change.Status);
                row["by"] = change.ChangedBy;
                row["reason"] = change.Reason;
                table.Rows.Add(row);
            }

            _history.DataSource = table;
            _history.AutoGenerateColumns = true;

            if (_history.Columns.Contains("reason")) _history.Columns["reason"].FillWeight = 220;
        }

        // ==================================================================
        //  The moves
        // ==================================================================

        private void Process()
        {
            string reason = TextPromptForm.Ask(this, "Send for processing",
                "What has to be checked before this document can be cleared?",
                "e.g. business inspection, record on file, supporting paper",
                "Business inspected on " + _clock.Now().ToString("dd MMM yyyy") + "; owner's record checked.", true);

            if (reason == null) return;

            Apply(_requests.SendToProcessing(_request, reason));
        }

        private void Clear()
        {
            string reason = TextPromptForm.Ask(this, "Clear this request",
                "What was checked, and by whom?",
                "e.g. record on file, no adverse record, requirements complete",
                "Requirements complete. Record checked and no adverse record on file.", true);

            if (reason == null) return;

            Apply(_requests.Clear(_request, reason));
        }

        private void Ready()
        {
            if (!Dialog.ConfirmChange(this, "Prepare " + _request.ReferenceNumber + " for release?"
                + Environment.NewLine + "The document can then be handed over at the counter.")) return;

            Apply(_requests.MarkReadyForRelease(_request));
        }

        private void Release()
        {
            string blocker;
            if (!_request.CanRelease(out blocker))
            {
                Dialog.Refused(this, blocker);
                return;
            }

            string receivedBy = TextPromptForm.Ask(this, "Release the document",
                "Who is receiving it? Type the name of the person at the counter.",
                "e.g. Maria Santos (the resident, or a relative with a note)",
                _request.ResidentName, true);

            if (receivedBy == null) return;

            if (!Dialog.ConfirmChange(this, "Hand " + _request.GetDocumentName() + " to " + receivedBy
                + "?" + Environment.NewLine + Environment.NewLine
                + "Fee: " + _request.GetFeeText() + ". This closes the request.")) return;

            Apply(_requests.Release(_request, receivedBy));
        }

        private void Reject()
        {
            string reason = TextPromptForm.Ask(this, "Reject this request",
                "Why is the barangay refusing it? The resident is entitled to a clear answer.",
                "e.g. the requirements are not complete, the record is not on file",
                "Requirements not complete.", true);

            if (reason == null) return;

            Apply(_requests.Reject(_request, reason));
        }

        private void Reopen()
        {
            string reason = TextPromptForm.Ask(this, "Reopen this request",
                "Why is a request that was rejected being taken up again?",
                "e.g. the missing requirement was submitted today",
                "Missing requirement submitted.", true);

            if (reason == null) return;

            Apply(_requests.Reopen(_request, reason));
        }

        private void Collect()
        {
            using (PaymentForm form = new PaymentForm(_receipts, _request, _clock))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                Message = form.Message;
                Dialog.Info(this, form.Message, "Collection recorded");
                RefreshScreen();
            }
        }

        /// <summary>Every move ends here, so the refusals are worded once.</summary>
        private void Apply(OperationResult result)
        {
            if (!result.Succeeded)
            {
                Dialog.Refused(this, result.Message);
                return;
            }

            Message = result.Message;
            Dialog.Info(this, result.Message, "Done");
            RefreshScreen();
        }
    }
}
