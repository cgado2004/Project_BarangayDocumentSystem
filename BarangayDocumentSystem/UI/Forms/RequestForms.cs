// ---------------------------------------------------------------------------
//  RequestForms.cs - the new request, and the confirmation before it is filed.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.Services.Documents;
using BarangayDocumentSystem.UI.Controls;
using BarangayDocumentSystem.UI.Dialogs;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The "New request" window - the replacement for the old "New Document"
    /// entry in the menu.
    ///
    /// Why the name changed and why it matters: a document is not something the
    /// barangay invents on its own. A resident asks for it, and the request is
    /// the thing that gets a reference number, a fee, a status and a history.
    /// Naming the screen after the request keeps that straight from the first
    /// click.
    ///
    /// Everything the form does until the last button is a rehearsal: the fee
    /// and the starting status shown at the bottom come from the real rules,
    /// and the same rules are run again inside the save. The fee shown is an
    /// amount, never the sentence of law behind it - the reason is recorded on
    /// the request and readable by the administrator, which is what the review
    /// asked for.
    /// </summary>
    public class NewRequestForm : Form
    {
        private readonly RequestService _requests;
        private readonly ResidentService _residents;
        private readonly IClock _clock;

        private Resident _resident;
        private GroupBox _residentBox;
        private Label _residentNote;

        private ComboBox _document;
        private ComboBox _scope;
        private ComboBox _purpose;
        private TextBox _purposeOther;

        private GroupBox _business;
        private TextBox _businessName;
        private TextBox _businessNature;
        private ComboBox _businessPurok;
        private TextBox _businessLocation;
        private ComboBox _businessOwnership;
        private TextBox _registration;
        private TextBox _employees;
        private CheckBox _renewal;

        private NumericUpDown _hours;
        private NumericUpDown _income;
        private TextBox _detail;
        private CheckBox _jobseeker;
        private CheckBox _needsValidation;

        private Label _feeLine;
        private Label _statusLine;
        private Label _timeLine;
        private List<string> _summaryLines = new List<string>();

        public string Message { get; private set; }

        public NewRequestForm(RequestService requests, ResidentService residents, IClock clock)
        {
            _requests = requests;
            _residents = residents;
            _clock = clock;

            BuildWindow();
            ShowResident();
            Rehearse();
        }

        // ==================================================================
        //  The window
        // ==================================================================

        private void BuildWindow()
        {
            Text = "New request";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(900, 680);
            ClientSize = new Size(940, 760);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Panel page = UiFactory.Page();
            page.Dock = DockStyle.Fill;

            Label heading = UiFactory.PageHeading("New request");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption("Choose who is asking and what they need. The fee and the "
                + "status below come from the barangay's own rules, and nothing is written until you confirm.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(880, 34);

            // ---- 1. who is asking ----
            _residentBox = new GroupBox();
            _residentBox.Text = "Resident";
            _residentBox.Font = AppTheme.SmallBold;
            _residentBox.ForeColor = AppTheme.Primary;
            _residentBox.BackColor = AppTheme.Surface;
            _residentBox.FlatStyle = FlatStyle.Flat;
            _residentBox.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 76);
            _residentBox.Size = new Size(880, 104);
            _residentBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Button choose = UiFactory.PrimaryButton("Choose the resident");
            choose.Location = new Point(AppTheme.Gap3, 34);
            choose.Click += delegate (object sender, EventArgs e) { ChooseResident(); };
            _residentBox.Controls.Add(choose);

            _residentNote = UiFactory.Hint("Nobody chosen yet. Please choose the resident who is asking.");
            _residentNote.Location = new Point(AppTheme.Gap3 + choose.Width + AppTheme.Gap3, 40);
            _residentNote.Size = new Size(600, 46);
            _residentBox.Controls.Add(_residentNote);

            // ---- 2. what is being asked for ----
            GroupBox what = Card("Document", AppTheme.PageMargin + 192);

            _document = UiFactory.DropDown(DocumentNames(), true);
            _document.SelectedIndexChanged += delegate (object sender, EventArgs e) { DocumentChanged(); };

            _scope = UiFactory.DropDown(new string[] { "For use here in the Philippines", "For use abroad" }, true);
            _scope.SelectedIndexChanged += delegate (object sender, EventArgs e) { Rehearse(); };

            _purpose = UiFactory.DropDown(new string[]
            {
                "Employment", "Scholarship", "Financial assistance", "Passport or travel",
                "Business requirement", "School requirement", "Bank requirement", "Other - I will type it"
            }, true);
            _purpose.SelectedIndexChanged += delegate (object sender, EventArgs e)
            {
                _purposeOther.Visible = _purpose.SelectedIndex == _purpose.Items.Count - 1;
                Rehearse();
            };

            _purposeOther = UiFactory.TextBox("type the purpose", 120);
            _purposeOther.Visible = false;
            _purposeOther.TextChanged += delegate (object sender, EventArgs e) { Rehearse(); };

            Place(what, "Document", _document, AppTheme.Gap3, 34, 340);
            Place(what, "Scope", _scope, AppTheme.Gap3 + 352, 34, 230);
            Place(what, "Purpose", _purpose, AppTheme.Gap3 + 594, 34, 270);

            _purposeOther.Location = new Point(AppTheme.Gap3 + 594, 90);
            _purposeOther.Width = 270;
            what.Controls.Add(_purposeOther);
            what.Height = 126;

            // ---- 3. the business, only when a clearance for one is asked for ----
            _business = Card("The business", AppTheme.PageMargin + 336);
            _business.Enabled = false;

            _businessName = UiFactory.TextBox("registered or known business name", 120);
            _businessNature = UiFactory.TextBox("e.g. sari-sari store, eatery", 120);
            _businessPurok = UiFactory.DropDown(PurokList.ForDropDown(), true);
            _businessLocation = UiFactory.TextBox("e.g. beside the covered court", 120);
            _businessOwnership = UiFactory.DropDown(new string[] { "Sole owner", "Partnership", "Corporation", "Cooperative" }, true);
            _registration = UiFactory.TextBox("DTI, SEC or CDA number", 40);
            _employees = UiFactory.TextBox("how many", 6);
            _renewal = UiFactory.CheckBox("This is a renewal of last year's clearance", false);

            Place(_business, "Business name", _businessName, AppTheme.Gap3, 34, 300);
            Place(_business, "What the business does", _businessNature, AppTheme.Gap3 + 312, 34, 250);
            Place(_business, "Purok", _businessPurok, AppTheme.Gap3 + 574, 34, 140);
            Place(_business, "Ownership", _businessOwnership, AppTheme.Gap3 + 726, 34, 140);
            Place(_business, "Registration number", _registration, AppTheme.Gap3, 88, 220);
            Place(_business, "Workers", _employees, AppTheme.Gap3 + 232, 88, 100);
            Place(_business, "Where exactly", _businessLocation, AppTheme.Gap3 + 344, 88, 300);

            _renewal.Location = new Point(AppTheme.Gap3 + 656, 96);
            _business.Controls.Add(_renewal);
            _business.Height = 150;

            // ---- 4. the odd details, shown only when the document needs them ----
            GroupBox extras = Card("Details", AppTheme.PageMargin + 498);

            _hours = new NumericUpDown();
            _hours.Minimum = 1;
            _hours.Maximum = 12;
            _hours.Value = 2;
            _hours.Font = AppTheme.Body;

            _income = new NumericUpDown();
            _income.Minimum = 0;
            _income.Maximum = 100000000m;
            _income.ThousandsSeparator = true;
            _income.Increment = 1000m;
            _income.Font = AppTheme.Body;

            _detail = UiFactory.TextBox("anything that must appear on the paper", 200);

            Place(extras, "Hours of use (facility)", _hours, AppTheme.Gap3, 34, 200);
            Place(extras, "Gross yearly income (cedula)", _income, AppTheme.Gap3 + 212, 34, 230);
            Place(extras, "Other detail to print", _detail, AppTheme.Gap3 + 454, 34, 410);

            _jobseeker = UiFactory.CheckBox("First-time jobseeker - free of charge under RA 11261", false);
            _jobseeker.Location = new Point(AppTheme.Gap3, 92);
            _jobseeker.CheckedChanged += delegate (object sender, EventArgs e) { Rehearse(); };
            extras.Controls.Add(_jobseeker);

            _needsValidation = UiFactory.CheckBox("This request has to be checked first", false);
            _needsValidation.Location = new Point(AppTheme.Gap3 + 400, 92);
            _needsValidation.CheckedChanged += delegate (object sender, EventArgs e) { Rehearse(); };
            extras.Controls.Add(_needsValidation);
            extras.Height = 128;

            // ---- 5. what will happen ----
            SectionPanel outcome = new SectionPanel("What will happen",
                "Read from the fee schedule and the office clock - not typed in by hand.");
            outcome.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 640);
            outcome.Size = new Size(880, 150);
            outcome.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _feeLine = new Label();
            _feeLine.Font = new Font(AppTheme.UiFamily, 13f, FontStyle.Bold);
            _feeLine.ForeColor = AppTheme.Ink;
            _feeLine.Location = new Point(AppTheme.Gap4, 58);
            _feeLine.AutoSize = true;
            outcome.Controls.Add(_feeLine);

            _statusLine = UiFactory.Body(string.Empty);
            _statusLine.Location = new Point(AppTheme.Gap4, 90);
            _statusLine.Size = new Size(840, 22);
            outcome.Controls.Add(_statusLine);

            _timeLine = UiFactory.Hint(string.Empty);
            _timeLine.Location = new Point(AppTheme.Gap4, 112);
            _timeLine.Size = new Size(840, 22);
            outcome.Controls.Add(_timeLine);

            page.Controls.Add(heading);
            page.Controls.Add(caption);
            page.Controls.Add(_residentBox);
            page.Controls.Add(what);
            page.Controls.Add(_business);
            page.Controls.Add(extras);
            page.Controls.Add(outcome);

            // ---- the buttons ----
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            Button file = UiFactory.PrimaryButton("Review and file the request");
            file.Width = 250;
            file.Location = new Point(ClientSize.Width - 250 - AppTheme.PageMargin, 12);
            file.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            file.Click += delegate (object sender, EventArgs e) { File(); };

            Button cancel = UiFactory.SecondaryButton("Cancel");
            cancel.Location = new Point(file.Left - cancel.Width - AppTheme.Gap2, 12);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(file);
            footer.Controls.Add(cancel);

            Controls.Add(page);
            Controls.Add(footer);

            AcceptButton = file;
            CancelButton = cancel;

            _purpose.SelectedIndex = 0;
            DocumentChanged();
        }

        private static GroupBox Card(string title, int top)
        {
            GroupBox box = new GroupBox();
            box.Text = title;
            box.Font = AppTheme.SmallBold;
            box.ForeColor = AppTheme.Primary;
            box.BackColor = AppTheme.Surface;
            box.FlatStyle = FlatStyle.Flat;
            box.Location = new Point(AppTheme.PageMargin, top);
            box.Size = new Size(880, 132);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return box;
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

        /// <summary>The document list comes from the templates themselves, so a
        /// document that has no wording cannot be chosen here - it simply is not
        /// on the list. That is why the enum and the paper can never drift
        /// apart.</summary>
        private static string[] DocumentNames()
        {
            List<string> names = new List<string>();

            foreach (DocumentType type in (DocumentType[])Enum.GetValues(typeof(DocumentType)))
                if (DocumentTemplateRegistry.HasTemplate(type)) names.Add(EnumText.Spaced(type.ToString()));

            return names.ToArray();
        }

        private DocumentType ChosenType()
        {
            string chosen = Convert.ToString(_document.SelectedItem);
            if (string.IsNullOrEmpty(chosen)) return DocumentType.BarangayClearance;

            foreach (DocumentType type in (DocumentType[])Enum.GetValues(typeof(DocumentType)))
                if (string.Equals(EnumText.Spaced(type.ToString()), chosen, StringComparison.OrdinalIgnoreCase))
                    return type;

            return DocumentType.BarangayClearance;
        }

        // ==================================================================
        //  Reacting to what is chosen
        // ==================================================================

        private void DocumentChanged()
        {
            bool business = DocumentTemplateRegistry.RequiresBusinessDetails(ChosenType());

            _business.Enabled = business;

            if (!business && _business.Visible) _business.Visible = false;
            _business.Visible = business;

            foreach (Control control in _business.Controls)
                control.Visible = true;

            Rehearse();
        }

        private void ChooseResident()
        {
            using (ResidentPickerForm picker = new ResidentPickerForm(_residents))
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;

                _resident = picker.Selected;
                ShowResident();
                Rehearse();
            }
        }

        private void ShowResident()
        {
            if (_resident == null)
            {
                _residentNote.Text = "Nobody chosen yet. Please choose the resident who is asking.";
                return;
            }

            _residentNote.Text = _resident.GetFullName().ToUpperInvariant()
                + "   -   " + _resident.GetAge() + " years old, " + _resident.Purok
                + Environment.NewLine + "Classification: " + _resident.GetClassificationText()
                + ".    Fee category: " + _resident.GetFeeCategoryText() + ".";
        }

        // ==================================================================
        //  The rehearsal
        // ==================================================================

        private DocumentRequest Gather()
        {
            DocumentRequest request = new DocumentRequest();

            request.DocumentType = ChosenType();
            request.Scope = _scope.SelectedIndex == 1 ? ClearanceScope.Abroad : ClearanceScope.Local;

            string purpose = _purpose.SelectedIndex == _purpose.Items.Count - 1
                ? _purposeOther.Text
                : Convert.ToString(_purpose.SelectedItem);
            request.Purpose = (purpose ?? string.Empty).Trim();

            request.Hours = _hours.Value;
            request.GrossAnnualIncome = _income.Value;
            request.Detail = _detail.Text.Trim();
            request.ApplyJobseekerWaiver = _jobseeker.Checked;
            request.RequiresValidation = _needsValidation.Checked;

            if (DocumentTemplateRegistry.RequiresBusinessDetails(request.DocumentType))
            {
                BusinessDetails details = new BusinessDetails();
                details.BusinessName = _businessName.Text.Trim();
                details.NatureOfBusiness = _businessNature.Text.Trim();
                details.Purok = Convert.ToString(_businessPurok.SelectedItem);
                details.LocationNote = _businessLocation.Text.Trim();
                details.OwnershipType = Convert.ToString(_businessOwnership.SelectedItem);
                details.RegistrationNumber = _registration.Text.Trim();
                details.IsRenewal = _renewal.Checked;

                int workers;
                if (int.TryParse(_employees.Text, out workers)) details.EmployeeCount = workers;

                request.Business = details;
            }

            return request;
        }

        /// <summary>
        /// Runs the real rules and shows the answer. Nothing is saved, and
        /// nothing here can promise something the save will not do, because
        /// this calls the very method the save calls.
        /// </summary>
        private void Rehearse()
        {
            if (_resident == null)
            {
                _feeLine.Text = "Choose the resident to see the fee.";
                _statusLine.Text = string.Empty;
                _timeLine.Text = string.Empty;
                _summaryLines.Clear();
                return;
            }

            try
            {
                DocumentRequest request = Gather();

                RequestPreview preview = _requests.Preview(_resident, request, _needsValidation.Checked);

                _feeLine.Text = preview.Assessment.IsFree
                    ? "Fee: free - " + preview.Assessment.Category
                    : "Fee: P" + preview.Assessment.FinalFee.ToString("#,##0.00")
                      + (preview.Assessment.StudentDiscountApplied ? " (student rate applied)" : string.Empty);

                _statusLine.Text = "Starting status: " + EnumText.Of(preview.StartingStatus) + " - "
                                 + preview.StatusExplanation;

                _timeLine.Text = preview.TimeExplanation;

                _needsValidation.Checked = request.RequiresValidation;
                _needsValidation.Enabled = request.DocumentType != DocumentType.BarangayBusinessClearance;

                _summaryLines = new List<string>();
                foreach (string line in preview.GetLines()) _summaryLines.Add(line);
            }
            catch (Exception error)
            {
                _feeLine.Text = "The fee could not be worked out.";
                _statusLine.Text = error.Message;
                _timeLine.Text = string.Empty;
            }
        }

        // ==================================================================
        //  Filing
        // ==================================================================

        private void File()
        {
            if (_resident == null)
            {
                Dialog.Warn(this, "Please choose the resident who is asking for the document.",
                    "No resident chosen");
                return;
            }

            DocumentRequest request = Gather();

            IList<string> problems = InputValidator.ValidateRequest(_resident, request, _needsValidation.Checked);
            if (problems.Count > 0)
            {
                Dialog.Warn(this, Dialog.Problems(problems) + Environment.NewLine + Environment.NewLine
                    + "Nothing has been filed yet.", "Please check the request");
                return;
            }

            // The confirmation: the same lines the rehearsal produced, in a
            // window of their own, with the two buttons the clerk expects.
            using (ConfirmationForm confirm = new ConfirmationForm(
                "File this request?",
                Lines(request),
                "File the request",
                "Go back and change something"))
            {
                if (confirm.ShowDialog(this) != DialogResult.OK) return;
            }

            Cursor = Cursors.WaitCursor;

            try
            {
                OperationResult<DocumentRequest> result =
                    _requests.FileRequest(_resident, request, _needsValidation.Checked);

                if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

                Message = result.Message;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (RepositoryException error)
            {
                Dialog.Error(this, error.Message, "The database is not answering");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not file the request.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private List<string> Lines(DocumentRequest request)
        {
            RequestPreview preview = _requests.Preview(_resident, request, _needsValidation.Checked);

            List<string> lines = new List<string>();
            foreach (string line in preview.GetLines()) lines.Add(line);

            lines.Add(string.Empty);
            lines.Add("The fee shown is the whole amount. The reason behind an amount, and any waiver, "
                    + "is recorded on the request and in the activity log - it is not printed on the paper.");
            lines.Add("Filed at " + _clock.Now().ToString("h:mm tt") + " on "
                    + _clock.Now().ToString("dd MMMM yyyy") + ".");

            return lines;
        }
    }

    /// <summary>
    /// The confirmation window.
    ///
    /// I wrote one of these and use it for everything that writes to the
    /// database, so the shape of "are you sure?" is always the same: what is
    /// about to happen, in a list, and two buttons that say what they do.
    /// "Yes" and "No" make people stop and think about which is which; "File the
    /// request" does not.
    /// </summary>
    public class ConfirmationForm : Form
    {
        public ConfirmationForm(string question, IList<string> lines, string okText, string cancelText)
        {
            Text = question;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 560);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading(question);
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);
            heading.MaximumSize = new Size(560, 0);

            Label caption = UiFactory.Caption("Please read this before it is written. The same "
                + "information will be on the record.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 42);
            caption.Size = new Size(560, 21);

            TextBox body = new TextBox();
            body.Multiline = true;
            body.ReadOnly = true;
            body.ScrollBars = ScrollBars.Vertical;
            body.BorderStyle = BorderStyle.FixedSingle;
            body.BackColor = AppTheme.Surface;
            body.ForeColor = AppTheme.Ink;
            body.Font = new Font(AppTheme.UiFamily, 10.5f, FontStyle.Regular);
            body.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 74);
            body.Size = new Size(560, 380);
            body.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            StringBuilder text = new StringBuilder();
            foreach (string line in lines)
            {
                text.AppendLine(line);
                text.AppendLine();
            }
            body.Text = text.ToString().TrimEnd();

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            Button ok = UiFactory.PrimaryButton(okText);
            ok.Width = 200;
            ok.Location = new Point(ClientSize.Width - 200 - AppTheme.PageMargin, 12);
            ok.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ok.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.OK; Close(); };

            Button cancel = UiFactory.SecondaryButton(cancelText);
            cancel.Width = 200;
            cancel.Location = new Point(ok.Left - 200 - AppTheme.Gap2, 12);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(ok);
            footer.Controls.Add(cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(body);
            Controls.Add(footer);

            AcceptButton = ok;
            CancelButton = cancel;

            Shown += delegate (object sender, EventArgs e) { cancel.Focus(); };
        }
    }
}
