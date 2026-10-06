// ---------------------------------------------------------------------------
//  AccountForms.cs - the account screens: a person's account, my own password,
//  and the receipt booklets.
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
using BarangayDocumentSystem.UI.Dialogs;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// Creating or changing one staff account.
    ///
    /// This window is the replacement for the sign-up screen the barangay asked
    /// me to remove, and that is the right way round: an account should be
    /// created by somebody accountable, who is signed in, and whose name ends up
    /// in the activity log next to the account.
    ///
    /// Two decisions worth reading. The new account is told to change its
    /// password at the first sign-in, so the administrator knows the first
    /// password but not the one the person finally settles on. And the role is
    /// described in plain words under the drop-down, because nobody should have
    /// to guess what "Punong Barangay" can do in a program.
    /// </summary>
    public class UserForm : Form
    {
        private readonly UserService _users;
        private readonly UserAccount _account;
        private readonly bool _isNew;

        private TextBox _fullName;
        private TextBox _username;
        private TextBox _position;
        private ComboBox _role;
        private Label _roleNote;
        private TextBox _password;
        private TextBox _confirmation;
        private CheckBox _mustChange;
        private CheckBox _isActive;
        private Button _save;
        private Button _cancel;

        public string Message { get; private set; }

        public UserForm(UserService users, UserAccount account)
        {
            _users = users;
            _account = account;
            _isNew = account == null;

            BuildWindow();
            Load();
        }

        private void BuildWindow()
        {
            Text = _isNew ? "New staff account" : "Account of " + _account.FullName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(600, 620);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading(_isNew ? "New staff account" : "Staff account");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption(_isNew
                ? "There is no self-registration. Accounts are made here, by whoever is signed in, and the "
                  + "activity log records who made them."
                : "The role decides which screens this person can open. The program checks the role again "
                  + "before it writes anything, so hiding a screen is not the protection - it is only good "
                  + "manners.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(540, 44);

            GroupBox who = Card("Who", AppTheme.PageMargin + 90);

            _fullName = UiFactory.TextBox("e.g. Maria Santos", 120);
            _username = UiFactory.TextBox("e.g. m.santos", 50);
            _position = UiFactory.TextBox("e.g. Barangay Secretary", 80);

            Place(who, "Full name", _fullName, AppTheme.Gap3, 30, 250);
            Place(who, "User name", _username, AppTheme.Gap3 + 262, 30, 250);
            Place(who, "Position", _position, AppTheme.Gap3, 84, 512);

            GroupBox role = Card("What the role can do", AppTheme.PageMargin + 232);

            _role = UiFactory.DropDown(RoleNames(), true);
            _role.SelectedIndexChanged += delegate (object sender, EventArgs e) { RoleChanged(); };

            Place(role, "Role", _role, AppTheme.Gap3, 30, 512);

            _roleNote = UiFactory.Hint(string.Empty);
            _roleNote.Location = new Point(AppTheme.Gap3, 84);
            _roleNote.Size = new Size(512, 44);
            role.Controls.Add(_roleNote);

            GroupBox password = Card(_isNew ? "The first password" : "Password", AppTheme.PageMargin + 374);

            _password = UiFactory.TextBox(_isNew ? "give a first password" : "leave blank to keep it as it is", 100);
            _confirmation = UiFactory.TextBox("type it again", 100);
            _password.UseSystemPasswordChar = true;
            _confirmation.UseSystemPasswordChar = true;

            Place(password, "Password", _password, AppTheme.Gap3, 30, 250);
            Place(password, "Password again", _confirmation, AppTheme.Gap3 + 262, 30, 250);

            _mustChange = UiFactory.CheckBox("Ask for the password to be changed at the first sign-in", _isNew);
            _mustChange.Location = new Point(AppTheme.Gap3, 84);

            _isActive = UiFactory.CheckBox("This account can sign in", true);
            _isActive.Location = new Point(AppTheme.Gap3 + 300, 84);
            _isActive.Margin = new Padding(0);

            password.Controls.Add(_mustChange);
            password.Controls.Add(_isActive);

            Label rules = UiFactory.Hint("At least " + AppConfig.MinimumPasswordLength + " characters. The "
                + "program keeps a scramble of the password, never the password, so nobody - not even the "
                + "administrator - can read it back.");
            rules.Location = new Point(AppTheme.Gap3, 108);
            rules.Size = new Size(512, 34);
            password.Controls.Add(rules);
            password.Height = 156;

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            _save = UiFactory.PrimaryButton(_isNew ? "Create the account" : "Save the changes");
            _save.Width = 190;
            _save.Location = new Point(ClientSize.Width - 190 - AppTheme.PageMargin, 12);
            _save.Click += delegate (object sender, EventArgs e) { Save(); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Location = new Point(_save.Left - _cancel.Width - AppTheme.Gap2, 12);
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_save);
            footer.Controls.Add(_cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(who);
            Controls.Add(role);
            Controls.Add(password);
            Controls.Add(footer);

            AcceptButton = _save;
            CancelButton = _cancel;
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
            box.Size = new Size(540, 132);
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

        /// <summary>The three roles, in the order the enum lists them, so the
        /// code and the drop-down can never drift apart.</summary>
        private static string[] RoleNames()
        {
            List<string> names = new List<string>();

            foreach (UserRole role in (UserRole[])Enum.GetValues(typeof(UserRole)))
                names.Add(EnumText.Of(role));

            return names.ToArray();
        }

        private void Load()
        {
            _role.SelectedIndex = 0;

            if (_isNew) { RoleChanged(); return; }

            _fullName.Text = _account.FullName;
            _username.Text = _account.Username;
            _position.Text = _account.Position;
            _role.SelectedIndex = (int)_account.Role;
            _mustChange.Checked = _account.NeedsPasswordChange();
            _isActive.Checked = _account.IsActive;

            RoleChanged();
        }

        private void RoleChanged()
        {
            UserRole role = (UserRole)Math.Max(_role.SelectedIndex, 0);

            _roleNote.Text = PermissionSet.ExplainRole(role);
        }

        private void Save()
        {
            UserRole role = (UserRole)Math.Max(_role.SelectedIndex, 0);

            IList<string> problems = InputValidator.ValidateUser(_username.Text, _fullName.Text,
                EnumText.Of(role), _isNew, _password.Text, _confirmation.Text);

            if (problems.Count > 0)
            {
                Dialog.Warn(this, Dialog.Problems(problems) + Environment.NewLine + Environment.NewLine
                    + "Nothing has been saved yet.", "Please check the account");
                return;
            }

            string question = _isNew
                ? "Create the account of " + _fullName.Text.Trim() + " as " + EnumText.Of(role) + "?"
                : "Save the changes to " + _account.FullName + "'s account?";

            question += Environment.NewLine + Environment.NewLine + PermissionSet.ExplainRole(role);
            if (_password.Text.Length > 0)
                question += Environment.NewLine + "The password will be " + (_isNew ? "set" : "replaced") + ".";

            if (!Dialog.ConfirmChange(this, question)) return;

            Cursor = Cursors.WaitCursor;
            _save.Enabled = false;

            try
            {
                if (_isNew)
                {
                    OperationResult<UserAccount> created = _users.CreateUser(_username.Text, _fullName.Text,
                        role, _position.Text, _password.Text, _confirmation.Text, _mustChange.Checked);

                    if (!created.Succeeded) { Dialog.Refused(this, created.Message); return; }

                    Message = created.Message;
                }
                else
                {
                    OperationResult updated = _users.UpdateUser(_account, _fullName.Text, role, _position.Text);
                    if (!updated.Succeeded) { Dialog.Refused(this, updated.Message); return; }

                    // A blank password box means "leave the password alone",
                    // which is what most edits are.
                    if (_password.Text.Length > 0)
                    {
                        OperationResult reset = _users.ResetPassword(_account, _password.Text, _confirmation.Text);
                        if (!reset.Succeeded) { Dialog.Refused(this, reset.Message); return; }
                    }

                    if (_isActive.Checked != _account.IsActive)
                    {
                        OperationResult active = _users.SetActive(_account, _isActive.Checked);
                        if (!active.Succeeded) { Dialog.Refused(this, active.Message); return; }
                    }

                    Message = updated.Message;
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
                Dialog.FromException(this, "I could not save the account.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _save.Enabled = true;
            }
        }
    }

    /// <summary>
    /// Changing your own password.
    ///
    /// The old password is asked for even though the person is already signed
    /// in: a computer left unattended at the counter should not be enough for
    /// somebody else to lock the real owner out of their own account.
    ///
    /// The rules are shown before they are broken, not after.
    /// </summary>
    public class ChangePasswordForm : Form
    {
        private readonly AuthenticationService _authentication;
        private readonly UserAccount _account;

        private TextBox _current;
        private TextBox _fresh;
        private TextBox _confirmation;
        private Button _save;
        private Button _cancel;

        /// <summary>True when this is the first sign-in of a new account, where
        /// changing the password is not optional.</summary>
        public bool IsFirstSignIn { get; set; }

        public string Message { get; private set; }

        public ChangePasswordForm(AuthenticationService authentication, UserAccount account)
        {
            _authentication = authentication;
            _account = account;

            BuildWindow();
        }

        private void BuildWindow()
        {
            Text = "Change my password";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(560, 470);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading("Change my password");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption("Signed in as " + (_account == null ? "-" : _account.FullName)
                + ". The new password has to have at least " + AppConfig.MinimumPasswordLength
                + " characters, and cannot be the one already in use.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(500, 44);

            GroupBox boxes = new GroupBox();
            boxes.Text = "Passwords";
            boxes.Font = AppTheme.SmallBold;
            boxes.ForeColor = AppTheme.Primary;
            boxes.BackColor = AppTheme.Surface;
            boxes.FlatStyle = FlatStyle.Flat;
            boxes.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 90);
            boxes.Size = new Size(500, 220);

            _current = UiFactory.TextBox("the password in use now", 100);
            _fresh = UiFactory.TextBox("the new password", 100);
            _confirmation = UiFactory.TextBox("the new password again", 100);

            _current.UseSystemPasswordChar = true;
            _fresh.UseSystemPasswordChar = true;
            _confirmation.UseSystemPasswordChar = true;

            Place(boxes, "Current password", _current, AppTheme.Gap3, 30, 472);
            Place(boxes, "New password", _fresh, AppTheme.Gap3, 84, 472);
            Place(boxes, "New password again", _confirmation, AppTheme.Gap3, 138, 472);

            CheckBox show = UiFactory.CheckBox("Show the passwords while I type", false);
            show.Location = new Point(AppTheme.Gap3, 190);
            show.Font = AppTheme.Small;
            show.ForeColor = AppTheme.Muted;
            show.CheckedChanged += delegate (object sender, EventArgs e)
            {
                _current.UseSystemPasswordChar = !show.Checked;
                _fresh.UseSystemPasswordChar = !show.Checked;
                _confirmation.UseSystemPasswordChar = !show.Checked;
            };
            boxes.Controls.Add(show);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            _save = UiFactory.PrimaryButton("Save the new password");
            _save.Width = 200;
            _save.Location = new Point(ClientSize.Width - 200 - AppTheme.PageMargin, 12);
            _save.Click += delegate (object sender, EventArgs e) { Save(); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Location = new Point(_save.Left - _cancel.Width - AppTheme.Gap2, 12);
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_save);
            footer.Controls.Add(_cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(boxes);
            Controls.Add(footer);

            AcceptButton = _save;

            // On the first sign-in there is nothing to cancel back to, so the
            // Cancel button is taken away rather than left there to be pressed.
            if (!IsFirstSignIn) CancelButton = _cancel;
            else _cancel.Enabled = false;

            Shown += delegate (object sender, EventArgs e) { _current.Focus(); };
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

        private void Save()
        {
            IList<string> problems = InputValidator.ValidatePassword(_fresh.Text, _confirmation.Text);
            if (problems.Count > 0)
            {
                Dialog.Warn(this, Dialog.Problems(problems), "Please check the new password");
                _fresh.Focus();
                return;
            }

            if (string.IsNullOrEmpty(_current.Text))
            {
                Dialog.Warn(this, "Please type the password you are using now.", "The current password is needed");
                _current.Focus();
                return;
            }

            try
            {
                OperationResult result = _authentication.ChangeOwnPassword(_account, _current.Text,
                    _fresh.Text, _confirmation.Text);

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
                Dialog.FromException(this, "I could not change the password.", error);
            }
        }
    }

    /// <summary>
    /// Recording a receipt booklet.
    ///
    /// A collection points at a booklet, so the booklet has to be on file first.
    /// The window asks for the three things printed on the cover: the series
    /// code, the first control number and the last one - and it shows the range
    /// back, so a mistyped range is caught while the booklet is still in the
    /// person's hand.
    /// </summary>
    public class ReceiptSeriesForm : Form
    {
        private readonly ReceiptService _receipts;

        private TextBox _series;
        private TextBox _from;
        private TextBox _to;
        private TextBox _issuedTo;
        private Label _preview;
        private Button _save;
        private Button _cancel;

        public string Message { get; private set; }

        public ReceiptSeriesForm(ReceiptService receipts)
        {
            _receipts = receipts;

            Text = "Record a receipt booklet";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(560, 440);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading("Record a receipt booklet");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin);

            Label caption = UiFactory.Caption("Copy the cover of the booklet. The control numbers are what "
                + "let a collection be traced back to a real form, so they have to be right.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 34);
            caption.Size = new Size(500, 44);

            GroupBox booklet = new GroupBox();
            booklet.Text = "The booklet";
            booklet.Font = AppTheme.SmallBold;
            booklet.ForeColor = AppTheme.Primary;
            booklet.BackColor = AppTheme.Surface;
            booklet.FlatStyle = FlatStyle.Flat;
            booklet.Location = new Point(AppTheme.PageMargin, AppTheme.PageMargin + 90);
            booklet.Size = new Size(500, 200);

            _series = UiFactory.TextBox("e.g. A, or BPS-2026", 20);
            _from = UiFactory.TextBox("first control number", 40);
            _to = UiFactory.TextBox("last control number", 40);
            _issuedTo = UiFactory.TextBox("who keeps this booklet", 120);

            Place(booklet, "Series code", _series, AppTheme.Gap3, 30, 200);
            Place(booklet, "First control number", _from, AppTheme.Gap3 + 212, 30, 150);
            Place(booklet, "Last control number", _to, AppTheme.Gap3 + 374, 30, 150);
            Place(booklet, "Issued to", _issuedTo, AppTheme.Gap3, 84, 472);

            _series.TextChanged += delegate (object sender, EventArgs e) { Refresh(); };
            _from.TextChanged += delegate (object sender, EventArgs e) { Refresh(); };
            _to.TextChanged += delegate (object sender, EventArgs e) { Refresh(); };

            _preview = UiFactory.Hint(string.Empty);
            _preview.Location = new Point(AppTheme.Gap3, 138);
            _preview.Size = new Size(472, 44);
            booklet.Controls.Add(_preview);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 60;
            footer.BackColor = AppTheme.Surface;

            _save = UiFactory.PrimaryButton("Record the booklet");
            _save.Width = 190;
            _save.Location = new Point(ClientSize.Width - 190 - AppTheme.PageMargin, 12);
            _save.Click += delegate (object sender, EventArgs e) { Save(); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Location = new Point(_save.Left - _cancel.Width - AppTheme.Gap2, 12);
            _cancel.Click += delegate (object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.Add(_save);
            footer.Controls.Add(_cancel);

            Controls.Add(heading);
            Controls.Add(caption);
            Controls.Add(booklet);
            Controls.Add(footer);

            AcceptButton = _save;
            CancelButton = _cancel;

            Refresh();
            Shown += delegate (object sender, EventArgs e) { _series.Focus(); };
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

        private void Refresh()
        {
            if (string.IsNullOrWhiteSpace(_series.Text))
            {
                _preview.Text = "Please copy the series code from the cover of the booklet.";
                return;
            }

            _preview.Text = "Booklet " + _series.Text.Trim() + " will cover control numbers "
                          + (string.IsNullOrWhiteSpace(_from.Text) ? "?" : _from.Text.Trim()) + " to "
                          + (string.IsNullOrWhiteSpace(_to.Text) ? "?" : _to.Text.Trim()) + ".";
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_series.Text) || string.IsNullOrWhiteSpace(_from.Text)
                || string.IsNullOrWhiteSpace(_to.Text))
            {
                Dialog.Warn(this, "Please fill in the series code and both control numbers.", "Please check");
                return;
            }

            if (!Dialog.ConfirmChange(this, "Record booklet " + _series.Text.Trim() + " covering "
                + _from.Text.Trim() + " to " + _to.Text.Trim() + "?")) return;

            try
            {
                OperationResult<ReceiptSeries> result = _receipts.AddSeries(_series.Text, _from.Text,
                    _to.Text, _issuedTo.Text);

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
                Dialog.FromException(this, "I could not record the booklet.", error);
            }
        }
    }
}
