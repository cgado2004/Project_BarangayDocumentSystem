// ---------------------------------------------------------------------------
//  PaymentForm.cs - writing an official receipt.
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
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The collection window - the official receipt, written the way a real
    /// receipt book works.
    ///
    /// The barangay keeps a government receipt book, so a collection is not one
    /// number but three, and they have to agree with each other:
    ///
    ///   * the series - which booklet;
    ///   * the control number - which printed form inside that booklet, and it
    ///     has to fall inside a booklet the barangay actually holds, or the
    ///     collection cannot be traced afterwards; that traceability is the
    ///     whole point of a government receipt;
    ///   * the official receipt number - unique inside its series, so the same
    ///     number cannot be used twice.
    ///
    /// The window also writes for something with no request behind it - a cedula
    /// paid at the counter - because a receipt is a receipt either way. Only the
    /// link to a request is empty.
    /// </summary>
    public class PaymentForm : Form
    {
        private readonly ReceiptService _receipts;
        private readonly DocumentRequest _request;         // null = a counter collection
        private readonly string _payerName;
        private readonly DateTime _moment;

        private GroupBox _booklet;
        private ComboBox _series;
        private TextBox _orNumber;
        private TextBox _controlNumber;
        private Label _bookletNote;
        private NumericUpDown _amount;
        private ComboBox _method;
        private TextBox _remarks;
        private Label _payerBox;
        private Button _collect;
        private Button _cancel;

        public string Message { get; private set; }

        /// <summary>Collecting on a filed request: the payer and the amount
        /// come from the request itself.</summary>
        public PaymentForm(ReceiptService receipts, DocumentRequest request, IClock clock)
        {
            _receipts = receipts;
            _request = request;
            _payerName = request == null ? string.Empty : request.ResidentName;
            _moment = clock == null ? DateTime.Now : clock.Now();

            BuildWindow();
        }

        /// <summary>Collecting at the counter with no request behind it.</summary>
        public PaymentForm(ReceiptService receipts, string payerName, decimal suggestedAmount, IClock clock)
        {
            _receipts = receipts;
            _request = null;
            _payerName = payerName ?? string.Empty;
            _moment = clock == null ? DateTime.Now : clock.Now();

            BuildWindow();

            if (suggestedAmount > 0m) _amount.Value = Math.Min(suggestedAmount, _amount.Maximum);
        }

        // ==================================================================
        //  The window
        // ==================================================================

        private void BuildWindow()
        {
            Text = _request == null
                ? "Collect a payment"
                : "Collect for " + _request.ReferenceNumber;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(680, 620);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading("Official receipt");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption("Write the numbers exactly as they are printed on the form in "
                + "your hand. The control number has to belong to a booklet the barangay holds.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(610, 34);

            // ---- who is paying ----
            GroupBox payer = new GroupBox();
            payer.Text = "Payor";
            payer.Font = AppTheme.SmallBold;
            payer.ForeColor = AppTheme.Primary;
            payer.BackColor = AppTheme.Surface;
            payer.FlatStyle = FlatStyle.Flat;
            payer.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 76);
            payer.Size = new Size(616, 86);

            _payerBox = UiFactory.Body(string.Empty);
            _payerBox.Font = new Font(AppTheme.UiFamily, 12f, FontStyle.Bold);
            _payerBox.Location = new Point(AppTheme.Gap3, 30);
            _payerBox.AutoSize = true;
            payer.Controls.Add(_payerBox);

            Label payerNote = UiFactory.Hint(_request == null
                ? "A collection at the counter with no request behind it."
                : _request.ReferenceNumber + "  -  " + _request.GetDocumentName() + "  -  fee "
                  + _request.GetFeeText());
            payerNote.Location = new Point(AppTheme.Gap3, 54);
            payerNote.Size = new Size(580, 20);
            payer.Controls.Add(payerNote);

            // ---- the booklet ----
            _booklet = new GroupBox();
            _booklet.Text = "The receipt book";
            _booklet.Font = AppTheme.SmallBold;
            _booklet.ForeColor = AppTheme.Primary;
            _booklet.BackColor = AppTheme.Surface;
            _booklet.FlatStyle = FlatStyle.Flat;
            _booklet.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 174);
            _booklet.Size = new Size(616, 150);

            _series = UiFactory.DropDown(SeriesNames(), true);
            _series.SelectedIndexChanged += delegate (object sender, EventArgs e) { SeriesChanged(); };

            _orNumber = UiFactory.TextBox("as printed, e.g. 1234567", 40);
            _controlNumber = UiFactory.TextBox("as printed, e.g. 0000123", 40);
            _controlNumber.TextChanged += delegate (object sender, EventArgs e) { ControlChanged(); };

            Place(_booklet, "Receipt booklet (series)", _series, AppTheme.Gap3, 30, 260);
            Place(_booklet, "Official receipt number", _orNumber, AppTheme.Gap3 + 272, 30, 150);
            Place(_booklet, "Control number", _controlNumber, AppTheme.Gap3 + 434, 30, 150);

            _bookletNote = UiFactory.Hint(string.Empty);
            _bookletNote.Location = new Point(AppTheme.Gap3, 88);
            _bookletNote.Size = new Size(580, 50);
            _booklet.Controls.Add(_bookletNote);

            // ---- the amount ----
            GroupBox amount = new GroupBox();
            amount.Text = "Amount and method";
            amount.Font = AppTheme.SmallBold;
            amount.ForeColor = AppTheme.Primary;
            amount.BackColor = AppTheme.Surface;
            amount.FlatStyle = FlatStyle.Flat;
            amount.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 336);
            amount.Size = new Size(616, 132);

            _amount = new NumericUpDown();
            _amount.Font = new Font(AppTheme.UiFamily, 14f, FontStyle.Bold);
            _amount.DecimalPlaces = 2;
            _amount.Minimum = 0m;
            _amount.Maximum = 1000000m;
            _amount.ThousandsSeparator = true;
            _amount.Value = _request != null && _request.Fee > 0m ? _request.Fee : 0m;

            _method = UiFactory.DropDown(new string[] { "Cash", "Check", "Online payment" }, true);
            _remarks = UiFactory.TextBox("anything worth noting on the receipt", 200);

            Place(amount, "Amount collected (P)", _amount, AppTheme.Gap3, 30, 200);
            Place(amount, "How it was paid", _method, AppTheme.Gap3 + 212, 30, 180);
            Place(amount, "Remarks", _remarks, AppTheme.Gap3 + 404, 30, 180);

            // ---- the buttons ----
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            _collect = UiFactory.PrimaryButton("Write the receipt");
            _collect.Width = 190;
            _collect.Location = new Point(ClientSize.Width - 190 - AppTheme.PageMargin, 12);
            _collect.Click += delegate (object sender, EventArgs e) { Collect(); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Location = new Point(_collect.Left - _cancel.Width - AppTheme.Gap2, 12);
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_collect);
            footer.Controls.Add(_cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(payer);
            Controls.Add(_booklet);
            Controls.Add(amount);
            Controls.Add(footer);

            AcceptButton = _collect;
            CancelButton = _cancel;

            _payerBox.Text = _payerName.ToUpperInvariant();
        }

        private static void Place(Control parent, string caption, Control control, int x, int y, int width)
        {
            Label label = UiFactory.FieldLabel(caption);
            label.Location = new Point(x, y);
            label.Width = width;

            control.Location = new Point(x, y + 18);
            control.Width = width;

            parent.Controls.Add(label);
            parent.Controls.Add(control);
        }

        private List<string> SeriesNames()
        {
            List<string> names = new List<string>();

            foreach (ReceiptSeries series in _receipts.GetSeries(true))
                names.Add(series.SeriesCode + "  (" + series.RangeText() + ")");

            return names;
        }

        private ReceiptSeries SelectedSeries()
        {
            if (_series.SelectedIndex < 0) return null;

            IList<ReceiptSeries> all = _receipts.GetSeries(true);
            return _series.SelectedIndex < all.Count ? all[_series.SelectedIndex] : null;
        }

        // ==================================================================
        //  Reacting to the book in the clerk's hand
        // ==================================================================

        private void SeriesChanged()
        {
            ReceiptSeries series = SelectedSeries();

            if (series == null)
            {
                _bookletNote.Text = "No receipt booklet is recorded yet. Ask the administrator to record "
                    + "the booklet on the Official Receipts screen before collecting money.";
                _collect.Enabled = false;
                return;
            }

            _collect.Enabled = true;

            // A courteous guess at the next number: the clerk still reads it off
            // the paper, because the paper is what counts.
            string suggestion = _receipts.SuggestNextOrNumber(series.SeriesCode);
            if (!string.IsNullOrEmpty(suggestion) && string.IsNullOrWhiteSpace(_orNumber.Text))
                _orNumber.Text = suggestion;

            _bookletNote.Text = "Booklet " + series.SeriesCode + " covers control numbers "
                              + series.RangeText()
                              + (series.IsActive ? string.Empty : " - and it is closed")
                              + (string.IsNullOrEmpty(suggestion) ? "." : ". Last used: " + suggestion + ".");
        }

        /// <summary>
        /// Checks the control number as it is typed and says which booklet it
        /// came from. A number that belongs to no booklet is a form the barangay
        /// never received, and that is worth catching before the money moves.
        /// </summary>
        private void ControlChanged()
        {
            string typed = _controlNumber.Text.Trim();
            if (typed.Length < 4) return;

            ReceiptSeries match = _receipts.FindSeriesForControlNumber(typed);

            if (match == null)
            {
                _bookletNote.ForeColor = AppTheme.Danger;
                _bookletNote.Text = "No booklet on record contains control number " + typed
                                  + ". Please check the form in your hand, or ask the administrator to "
                                  + "record the booklet.";
                return;
            }

            _bookletNote.ForeColor = AppTheme.Muted;
            _bookletNote.Text = "Control number " + typed + " belongs to booklet " + match.SeriesCode
                              + " (" + match.RangeText() + ").";

            IList<ReceiptSeries> all = _receipts.GetSeries(true);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].SeriesId == match.SeriesId)
                {
                    if (_series.SelectedIndex != i) _series.SelectedIndex = i;
                    return;
                }
            }
        }

        // ==================================================================
        //  Writing it
        // ==================================================================

        private void Collect()
        {
            ReceiptSeries series = SelectedSeries();
            string seriesCode = series == null ? string.Empty : series.SeriesCode;
            string orNumber = _orNumber.Text.Trim();
            string controlNumber = _controlNumber.Text.Trim();
            string remarks = _remarks.Text.Trim();
            decimal amount = _amount.Value;
            decimal fee = _request == null ? amount : _request.Fee;

            // Cash, check or online - the three the barangay actually accepts,
            // in the order the enum lists them, so the two can never drift.
            PaymentMethod paymentMethod = (PaymentMethod)Math.Max(_method.SelectedIndex, 0);

            IList<string> problems = InputValidator.ValidatePayment(amount, orNumber, controlNumber,
                fee, seriesCode, _receipts.FindSeriesForControlNumber(controlNumber));

            if (problems.Count > 0)
            {
                Dialog.Warn(this, Dialog.Problems(problems) + Environment.NewLine + Environment.NewLine
                    + "Nothing has been written yet.", "Please check the receipt");
                return;
            }

            string question = "Write OR " + orNumber + " for P" + amount.ToString("#,##0.00")
                + " in booklet " + seriesCode + "?" + Environment.NewLine + Environment.NewLine
                + "Control number: " + controlNumber + Environment.NewLine
                + "Payor: " + _payerName.ToUpperInvariant() + Environment.NewLine
                + (_request == null
                    ? "This is a collection at the counter with no request behind it."
                    : "This settles the fee on " + _request.ReferenceNumber + " ("
                      + _request.GetDocumentName() + ").")
                + Environment.NewLine + Environment.NewLine
                + "Written at " + _moment.ToString("h:mm tt") + " on " + _moment.ToString("dd MMMM yyyy") + ".";

            if (!Dialog.ConfirmChange(this, question)) return;

            Cursor = Cursors.WaitCursor;
            _collect.Enabled = false;

            try
            {
                if (_request == null)
                {
                    OperationResult<OfficialReceipt> issued = _receipts.CollectStandalone(_payerName,
                        amount, seriesCode, orNumber, controlNumber, paymentMethod, remarks);

                    if (!issued.Succeeded) { Dialog.Refused(this, issued.Message); return; }

                    Message = issued.Message;
                }
                else
                {
                    OperationResult<OfficialReceipt> issued = _receipts.Collect(_request, amount,
                        seriesCode, orNumber, controlNumber, paymentMethod, remarks);

                    if (!issued.Succeeded) { Dialog.Refused(this, issued.Message); return; }

                    Message = issued.Message;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (RepositoryException error)
            {
                Dialog.Error(this, error.Message, "The database is not answering");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not write the receipt.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _collect.Enabled = true;
            }
        }
    }
}
