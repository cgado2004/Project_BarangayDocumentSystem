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








public static class Prompt
{

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



        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        DialogLayout.MakeScrollable(form);

        return form.ShowDialog(owner) == DialogResult.OK
            ? textBox.Text.Trim()
            : string.Empty;
    }
}





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





public sealed class ResidentPickerForm : Form
{
    private readonly IBarangayRepository _repository;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _empty = new();


    public Resident? SelectedResident { get; private set; }

    public ResidentPickerForm(IBarangayRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        Text = "New Request — choose a resident";
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

        DialogLayout.MakeScrollable(this);
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

public sealed class CreateAccountForm : Form
{
    private readonly IUserRepository _users;
    private readonly TextBox _fullName = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();

    public CreateAccountForm(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));

        Text = "Create staff account";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(480, 292);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);

        int top = 24;
        AddField("Full name", ref top, _fullName, 100);
        AddField("Username", ref top, _username, 30);
        AddField("Temporary password (at least 6 characters)", ref top, _password, 128);
        _password.UseSystemPasswordChar = true;

        var ok = UiFactory.PrimaryButton("Create account", 120);
        ok.Location = new Point(336, 236);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 120);
        cancel.Location = new Point(208, 236);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AddField(string caption, ref int top, TextBox box, int maxLength)
    {
        Controls.Add(new Label
        {
            Text = caption,
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, top),
            Size = new Size(432, 18)
        });
        top += 20;
        box.Location = new Point(24, top);
        box.Size = new Size(432, 27);
        box.MaxLength = maxLength;
        box.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(box);
        top += 42;
    }

    private void Submit()
    {
        string name = _fullName.Text.Trim();
        string username = _username.Text.Trim();
        string password = _password.Text;

        if (name.Length == 0)
        {
            Dialog.Warn("Enter the staff member's full name.");
            _fullName.Focus();
            return;
        }
        if (!Regex.IsMatch(username, @"\A[A-Za-z0-9._]{3,30}\z"))
        {
            Dialog.Warn("The username must be 3–30 characters using letters, digits, dots or underscores.");
            _username.Focus();
            return;
        }
        if (password.Length < 6 || password.Length > 128)
        {
            Dialog.Warn("The temporary password must be 6–128 characters.");
            _password.Focus();
            return;
        }

        try
        {
            _users.Create(username, name, PasswordHasher.Hash(password), UserRole.Staff);
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        Dialog.Info($"Staff account “{username}” was created.\nShare the username and temporary password with them.");
        DialogResult = DialogResult.OK;
        Close();
    }
}

public sealed class AccountEditForm : Form
{
    private readonly IUserRepository _users;
    private readonly int _userId;
    private readonly TextBox _fullName = new();
    private readonly TextBox _username = new();
    private readonly ComboBox _role = new();

    public AccountEditForm(IUserRepository users, UserAccount account, bool allowRoleChange, bool lockUsername)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _userId = account.UserId;

        Text = "Edit account";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);

        int top = 24;
        AddField("Full name", ref top, _fullName, 100);
        AddField("Username", ref top, _username, 30);
        _fullName.Text = account.FullName;
        _username.Text = account.Username;
        _username.ReadOnly = lockUsername;
        if (lockUsername) _username.ForeColor = AppTheme.TextMuted;

        if (allowRoleChange)
        {
            Controls.Add(Caption("Role", top));
            top += 20;
            _role.Location = new Point(24, top);
            _role.Size = new Size(432, 27);
            _role.DropDownStyle = ComboBoxStyle.DropDownList;
            _role.Items.AddRange(new object[] { UserRole.Staff, UserRole.Admin });
            _role.SelectedItem = account.Role;
            Controls.Add(_role);
            top += 42;
        }

        int buttonTop = top + 6;
        ClientSize = new Size(480, buttonTop + 58);

        var ok = UiFactory.PrimaryButton("Save", 100);
        ok.Location = new Point(356, buttonTop);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(258, buttonTop);
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

    private void AddField(string caption, ref int top, TextBox box, int maxLength)
    {
        Controls.Add(Caption(caption, top));
        top += 20;
        box.Location = new Point(24, top);
        box.Size = new Size(432, 27);
        box.MaxLength = maxLength;
        box.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(box);
        top += 42;
    }

    private void Submit()
    {
        string name = _fullName.Text.Trim();
        string username = _username.Text.Trim();
        if (name.Length == 0 || !Regex.IsMatch(username, @"\A[A-Za-z0-9._]{3,30}\z"))
        {
            Dialog.Warn("Enter a full name and a 3–30 character username (letters, digits, . or _).");
            return;
        }

        UserRole role = _role.SelectedItem is UserRole r ? r : UserRole.Staff;
        try
        {
            _users.Update(_userId, username, name, role);
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}

public sealed class ChangePasswordForm : Form
{
    private readonly IUserRepository _users;
    private readonly int _userId;
    private readonly string _currentHash;
    private readonly TextBox _current = new();
    private readonly TextBox _password = new();
    private readonly TextBox _confirm = new();

    public ChangePasswordForm(IUserRepository users, UserAccount account)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _userId = account.UserId;
        _currentHash = account.PasswordHash;

        Text = "Change password";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);

        int top = 24;
        AddField("Current password", ref top, _current, 128);
        AddField("New password (at least 6 characters)", ref top, _password, 128);
        AddField("Confirm new password", ref top, _confirm, 128);
        _current.UseSystemPasswordChar = true;
        _password.UseSystemPasswordChar = true;
        _confirm.UseSystemPasswordChar = true;

        int buttonTop = top + 6;
        ClientSize = new Size(480, buttonTop + 58);

        var ok = UiFactory.PrimaryButton("Change password", 160);
        ok.Location = new Point(296, buttonTop);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(198, buttonTop);
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

    private void AddField(string caption, ref int top, TextBox box, int maxLength)
    {
        Controls.Add(Caption(caption, top));
        top += 20;
        box.Location = new Point(24, top);
        box.Size = new Size(432, 27);
        box.MaxLength = maxLength;
        box.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(box);
        top += 42;
    }

    private void Submit()
    {
        if (!PasswordHasher.Verify(_current.Text, _currentHash))
        {
            Dialog.Warn("The current password is incorrect.");
            _current.Focus();
            return;
        }

        if (_password.Text.Length < 6 || _password.Text.Length > 128)
        {
            Dialog.Warn("The new password must be 6–128 characters.");
            return;
        }
        if (_password.Text != _confirm.Text)
        {
            Dialog.Warn("The confirmation does not match the new password.");
            return;
        }

        try
        {
            _users.ChangePassword(_userId, PasswordHasher.Hash(_password.Text));
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        Dialog.Info("Your password was changed.");
        DialogResult = DialogResult.OK;
        Close();
    }
}

public sealed class ResetPasswordForm : Form
{
    private readonly IUserRepository _users;
    private readonly int _userId;
    private readonly TextBox _password = new();

    public ResetPasswordForm(IUserRepository users, UserAccount account)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _userId = account.UserId;

        Text = $"Reset password \u2014 {account.Username}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);

        int top = 24;
        AddField($"New temporary password for {account.FullName} (at least 6 characters)", ref top, _password, 128);
        _password.UseSystemPasswordChar = true;

        int buttonTop = top + 14;
        ClientSize = new Size(480, buttonTop + 58);

        var ok = UiFactory.PrimaryButton("Set password", 150);
        ok.Location = new Point(306, buttonTop);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(208, buttonTop);
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

    private void AddField(string caption, ref int top, TextBox box, int maxLength)
    {
        Controls.Add(Caption(caption, top));
        top += 20;
        box.Location = new Point(24, top);
        box.Size = new Size(432, 27);
        box.MaxLength = maxLength;
        box.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(box);
        top += 42;
    }

    private void Submit()
    {
        if (_password.Text.Length < 6 || _password.Text.Length > 128)
        {
            Dialog.Warn("The temporary password must be 6–128 characters.");
            return;
        }

        try
        {
            _users.ChangePassword(_userId, PasswordHasher.Hash(_password.Text));
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        Dialog.Info($"A temporary password was set.\nShare it with the staff member \u2014 they can change it after signing in.");
        DialogResult = DialogResult.OK;
        Close();
    }
}

public sealed class DependentForm : Form
{
    private readonly TextBox _fullName = new();
    private readonly ComboBox _relationship = new();
    private readonly DateTimePicker _dateOfBirth = new();

    public Dependent? Result { get; private set; }

    public DependentForm()
    {
        Text = "Add dependent";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        ClientSize = new Size(460, 300);

        int top = 20;
        AddField("Full name", ref top, _fullName, 150);

        var relLabel = new Label
        {
            Text = "Relationship",
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, top),
            Size = new Size(412, 18)
        };
        top += 20;
        _relationship.Location = new Point(24, top);
        _relationship.Size = new Size(412, 27);
        _relationship.DropDownStyle = ComboBoxStyle.DropDownList;
        _relationship.Items.AddRange(new object[]
        {
            "Child", "Spouse", "Parent", "Sibling", "Grandchild", "Grandparent", "Other"
        });
        Controls.Add(relLabel);
        Controls.Add(_relationship);
        top += 42;

        var dobLabel = new Label
        {
            Text = "Date of birth",
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, top),
            Size = new Size(412, 18)
        };
        top += 20;
        _dateOfBirth.Location = new Point(24, top);
        _dateOfBirth.Size = new Size(412, 27);
        _dateOfBirth.Format = DateTimePickerFormat.Short;
        _dateOfBirth.Value = new DateTime(2000, 1, 1);
        _dateOfBirth.MinDate = new DateTime(1900, 1, 1);
        Controls.Add(dobLabel);
        Controls.Add(_dateOfBirth);

        var ok = UiFactory.PrimaryButton("Add", 100);
        ok.Location = new Point(326, 238);
        ok.Height = 36;
        ok.Click += (_, _) => Submit();

        var cancel = UiFactory.SecondaryButton("Cancel", 90);
        cancel.Location = new Point(228, 238);
        cancel.Height = 36;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        DialogLayout.MakeScrollable(this);
    }

    private void AddField(string caption, ref int top, TextBox box, int maxLength)
    {
        Controls.Add(new Label
        {
            Text = caption,
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(24, top),
            Size = new Size(412, 18)
        });
        top += 20;
        box.Location = new Point(24, top);
        box.Size = new Size(412, 27);
        box.MaxLength = maxLength;
        box.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(box);
        top += 42;
    }

    private void Submit()
    {
        string name = _fullName.Text.Trim();
        if (name.Length == 0)
        {
            Dialog.Warn("Enter the dependent's full name.");
            _fullName.Focus();
            return;
        }
        if (_relationship.SelectedIndex < 0)
        {
            Dialog.Warn("Choose the dependent's relationship.");
            _relationship.Focus();
            return;
        }

        Result = new Dependent(name, _relationship.SelectedItem?.ToString() ?? "Other", _dateOfBirth.Value.Date);
        DialogResult = DialogResult.OK;
        Close();
    }
}