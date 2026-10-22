using System.Windows.Forms;
using System.Drawing;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;
namespace BarangayDocumentSystem.Forms;

/// <summary>
/// A small modal text-input dialog, built entirely in code.
///
/// WinForms has MessageBox for output but no built-in input box, so this
/// fills the gap. Everything is created and wired programmatically to show
/// that the designer is a convenience, not a requirement.
/// </summary>
public static class Prompt
{
    /// <summary>Returns the typed text, or an empty string if cancelled.</summary>
    public static string Show(IWin32Window owner, string title, string question)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(460, 190),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = question,
            Location = new Point(18, 18),
            Size = new Size(424, 46)
        };

        var textBox = new TextBox
        {
            Location = new Point(18, 70),
            Size = new Size(424, 27),
            MaxLength = 200
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(226, 120),
            Size = new Size(104, 38)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(338, 120),
            Size = new Size(104, 38)
        };

        // Controls.Add is what actually puts them on the form — forgetting it
        // is a classic WinForms bug: the control exists but never appears.
        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        form.AcceptButton = ok;    // Enter
        form.CancelButton = cancel; // Esc

        return form.ShowDialog(owner) == DialogResult.OK
            ? textBox.Text.Trim()
            : string.Empty;
    }
}

/// <summary>
/// Sign-in screen, built entirely in code.
///
/// Registration is deliberately HIDDEN: there is no "Register" button or link.
/// Staff reveal the sign-up panel with Ctrl+Shift+R (press it again to go back).
/// On a brand-new database with no accounts at all, the sign-up panel opens by
/// itself so the first account can be created.
/// </summary>
public sealed class LoginForm : Form
{
    private const int FormWidth = 400;
    private const int HeaderHeight = 92;
    private const int SignInBodyHeight = 262;
    private const int RegisterBodyHeight = 440;

    private static readonly Regex UsernamePattern = new(@"^[A-Za-z0-9._]{3,30}$");

    private readonly IUserRepository _users;
    private bool _firstRun;

    private readonly Panel _signInPanel = new();
    private readonly TextBox _loginUser = new();
    private readonly TextBox _loginPass = new();
    private readonly Label _loginMessage = new();
    private readonly Button _signInButton;

    private readonly Panel _registerPanel = new();
    private readonly Label _registerNote = new();
    private readonly TextBox _regName = new();
    private readonly TextBox _regUser = new();
    private readonly TextBox _regPass = new();
    private readonly TextBox _regConfirm = new();
    private readonly Label _registerMessage = new();
    private readonly Button _createButton;
    private readonly LinkLabel _backLink = new();

    /// <summary>The account that signed in; set only when the result is OK.</summary>
    public UserAccount? User { get; private set; }

    public LoginForm(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));

        Text = "Sign in — Barangay System";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(FormWidth, HeaderHeight + SignInBodyHeight);

        Controls.Add(BuildHeader());

        _signInButton = UiFactory.PrimaryButton("Sign in", FormWidth - 64);
        _createButton = UiFactory.PrimaryButton("Create account", FormWidth - 64);

        BuildSignInPanel();
        BuildRegisterPanel();

        Controls.Add(_signInPanel);
        Controls.Add(_registerPanel);

        _registerPanel.Visible = false;
        AcceptButton = _signInButton;

        Load += (_, _) => OnFormLoad();
    }

    // ------------------------------------------------------------ layout

    private static Control BuildHeader()
    {
        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(FormWidth, HeaderHeight),
            BackColor = AppTheme.SidebarBg
        };

        header.Controls.Add(new Label
        {
            Text = "MAGUGPO POBLACION",
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.SidebarActive,
            Location = new Point(32, 22),
            Size = new Size(FormWidth - 64, 18)
        });
        header.Controls.Add(new Label
        {
            Text = "Barangay System",
            Font = AppTheme.PageTitleFont,
            ForeColor = Color.White,
            Location = new Point(32, 42),
            Size = new Size(FormWidth - 64, 38)
        });
        return header;
    }

    private void BuildSignInPanel()
    {
        _signInPanel.Location = new Point(0, HeaderHeight);
        _signInPanel.Size = new Size(FormWidth, SignInBodyHeight);
        _signInPanel.BackColor = AppTheme.Background;

        AddField(_signInPanel, "Username", 24, _loginUser, false);
        AddField(_signInPanel, "Password", 84, _loginPass, true);

        StyleMessage(_loginMessage, new Point(32, 142));
        _signInPanel.Controls.Add(_loginMessage);

        _signInButton.Location = new Point(32, 190);
        _signInButton.Height = 40;
        _signInButton.Click += (_, _) => SignIn();
        _signInPanel.Controls.Add(_signInButton);
    }

    private void BuildRegisterPanel()
    {
        _registerPanel.Location = new Point(0, HeaderHeight);
        _registerPanel.Size = new Size(FormWidth, RegisterBodyHeight);
        _registerPanel.BackColor = AppTheme.Background;

        _registerNote.Location = new Point(32, 14);
        _registerNote.Size = new Size(FormWidth - 64, 36);
        _registerNote.Font = AppTheme.SmallFont;
        _registerNote.ForeColor = AppTheme.TextSecondary;
        _registerPanel.Controls.Add(_registerNote);

        AddField(_registerPanel, "Full name", 56, _regName, false);
        AddField(_registerPanel, "Username (letters, numbers, . or _)", 116, _regUser, false);
        AddField(_registerPanel, "Password (at least 6 characters)", 176, _regPass, true);
        AddField(_registerPanel, "Confirm password", 236, _regConfirm, true);

        StyleMessage(_registerMessage, new Point(32, 298));
        _registerPanel.Controls.Add(_registerMessage);

        _createButton.Location = new Point(32, 342);
        _createButton.Height = 40;
        _createButton.Click += (_, _) => CreateAccount();
        _registerPanel.Controls.Add(_createButton);

        _backLink.Text = "Back to sign in";
        _backLink.Location = new Point(32, 394);
        _backLink.Size = new Size(FormWidth - 64, 22);
        _backLink.TextAlign = ContentAlignment.MiddleCenter;
        _backLink.Font = AppTheme.SmallFont;
        _backLink.LinkColor = AppTheme.Primary;
        _backLink.ActiveLinkColor = AppTheme.PrimaryHover;
        _backLink.LinkClicked += (_, _) => ShowSignIn();
        _registerPanel.Controls.Add(_backLink);
    }

    private static void AddField(Panel panel, string caption, int top, TextBox box, bool password)
    {
        panel.Controls.Add(new Label
        {
            Text = caption,
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(32, top),
            Size = new Size(FormWidth - 64, 18)
        });

        box.Location = new Point(32, top + 20);
        box.Size = new Size(FormWidth - 64, 27);
        box.Font = AppTheme.BodyFont;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = AppTheme.Surface;
        box.UseSystemPasswordChar = password;
        box.MaxLength = password ? 128 : 100;
        panel.Controls.Add(box);
    }

    private static void StyleMessage(Label label, Point location)
    {
        label.Location = location;
        label.Size = new Size(FormWidth - 64, 38);
        label.Font = AppTheme.SmallFont;
        label.ForeColor = AppTheme.Danger;
    }

    // ------------------------------------------------------------ behaviour

    private void OnFormLoad()
    {
        try { _firstRun = !_users.AnyUsers(); }
        catch (RepositoryException ex) { Dialog.Error(ex.Message, "Database problem"); }

        if (_firstRun) ShowRegister();
        else _loginUser.Focus();
    }

    /// <summary>Ctrl+Shift+R toggles the hidden sign-up panel.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.R))
        {
            if (_registerPanel.Visible) { if (!_firstRun) ShowSignIn(); }
            else ShowRegister();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowRegister()
    {
        _registerMessage.Text = string.Empty;
        _registerNote.Text = _firstRun
            ? "No accounts exist yet. Create the first account to get started."
            : "Create a new staff account.";
        _backLink.Visible = !_firstRun;

        _signInPanel.Visible = false;
        _registerPanel.Visible = true;
        ClientSize = new Size(FormWidth, HeaderHeight + RegisterBodyHeight);
        AcceptButton = _createButton;
        _regName.Focus();
    }

    private void ShowSignIn()
    {
        _loginMessage.Text = string.Empty;
        _registerPanel.Visible = false;
        _signInPanel.Visible = true;
        ClientSize = new Size(FormWidth, HeaderHeight + SignInBodyHeight);
        AcceptButton = _signInButton;
        (string.IsNullOrEmpty(_loginUser.Text) ? _loginUser : _loginPass).Focus();
    }

    private void SignIn()
    {
        string username = _loginUser.Text.Trim();
        if (username.Length == 0 || _loginPass.Text.Length == 0)
        {
            _loginMessage.Text = "Enter your username and password.";
            return;
        }

        UserAccount? account;
        try { account = _users.Find(username); }
        catch (RepositoryException ex) { Dialog.Error(ex.Message, "Database problem"); return; }

        // One message for both "no such user" and "wrong password".
        if (account is null || !PasswordHasher.Verify(_loginPass.Text, account.PasswordHash))
        {
            _loginMessage.Text = "Incorrect username or password.";
            _loginPass.Clear();
            _loginPass.Focus();
            return;
        }

        User = account;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void CreateAccount()
    {
        string name = _regName.Text.Trim();
        string username = _regUser.Text.Trim();

        string? problem =
            name.Length == 0 ? "Enter the full name." :
            !UsernamePattern.IsMatch(username) ? "Username must be 3–30 characters: letters, numbers, . or _" :
            _regPass.Text.Length < 6 ? "Password must be at least 6 characters." :
            _regPass.Text != _regConfirm.Text ? "The two passwords do not match." :
            null;

        if (problem is not null) { _registerMessage.Text = problem; return; }

        try
        {
            if (_users.Find(username) is not null)
            {
                _registerMessage.Text = "That username is already taken.";
                return;
            }
            _users.Create(username, name, PasswordHasher.Hash(_regPass.Text));
        }
        catch (RepositoryException ex)
        {
            _registerMessage.Text = ex.Message;
            return;
        }

        _firstRun = false;
        _regName.Clear(); _regPass.Clear(); _regConfirm.Clear(); _regUser.Clear();

        ShowSignIn();
        _loginUser.Text = username;
        _loginPass.Clear();
        _loginPass.Focus();
        Dialog.Info("Account created. You can sign in now.", "Account created");
    }
}

/// <summary>
/// Asks where a resident is transferring to before they are archived.
/// Nothing is deleted — the resident moves to the Archive page.
/// </summary>
public sealed class TransferForm : Form
{
    private readonly TextBox _destination = new();
    private readonly ComboBox _reason = new();
    private readonly string _residentName;

    public string Destination { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;

    public TransferForm(Resident resident)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));
        _residentName = resident.GetFullName();

        Text = "Transfer resident";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(480, 330);

        Controls.Add(new Label
        {
            Text = $"{resident.GetFullName()} will be moved to the Archive.\n" +
                   $"Their record and {resident.Requests.Count} document request(s) are kept — nothing is deleted.",
            Location = new Point(24, 20),
            Size = new Size(432, 48),
            ForeColor = AppTheme.TextPrimary
        });

        Controls.Add(Caption("Transferred to (barangay, city or address)", 80));
        _destination.Location = new Point(24, 100);
        _destination.Size = new Size(432, 27);
        _destination.MaxLength = 255;
        _destination.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(_destination);

        Controls.Add(Caption("Reason", 144));
        _reason.Location = new Point(24, 164);
        _reason.Size = new Size(432, 27);
        _reason.DropDownStyle = ComboBoxStyle.DropDown;
        _reason.MaxLength = 255;
        _reason.Items.AddRange(new object[]
        {
            "Moved to another barangay",
            "Moved to another city / municipality",
            "Employment",
            "Marriage",
            "Studies",
            "Joined family elsewhere"
        });
        Controls.Add(_reason);

        var ok = UiFactory.PrimaryButton("Transfer to archive", 170);
        ok.Location = new Point(286, 262);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(186, 262);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label Caption(string text, int top) => new()
    {
        Text = text,
        Font = AppTheme.SmallBoldFont,
        ForeColor = AppTheme.TextSecondary,
        Location = new Point(24, top),
        Size = new Size(432, 18)
    };

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(_destination.Text))
        {
            Dialog.Warn("Please enter where the resident is transferring to.");
            _destination.Focus();
            return;
        }

        if (!Dialog.Confirm(
                $"Transfer {_residentName} to {_destination.Text.Trim()} and move them to the Archive?",
                "Confirm transfer"))
            return;

        Destination = _destination.Text.Trim();
        Reason = _reason.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}

/// <summary>
/// "Who is this document for?" — lists ACTIVE residents (archived ones are not
/// offered) so the Requests page can start a new document.
/// </summary>
public sealed class ResidentPickerForm : Form
{
    private readonly IBarangayRepository _repository;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _empty = new();

    /// <summary>The chosen resident; set only when the result is OK.</summary>
    public Resident? SelectedResident { get; private set; }

    public ResidentPickerForm(IBarangayRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Text = "New document — choose a resident";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(760, 500);

        _search.Location = new Point(20, 18);
        _search.Size = new Size(720, 27);
        _search.BorderStyle = BorderStyle.FixedSingle;
        CueBanner.Set(_search, "Search by name, purok, or contact number…");
        _search.TextChanged += (_, _) => LoadResidents();
        Controls.Add(_search);

        UiFactory.StyleGrid(_grid);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Choose(); };

        var host = UiFactory.CardHost(_grid);
        host.Location = new Point(20, 58);
        host.Size = new Size(720, 372);
        host.Dock = DockStyle.None;
        Controls.Add(host);

        _empty.Dock = DockStyle.Fill;
        _empty.TextAlign = ContentAlignment.MiddleCenter;
        _empty.ForeColor = AppTheme.TextMuted;
        _empty.BackColor = AppTheme.Surface;
        _empty.Visible = false;
        host.Controls.Add(_empty);
        _empty.BringToFront();

        var choose = UiFactory.PrimaryButton("Continue", 120);
        choose.Location = new Point(620, 448);
        choose.Height = 36;
        choose.Click += (_, _) => Choose();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(522, 448);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(choose);
        Controls.Add(cancel);
        AcceptButton = choose;
        CancelButton = cancel;

        LoadResidents();
    }

    private void LoadResidents()
    {
        _grid.DataSource = _repository.SearchResidents(_search.Text.Trim())
            .Select(r => new
            {
                ID = r.ResidentId,
                Name = r.GetSortableName(),
                Age = r.GetAge(),
                Purok = r.Purok,
                Contact = r.ContactNumber,
                Classification = r.GetClassificationText()
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;

        _empty.Text = string.IsNullOrWhiteSpace(_search.Text)
            ? "No residents registered yet — add one on the Residents page first"
            : $"No residents match “{_search.Text.Trim()}”";
        _empty.Visible = _grid.Rows.Count == 0;
    }

    private void Choose()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id)
        {
            Dialog.SelectFirst("resident");
            return;
        }

        SelectedResident = _repository.Residents.FirstOrDefault(r => r.ResidentId == id);
        if (SelectedResident is null) return;

        DialogResult = DialogResult.OK;
        Close();
    }
}
