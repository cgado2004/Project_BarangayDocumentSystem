// ---------------------------------------------------------------------------
//  LoginForm.cs - the door into the system.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.UI.Dialogs;

namespace BarangayDocumentSystem.UI.Forms
{
    /// <summary>
    /// The sign-in window.
    ///
    /// There is no "Register" button on this window, and that is on purpose:
    /// the barangay asked for sign-up to be removed. It is also the safe
    /// choice - if anybody could create an account, anybody could read the
    /// residents' records. Accounts are created by the administrator on the
    /// User Accounts screen, and that is the only place they exist.
    ///
    /// The window also checks the database before it lets anybody type a
    /// password. If MySQL or SQL Server is not running, the person is told
    /// that, instead of being handed a login box that refuses a correct
    /// password for a reason that has nothing to do with them.
    /// </summary>
    public class LoginForm : Form
    {
        private readonly AuthenticationService _authentication;
        private readonly IBarangayRepository _repository;

        private TextBox _username;
        private TextBox _password;
        private Button _signIn;
        private Label _status;
        private Label _attempts;

        public UserAccount SignedInUser { get; private set; }

        public LoginForm(AuthenticationService authentication, IBarangayRepository repository)
        {
            _authentication = authentication;
            _repository = repository;

            BuildWindow();
        }

        private void BuildWindow()
        {
            Text = "Sign in - " + AppConfig.BarangayName + " Document System";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(860, 520);
            BackColor = AppTheme.Canvas;
            Font = AppTheme.Body;

            Panel artwork = new Panel();
            artwork.Dock = DockStyle.Left;
            artwork.Width = 380;
            artwork.Paint += PaintArtwork;

            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = AppTheme.Canvas;
            card.Padding = new Padding(AppTheme.Gap5, AppTheme.Gap5, AppTheme.Gap5, AppTheme.Gap4);

            Label heading = UiFactory.PageHeading("Sign in");
            heading.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 64);

            Label caption = UiFactory.Caption("Use the account the barangay administrator gave you. "
                + "If you do not have one, ask at the barangay hall.");
            caption.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 104);
            caption.Size = new Size(380, 40);

            _username = UiFactory.TextBox("your user name", 50);
            _username.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 168);
            _username.Width = 380;
            _username.Font = new Font(AppTheme.UiFamily, 11f, FontStyle.Regular);

            _password = UiFactory.TextBox("your password", 100);
            _password.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 236);
            _password.Width = 380;
            _password.Font = new Font(AppTheme.UiFamily, 11f, FontStyle.Regular);
            _password.UseSystemPasswordChar = true;

            _signIn = UiFactory.PrimaryButton("Sign in");
            _signIn.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 300);
            _signIn.Width = 380;
            _signIn.Height = 42;
            _signIn.Click += SignInClicked;

            CheckBox show = UiFactory.CheckBox("Show the password while I type", false);
            show.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 352);
            show.Font = AppTheme.Small;
            show.ForeColor = AppTheme.Muted;
            show.CheckedChanged += delegate (object sender, EventArgs e)
            {
                _password.UseSystemPasswordChar = !show.Checked;
            };

            _attempts = UiFactory.Hint(string.Empty);
            _attempts.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 386);
            _attempts.Size = new Size(380, 42);
            _attempts.ForeColor = AppTheme.Danger;

            _status = UiFactory.Hint(string.Empty);
            _status.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 428);
            _status.Size = new Size(380, 44);

            card.Controls.Add(heading);
            card.Controls.Add(caption);
            card.Controls.Add(_username);
            card.Controls.Add(_password);
            card.Controls.Add(show);
            card.Controls.Add(_signIn);
            card.Controls.Add(_attempts);
            card.Controls.Add(_status);

            Label userLabel = UiFactory.FieldLabel("User name");
            userLabel.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 148);
            userLabel.Width = 380;

            Label passLabel = UiFactory.FieldLabel("Password");
            passLabel.Location = new Point(AppTheme.Gap5 + AppTheme.Gap4, 216);
            passLabel.Width = 380;

            card.Controls.Add(userLabel);
            card.Controls.Add(passLabel);

            Controls.Add(card);
            Controls.Add(artwork);

            AcceptButton = _signIn;

            Shown += delegate (object sender, EventArgs e)
            {
                CheckDatabase();
                _username.Focus();
            };
        }

        /// <summary>
        /// The left half: the barangay seal and the name of the barangay.
        /// The seal is the same file every printed document uses; it is drawn
        /// here and not altered in any way.
        /// </summary>
        private void PaintArtwork(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            AppTheme.Smooth(e.Graphics);

            using (LinearGradientBrush wash = new LinearGradientBrush(
                panel.ClientRectangle, AppTheme.Navy, AppTheme.Primary, LinearGradientMode.ForwardDiagonal))
                e.Graphics.FillRectangle(wash, panel.ClientRectangle);

            try
            {
                string path = AppConfig.LogoFile;
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(AppConfig.ApplicationFolder, path);

                if (System.IO.File.Exists(path))
                {
                    using (Image logo = Image.FromFile(path))
                    {
                        int size = 148;
                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(logo, (panel.Width - size) / 2, 72, size, size);
                    }
                }
            }
            catch (Exception error)
            {
                AppLog.Warn("The seal could not be shown on the sign-in window: " + error.Message);
            }

            using (Brush white = new SolidBrush(Color.White))
            using (Brush soft = new SolidBrush(Color.FromArgb(0xCC, 0xDA, 0xF2)))
            using (Brush gold = new SolidBrush(AppTheme.Gold))
            using (Font title = new Font(AppTheme.UiFamily, 15f, FontStyle.Bold))
            using (Font place = new Font(AppTheme.UiFamily, 9.5f, FontStyle.Regular))
            using (StringFormat centred = new StringFormat())
            {
                centred.Alignment = StringAlignment.Center;

                e.Graphics.DrawString("Barangay Document System", title, white,
                    new RectangleF(0, 240, panel.Width, 30), centred);
                e.Graphics.DrawString(AppConfig.BarangayName, place, soft,
                    new RectangleF(0, 274, panel.Width, 24), centred);
                e.Graphics.DrawString(AppConfig.CityName + ", " + AppConfig.ProvinceName, place, soft,
                    new RectangleF(0, 296, panel.Width, 24), centred);

                e.Graphics.FillRectangle(gold, (panel.Width - 60) / 2, 332, 60, 3);

                e.Graphics.DrawString("Office hours: " + AppConfig.OfficeHours, place, soft,
                    new RectangleF(0, 350, panel.Width, 24), centred);
                e.Graphics.DrawString("Documents are cleared on the spot between "
                    + AppConfig.OfficeWindowStart.ToString(@"h\:mm") + " and "
                    + AppConfig.OfficeWindowEnd.ToString(@"h\:mm") + ".", AppTheme.Small, soft,
                    new RectangleF(24, 374, panel.Width - 48, 40), centred);
            }
        }

        // ==================================================================
        //  Checking the database, and signing in
        // ==================================================================

        private void CheckDatabase()
        {
            Cursor = Cursors.WaitCursor;

            try
            {
                _status.ForeColor = AppTheme.Muted;
                _status.Text = _repository.Describe();
                _signIn.Enabled = true;
            }
            catch (Exception error)
            {
                _status.ForeColor = AppTheme.Danger;
                _status.Text = "The barangay database cannot be opened just now. Please tell whoever "
                             + "looks after the computer, and show them this: " + error.Message;
                _signIn.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SignInClicked(object sender, EventArgs e)
        {
            _attempts.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_username.Text))
            {
                _attempts.Text = "Please type your user name.";
                _username.Focus();
                return;
            }

            if (string.IsNullOrEmpty(_password.Text))
            {
                _attempts.Text = "Please type your password.";
                _password.Focus();
                return;
            }

            Cursor = Cursors.WaitCursor;
            _signIn.Enabled = false;

            try
            {
                OperationResult<UserAccount> result = _authentication.SignIn(_username.Text, _password.Text);

                if (!result.Succeeded)
                {
                    _attempts.Text = result.Message;
                    _password.SelectAll();
                    _password.Focus();
                    return;
                }

                SignedInUser = result.Value;

                // The office asked for the password to be changed on the first
                // sign-in, so I do not let anybody past this point with the
                // administrator's first password still in place.
                if (SignedInUser.NeedsPasswordChange())
                {
                    using (ChangePasswordForm form = new ChangePasswordForm(_authentication, SignedInUser))
                    {
                        form.IsFirstSignIn = true;

                        if (form.ShowDialog(this) != DialogResult.OK)
                        {
                            _authentication.SignOut(SignedInUser);
                            SignedInUser = null;
                            _attempts.Text = "The password has to be changed before you can use the system.";
                            _password.Text = string.Empty;
                            return;
                        }
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (RepositoryException error)
            {
                Dialog.Error(this, error.Message, "The database is not answering");
                CheckDatabase();
            }
            catch (Exception error)
            {
                Dialog.FromException(this, "I could not finish the sign-in.", error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _signIn.Enabled = true;
            }
        }
    }
}
