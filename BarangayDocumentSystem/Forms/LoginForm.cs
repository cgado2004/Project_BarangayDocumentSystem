#nullable enable
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

public sealed class LoginForm : Form
{
    private readonly IUserRepository _users;
    private readonly TextBox _username = new() { Name = "username", MaxLength = 30 };
    private readonly TextBox _password = new() { Name = "password", MaxLength = 128, UseSystemPasswordChar = true };
    private readonly TextBox _fullName = new() { Name = "fullName", MaxLength = 100 };
    private readonly TextBox _confirm = new() { Name = "confirmPassword", MaxLength = 128, UseSystemPasswordChar = true };
    private readonly Label _nameLabel = new();
    private readonly Label _confirmLabel = new();
    private readonly Label _title = new();
    private readonly Label _message = new();
    private readonly Button _submit = UiFactory.PrimaryButton("Sign in");
    private readonly Timer _retryTimer = new() { Interval = 1000 };
    private bool _setup;
    private bool _busy;
    private bool _available;
    private int _failedAttempts;
    private DateTime _retryAfter;

    public UserAccount? User { get; private set; }

    public LoginForm(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        Text = "Sign in — Barangay System";
        Font = AppTheme.BodyFont;
        BackColor = AppTheme.Background;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 610);
        MinimumSize = new Size(380, 400);
        MaximizeBox = false;

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1,
            Padding = new Padding(28), Margin = Padding.Empty
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _title.Text = "Sign in";
        _title.Font = AppTheme.HeadingFont;
        Add(fields, _title);
        AddField(fields, _nameLabel, "Full name", _fullName);
        AddField(fields, new Label(), "Username", _username);
        AddField(fields, new Label(), "Password", _password);
        AddField(fields, _confirmLabel, "Confirm password", _confirm);
        _message.ForeColor = AppTheme.TextSecondary;
        Add(fields, _message);
        _submit.Name = "submit";
        _submit.Dock = DockStyle.Top;
        _submit.AutoSize = true;
        _submit.MinimumSize = new Size(0, 42);
        _submit.Margin = new Padding(0, 18, 0, 0);
        AddRow(fields, _submit);
        scroll.Controls.Add(fields);
        Controls.Add(scroll);
        AcceptButton = _submit;
        SetMode(false);
        _submit.Enabled = false;
        _submit.Click += async (_, _) => await Submit();
        _retryTimer.Tick += (_, _) => UpdateSubmit();
        _retryTimer.Start();
        Shown += async (_, _) => await CheckSetup();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private static void Add(TableLayoutPanel panel, Label label)
    {
        label.AutoSize = true;
        label.Dock = DockStyle.Top;
        label.Margin = new Padding(0, 12, 0, 6);
        AddRow(panel, label);
    }

    private static void AddField(TableLayoutPanel panel, Label label, string text, TextBox field)
    {
        label.Text = text;
        Add(panel, label);
        field.Dock = DockStyle.Top;
        field.Margin = new Padding(0, 0, 0, 8);
        AddRow(panel, field);
    }

    private static void AddRow(TableLayoutPanel panel, Control control)
    {
        int row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(control, 0, row);
    }

    private void SetMode(bool setup)
    {
        _setup = setup;
        _nameLabel.Visible = _fullName.Visible = _confirmLabel.Visible = _confirm.Visible = setup;
        _title.Text = setup ? "Set up the first staff account" : "Sign in";
        _submit.Text = setup ? "Finish setup" : "Sign in";
        _message.Text = setup
            ? "First use only. Choose a password with 15–128 characters. A long passphrase works well."
            : "Use your staff account. Contact the system administrator if you need access.";
        _password.Clear();
        _confirm.Clear();
        ClientSize = new Size(ClientSize.Width, (int)((setup ? 610 : 410) * DeviceDpi / 96F));
        if (IsHandleCreated) FitWindow();
    }

    private void FitWindow()
    {
        var area = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
    }

    private async Task CheckSetup()
    {
        FitWindow();
        _busy = true;
        try
        {
            SetMode(!await Task.Run(() => _users.AnyUsers()));
            _available = true;
        }
        catch (RepositoryException)
        {
            _message.Text = "Sign-in is unavailable. Contact the system administrator and try again later.";
        }
        finally { _busy = false; UpdateSubmit(); }
    }

    private void UpdateSubmit() =>
        _submit.Enabled = _available && !_busy && DateTime.UtcNow >= _retryAfter;

    private async Task Submit()
    {
        if (!_available || _busy || DateTime.UtcNow < _retryAfter) return;
        string username = _username.Text.Trim();
        string password = _password.Text;
        if (username.Length == 0 || password.Length == 0)
        {
            _message.Text = "Enter your username and password.";
            return;
        }
        if (_setup && (_fullName.Text.Trim().Length == 0
            || !Regex.IsMatch(username, @"\A[A-Za-z0-9._]{3,30}\z")
            || password.Length < 15 || password.Length > 128 || password != _confirm.Text))
        {
            _message.Text = "Enter your name, a 3–30 character username (letters, digits, . or _), and matching passwords of 15–128 characters.";
            return;
        }

        _busy = true;
        UpdateSubmit();
        _password.Clear();
        _confirm.Clear();
        try
        {
            if (_setup)
            {
                string name = _fullName.Text.Trim();
                bool created = await Task.Run(() =>
                {
                    if (_users.AnyUsers()) return false;
                    _users.CreateFirst(username, name, PasswordHasher.Hash(password));
                    return true;
                });
                SetMode(false);
                _message.Text = created ? "Account created. Sign in to continue." : "Setup is complete. Sign in with an existing staff account.";
                _password.Focus();
                return;
            }

            var account = await Task.Run(() =>
            {
                var found = _users.Find(username);
                return found is not null && PasswordHasher.Verify(password, found.PasswordHash) ? found : null;
            });
            if (account is null)
            {
                _failedAttempts++;
                if (_failedAttempts >= 5)
                {
                    _failedAttempts = 0;
                    _retryAfter = DateTime.UtcNow.AddSeconds(30);
                    _message.Text = "Too many attempts. Wait 30 seconds, then try again.";
                }
                else _message.Text = "Incorrect username or password.";
                _password.Focus();
                return;
            }
            User = account;
            _busy = false;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (RepositoryException)
        {
            _message.Text = "Unable to complete sign-in or setup. Contact the system administrator and try again later.";
        }
        finally { _busy = false; if (!IsDisposed) UpdateSubmit(); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _retryTimer.Dispose();
        base.Dispose(disposing);
    }
}
