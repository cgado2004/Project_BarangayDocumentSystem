#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Forms;

public sealed class LoginForm : Form
{
    private readonly Func<string, string, bool> _authenticate;
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Label _error = new();

    public string SignedInUser { get; private set; } = string.Empty;

    public LoginForm(Func<string, string, bool> authenticate)
    {
        _authenticate = authenticate ?? throw new ArgumentNullException(nameof(authenticate));
        Text = "Sign in";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 390);
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        AutoScaleMode = AutoScaleMode.Font;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = AppTheme.SidebarBg,
            Padding = new Padding(32, 16, 32, 10)
        };
        var brand = new Label
        {
            Dock = DockStyle.Top,
            Height = 21,
            Text = "MAGUGPO POBLACION",
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.SidebarActive
        };
        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Sign in to Barangay System",
            Font = AppTheme.SubheadFont,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title);
        header.Controls.Add(brand);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            BackColor = AppTheme.Surface,
            Padding = new Padding(32, 22, 32, 18)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        foreach (int height in new[] { 24, 35, 13, 24, 35, 28, 42, 48 })
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        body.Controls.Add(FieldLabel("Username"), 0, 0);
        _username.Name = "username";
        _username.Dock = DockStyle.Fill;
        _username.Font = AppTheme.BodyFont;
        _username.MaxLength = 80;
        _username.Margin = new Padding(0);
        body.Controls.Add(_username, 0, 1);

        body.Controls.Add(FieldLabel("Password"), 0, 3);
        _password.Name = "password";
        _password.Dock = DockStyle.Fill;
        _password.Font = AppTheme.BodyFont;
        _password.UseSystemPasswordChar = true;
        _password.Margin = new Padding(0);
        body.Controls.Add(_password, 0, 4);

        var showPassword = new CheckBox
        {
            Name = "showPassword",
            Text = "Show password",
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0)
        };
        showPassword.CheckedChanged += (_, _) =>
            _password.UseSystemPasswordChar = !showPassword.Checked;
        body.Controls.Add(showPassword, 0, 5);

        _error.Name = "loginError";
        _error.Dock = DockStyle.Fill;
        _error.ForeColor = AppTheme.Danger;
        _error.TextAlign = ContentAlignment.MiddleLeft;
        _error.Margin = new Padding(0);
        body.Controls.Add(_error, 0, 6);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0)
        };
        var signIn = UiFactory.PrimaryButton("Sign in", 120);
        signIn.Name = "signIn";
        signIn.Click += (_, _) => TrySignIn();
        var cancel = UiFactory.SecondaryButton("Cancel", 100);
        cancel.Name = "cancel";
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        actions.Controls.Add(signIn);
        actions.Controls.Add(cancel);
        body.Controls.Add(actions, 0, 7);

        AcceptButton = signIn;
        CancelButton = cancel;
        Controls.Add(body);
        Controls.Add(header);
    }

    private static Label FieldLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        Font = AppTheme.BodyBoldFont,
        ForeColor = AppTheme.TextPrimary,
        Margin = new Padding(0)
    };

    private void TrySignIn()
    {
        string username = _username.Text.Trim();
        string password = _password.Text;
        _error.Text = string.Empty;

        if (username.Length == 0)
        {
            _error.Text = "Enter your username.";
            _username.Focus();
            return;
        }

        if (password.Length == 0)
        {
            _error.Text = "Enter your password.";
            _password.Focus();
            return;
        }

        if (!_authenticate(username, password))
        {
            _error.Text = "Username or password is incorrect.";
            _password.SelectAll();
            _password.Focus();
            return;
        }

        SignedInUser = username;
        _password.Clear();
        DialogResult = DialogResult.OK;
        Close();
    }
}
