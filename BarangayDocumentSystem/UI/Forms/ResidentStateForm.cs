// ---------------------------------------------------------------------------
//  ResidentStateForm.cs - deactivating or archiving a resident.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// Where a resident is taken off the active list - or put back on it.
    ///
    /// This screen exists instead of a Delete button, and it is the whole answer
    /// to the "deactivation / archive" note in the review. Nothing is deleted
    /// from the registry, because certificates already issued point at these
    /// records. Instead:
    ///
    ///   Active    - living in the barangay, counted in the census.
    ///   Inactive  - moved out, passed away, or a duplicate that was merged.
    ///               Not counted any more, but not hidden.
    ///   Archived  - finished with, kept only for the record.
    ///
    /// A reason is required. Six months later, "why is this person inactive?"
    /// is the question the barangay will ask, and the answer has to be on the
    /// record and in the activity log.
    /// </summary>
    public class ResidentStateForm : Form
    {
        private readonly ResidentService _residents;
        private readonly Resident _resident;

        private RadioButton _active;
        private RadioButton _inactive;
        private RadioButton _archived;
        private ComboBox _reason;
        private TextBox _reasonNote;
        private Label _note;
        private Button _confirm;
        private Button _cancel;

        public string Message { get; private set; }

        public ResidentStateForm(ResidentService residents, Resident resident)
        {
            _residents = residents;
            _resident = resident;

            BuildWindow();
            LoadResident();
        }

        private void BuildWindow()
        {
            Text = "Status in the barangay";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(600, 520);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading("Active, inactive or archived");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);

            NameGroupBox nameBox = new NameGroupBox("Resident");
            nameBox.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 44);
            nameBox.Size = new Size(552, 88);
            nameBox.NameText = _resident.GetFullName().ToUpperInvariant();
            nameBox.NoteText = _resident.Purok + "  |  age " + _resident.GetAge()
                             + "  |  currently " + EnumText.Of(_resident.RecordState);

            FlatGroupBox choices = new FlatGroupBox("What should happen");
            choices.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 146);
            choices.Size = new Size(552, 156);

            _active = new RadioButton();
            _active.Text = "Active - living in the barangay, counted in the census";
            _active.Font = AppTheme.Body;
            _active.ForeColor = AppTheme.Ink;
            _active.Location = new Point(12, 28);
            _active.AutoSize = true;

            _inactive = new RadioButton();
            _inactive.Text = "Inactive - moved out, passed away, or a duplicate record";
            _inactive.Font = AppTheme.Body;
            _inactive.ForeColor = AppTheme.Ink;
            _inactive.Location = new Point(12, 58);
            _inactive.AutoSize = true;

            _archived = new RadioButton();
            _archived.Text = "Archived - finished with, kept for the record only";
            _archived.Font = AppTheme.Body;
            _archived.ForeColor = AppTheme.Ink;
            _archived.Location = new Point(12, 88);
            _archived.AutoSize = true;

            _reason = UiFactory.DropDown(new string[]
            {
                "Moved out of the barangay",
                "Passed away",
                "Duplicate of an existing record",
                "Encoded by mistake",
                "Transferred to another barangay",
                "Other - I will type the reason"
            }, false);
            _reason.Location = new Point(12, 118);
            _reason.Width = 300;

            choices.Controls.Add(_active);
            choices.Controls.Add(_inactive);
            choices.Controls.Add(_archived);
            choices.Controls.Add(_reason);

            Label reasonLabel = UiFactory.FieldLabel("Or type the reason here");
            reasonLabel.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 314);
            reasonLabel.Width = 552;

            _reasonNote = UiFactory.TextBox("Type the reason, for example: family moved to Davao City", 200);
            _reasonNote.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 334);
            _reasonNote.Width = 552;

            _note = new Label();
            _note.Font = AppTheme.Small;
            _note.ForeColor = AppTheme.Muted;
            _note.AutoSize = false;
            _note.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 382);
            _note.Size = new Size(552, 44);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = AppTheme.Surface;

            _confirm = UiFactory.PrimaryButton("Save the change");
            _confirm.Width = 180;
            _confirm.Location = new Point(ClientSize.Width - 180 - AppTheme.PageMargin, 15);
            _confirm.Click += ConfirmClicked;

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Width = 110;
            _cancel.Location = new Point(ClientSize.Width - 180 - 110 - AppTheme.PageMargin - AppTheme.Gap2, 15);
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_confirm);
            footer.Controls.Add(_cancel);

            Controls.Add(heading);
            Controls.Add(nameBox);
            Controls.Add(choices);
            Controls.Add(reasonLabel);
            Controls.Add(_reasonNote);
            Controls.Add(_note);
            Controls.Add(footer);

            AcceptButton = _confirm;
            CancelButton = _cancel;
        }

        private void LoadResident()
        {
            switch (_resident.RecordState)
            {
                case RecordState.Inactive: _inactive.Checked = true; break;
                case RecordState.Archived: _archived.Checked = true; break;
                default: _active.Checked = true; break;
            }

            if (!string.IsNullOrWhiteSpace(_resident.StateReason)) _reasonNote.Text = _resident.StateReason;

            _note.Text = "Nothing is deleted. An inactive or archived resident keeps every document already "
                       + "issued to them, and the reason stays on the record and in the activity log.";

            if (_resident.StateChangedOn.HasValue)
                _note.Text += Environment.NewLine + "Last changed "
                            + _resident.StateChangedOn.Value.ToString("dd MMM yyyy") + " by "
                            + _resident.StateChangedBy + ".";
        }

        private RecordState Chosen()
        {
            if (_inactive.Checked) return RecordState.Inactive;
            if (_archived.Checked) return RecordState.Archived;
            return RecordState.Active;
        }

        private string ReasonText()
        {
            string typed = (_reasonNote.Text ?? string.Empty).Trim();
            if (typed.Length > 0) return typed;

            return Convert.ToString(_reason.Text);
        }

        private void ConfirmClicked(object sender, EventArgs e)
        {
            RecordState state = Chosen();
            string reason = ReasonText();

            if (state == _resident.RecordState)
            {
                Dialog.Warn(this, _resident.GetFullName() + " is already "
                    + EnumText.Of(state).ToLowerInvariant() + ". Nothing has changed.", "No change needed");
                return;
            }

            if (state != RecordState.Active && string.IsNullOrWhiteSpace(reason))
            {
                Dialog.Warn(this, "Please say why - the reason is what the barangay will read "
                    + "six months from now.", "A reason is needed");
                _reason.Focus();
                return;
            }

            string question = state == RecordState.Active
                ? "Put " + _resident.GetFullName() + " back on the active list of the barangay?"
                : "Mark " + _resident.GetFullName() + " as " + EnumText.Of(state).ToLowerInvariant()
                  + " in the barangay?" + Environment.NewLine + Environment.NewLine
                  + "Reason: " + reason;

            if (!Dialog.ConfirmChange(this, question)) return;

            Cursor = Cursors.WaitCursor;
            _confirm.Enabled = false;

            try
            {
                OperationResult result = _residents.ChangeState(_resident, state, reason);

                if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

                Message = result.Message;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Interfaces.RepositoryException error)
            {
                Dialog.Error(this, error.Message, "The database is not answering");
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not change the resident's status.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _confirm.Enabled = true;
            }
        }
    }
}
