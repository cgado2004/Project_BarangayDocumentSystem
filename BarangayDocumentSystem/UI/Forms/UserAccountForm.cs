// ---------------------------------------------------------------------------
//  UserAccountForm.cs - creating or editing a staff account.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Controls;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// Where a staff account is created or changed.
    ///
    /// This form is the replacement for the sign-up screen the barangay asked
    /// me to remove, and that is the right way round: an account should be
    /// created by somebody accountable, who is signed in, and whose name ends
    /// up in the activity log next to the account.
    ///
    /// Two decisions worth reading:
    ///
    ///  * The new account is told to change its password at the first sign-in,
    ///    so the administrator knows the first password but not the one the
    ///    person settles on.
    ///  * The role is picked from three, and the sentence under it says what
    ///    the role can do in plain words - a clerk, the punong barangay, or the
    ///    administrator. Nobody should be guessing what a role means.
    /// </summary>
    public class UserAccountForm : Form
    {
        private readonly UserService _users;
        private readonly UserAccount _account;
        private readonly bool _isNew;

        private TextBox _username;
        private TextBox _fullName;
        private ComboBox _role;
        private TextBox _position;
        private TextBox _password;
        private TextBox _confirmation;
        private CheckBox _mustChange;
        private Label _roleNote;
        private Button _save;
        private Button _cancel;

        public string Message { get; private set; }

        public UserAccountForm(UserService users, UserAccount account)
        {
            _users = users;
            _account = account;
            _isNew = account == null;

            BuildWindow();
            LoadAccount();
        }

        private void BuildWindow()
        {
            Text = _isNew ? "New staff account" : "Account of " + _account.FullName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(620, 620);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Label heading = UiFactory.PageHeading(_isNew ? "New staff account" : "Staff account");
            heading.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4);

            Label caption = UiFactory.Caption(
                _isNew
                    ? "There is no self-registration: accounts are created here by the administrator."
                    : "Changing an account is written into the activity log with your name against it.");
            caption.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 30);
            caption.Width = 560;

            FlatGroupBox who = new FlatGroupBox("Who");
            who.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 60);
            who.Size = new Size(556, 130);

            _fullName = UiFactory.TextBox("e.g. Maria Santos", 120);
            _username = UiFactory.TextBox("e.g. m.santos", 50);
            _position = UiFactory.TextBox("e.g. Barangay Secretary", 80);

            AddField(who, "Full name", _fullName, 12, 26, 260);
            AddField(who, "User name", _username, 284, 26, 260);
            AddField(who, "Position", _position, 12, 74, 532);

            FlatGroupBox role = new FlatGroupBox("What the role can do");
            role.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 204);
            role.Size = new Size(556, 104);

            _role = UiFactory.DropDown(new string[]
            {
                "Clerk - counter work: residents, requests, collections, census",
                "Punong Barangay - validates and approves, reads every report",
                "Administrator - manages the system, accounts and settings"
            }, true);
            _role.Location = new Point(12, 46);
            _role.Width = 532;
            _role.SelectedIndexChanged += delegate (object sender, EventArgs e) { RoleChanged(); };

            _roleNote = new Label();
            _roleNote.Font = AppTheme.Small;
            _roleNote.ForeColor = AppTheme.Muted;
            _roleNote.AutoSize = false;
            _roleNote.Location = new Point(12, 76);
            _roleNote.Size = new Size(532, 22);

            Label roleLabel = UiFactory.FieldLabel("Role");
            roleLabel.Location = new Point(12, 26);
            roleLabel.Width = 532;

            role.Controls.Add(roleLabel);
            role.Controls.Add(_role);
            role.Controls.Add(_roleNote);

            FlatGroupBox password = new FlatGroupBox(_isNew ? "The first password" : "Password");
            password.Location = new Point(AppTheme.PageMargin, AppTheme.Gap4 + 322);
            password.Size = new Size(556, 156);

            _password = UiFactory.TextBox(_isNew ? "give a starting password" : "leave blank to keep the current one", 100);
            _confirmation = UiFactory.TextBox("type it again", 100);
            _password.UseSystemPasswordChar = true;
            _confirmation.UseSystemPasswordChar = true;

            AddField(password, "Password", _password, 12, 26, 260);
            AddField(password, "Password again", _confirmation, 284, 26, 260);

            _mustChange = UiFactory.CheckBox(
                "Ask this person to change the password at the first sign-in", _isNew && AppConfig.ForcePasswordChangeOnFirstLogin);
            _mustChange.Location = new Point(12, 80);
            _mustChange.Font = AppTheme.Small;
            _mustChange.ForeColor = AppTheme.Muted;
            password.Controls.Add(_mustChange);

            Label rules = new Label();
            rules.Font = AppTheme.Small;
            rules.ForeColor = AppTheme.MutedSoft;
            rules.AutoSize = false;
            rules.Location = new Point(12, 106);
            rules.Size = new Size(532, 34);
            rules.Text = "At least " + AppConfig.MinimumPasswordLength
                       + " characters. The program stores a scramble of the password, not the password itself, "
                       + "so nobody - not even the administrator - can read it back.";
            password.Controls.Add(rules);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 64;
            footer.BackColor = AppTheme.Surface;

            _save = UiFactory.PrimaryButton(_isNew ? "Create the account" : "Save the changes");
            _save.Width = 190;
            _save.Location = new Point(ClientSize.Width - 190 - AppTheme.PageMargin, 15);
            _save.Click += delegate (object sender, EventArgs e) { Save(); };

            _cancel = UiFactory.SecondaryButton("Cancel");
            _cancel.Width = 100;
            _cancel.Location = new Point(ClientSize.Width - 190 - 100 - AppTheme.PageMargin - AppTheme.Gap2, 15);
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

        private static void AddField(Control parent, string caption, Control control, int x, int y, int width)
        {
            Label label = UiFactory.FieldLabel(caption);
            label.Location = new Point(x, y);
            label.Width = width;

            control.Location = new Point(x, y + 20);
            control.Width = width;

            parent.Controls.Add(label);
            parent.Controls.Add(control);
        }

        // ==================================================================
        //  Loading
        // ==================================================================

        private void LoadAccount()
        {
            if (_isNew)
            {
                _role.SelectedIndex = 0;
                RoleChanged();
                return;
            }

            _fullName.Text = _account.FullName;
            _username.Text = _account.Username;
            _username.ReadOnly = true;          // a user name is written in the log; it does not change
            _position.Text = _account.Position;
            _role.SelectedIndex = (int)_account.Role;
            _mustChange.Checked = _account.NeedsPasswordChange();

            RoleChanged();
        }

        private void RoleChanged()
        {
            switch (_role.SelectedIndex)
            {
                case 1:
                    _roleNote.Text = "Can validate and clear requests, release documents, and read every report "
                                   + "and the activity log. Cannot encode residents or collect money.";
                    break;
                case 2:
                    _roleNote.Text = "Everything the other roles can do, plus accounts, receipt booklets and "
                                   + "settings - and can see the reason behind a fee.";
                    break;
                default:
                    _roleNote.Text = "Counter work: residents, filing requests, collecting fees, the census. "
                                   + "Cannot void a receipt or manage accounts.";
                    break;
            }
        }

        // ==================================================================
        //  Saving
        // ==================================================================

        private void Save()
        {
            UserRole role = (UserRole)Math.Max(_role.SelectedIndex, 0);

            IList<string> problems = InputValidator.ValidateUser(_username.Text, _fullName.Text,
                EnumText.Of(role), _isNew, _password.Text, _confirmation.Text);

            if (problems.Count > 0)
            {
                Dialog.Warn(this, InputValidator.Describe(problems) + Environment.NewLine + Environment.NewLine
                    + "Nothing has been saved yet.", "Please check the account");
                return;
            }

            string question = _isNew
                ? "Create the account of " + _fullName.Text.Trim() + " (" + EnumText.Of(role) + ")?"
                : "Save the changes to " + _account.FullName + "'s account?";
            question += _isNew
                ? Environment.NewLine + Environment.NewLine + "User name: " + _username.Text.Trim()
                : string.Empty;

            if (_isNew || _password.Text.Length > 0)
                question += Environment.NewLine + "The password will " + (_isNew ? "be set" : "be replaced") + ".";

            if (!Dialog.ConfirmChange(this, question)) return;

            Cursor = Cursors.WaitCursor;
            _save.Enabled = false;

            try
            {
                OperationResult result;

                if (_isNew)
                {
                    OperationResult<UserAccount> created = _users.CreateUser(_username.Text, _fullName.Text, role,
                        _position.Text, _password.Text, _confirmation.Text, _mustChange.Checked);

                    result = created;
                    if (created.Succeeded) Message = created.Message;
                }
                else
                {
                    OperationResult updated = _users.UpdateUser(_account, _fullName.Text, role, _position.Text);
                    if (!updated.Succeeded) { Dialog.Refused(this, updated.Message); return; }

                    // A blank password box means "leave the password alone" -
                    // which is what most edits are.
                    if (_password.Text.Length > 0)
                    {
                        OperationResult reset = _users.ResetPassword(_account, _password.Text, _confirmation.Text);
                        if (!reset.Succeeded) { Dialog.Refused(this, reset.Message); return; }
                    }

                    result = updated;
                    Message = updated.Message;
                }

                if (!result.Succeeded) { Dialog.Refused(this, result.Message); return; }

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
}
