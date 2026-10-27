#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public sealed class SettingsView : ViewBase
{
    private readonly BarangayProfile _profile;
    private readonly IUserRepository _users;
    private UserAccount _me;
    private readonly DataGridView _usersGrid = new();
    private readonly Label _barangay = new();
    private readonly Label _city = new();
    private readonly Label _province = new();
    private readonly Label _myName = new();
    private readonly Label _myUsername = new();
    private readonly Label _myRole = new();

    public override string Title => "Settings";

    public event EventHandler? LogoutRequested;

    public SettingsView(IBarangayRepository repository, IUserRepository users, BarangayProfile profile, UserAccount me)
        : base(repository)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _me = me ?? throw new ArgumentNullException(nameof(me));
        AutoScroll = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 0,
            ColumnCount = 1,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddRow(root, BuildMyAccountCard(), 196);
        AddRow(root, BuildProfileCard(), 158);
        if (_me.Role == UserRole.Admin)
            AddRow(root, BuildAccountsCard(), 336);
        AddRow(root, BuildSecurityCard(), 104);

        Controls.Add(root);
        RefreshData();
    }

    private static void AddRow(TableLayoutPanel root, Control card, int height)
    {
        int row = root.RowCount++;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, height + 16F));
        root.Height += height + 16;
        card.Margin = new Padding(0, 0, 0, 16);
        root.Controls.Add(card, 0, row);
    }

    public override void RefreshData()
    {
        _barangay.Text = _profile.BarangayName;
        _city.Text = _profile.CityName;
        _province.Text = _profile.ProvinceName;
        RefreshMyAccount();
        if (_me.Role == UserRole.Admin) LoadUsers();
    }

    private void RefreshMyAccount()
    {
        _me = _users.GetById(_me.UserId) ?? _me;
        _myName.Text = _me.FullName;
        _myUsername.Text = _me.Username;
        _myRole.Text = _me.Role == UserRole.Admin ? "Administrator" : "Staff";
    }

    private Control BuildMyAccountCard()
    {
        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 3; i++)
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        AddDetail(details, 0, "Name", _myName);
        AddDetail(details, 1, "Username", _myUsername);
        AddDetail(details, 2, "Role", _myRole);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var edit = UiFactory.SecondaryButton("Edit my details", 150);
        edit.Margin = new Padding(0, 0, 8, 8);
        edit.Click += (_, _) => EditMyAccount();
        var password = UiFactory.SecondaryButton("Change my password", 180);
        password.Margin = new Padding(0, 0, 8, 8);
        password.Click += (_, _) => ChangeMyPassword();
        actions.Controls.Add(edit);
        actions.Controls.Add(password);
        details.Controls.Add(actions, 1, 3);

        return BuildCard("My account", details);
    }

    private void EditMyAccount()
    {
        bool locked = IsProtected(_me);
        using var form = new AccountEditForm(_users, _me, allowRoleChange: false, lockUsername: locked);
        if (form.ShowDialog(this) == DialogResult.OK)
            RefreshMyAccount();
    }

    private void ChangeMyPassword()
    {
        using var form = new ChangePasswordForm(_users, _me);
        if (form.ShowDialog(this) == DialogResult.OK)
            RefreshMyAccount();
    }

    private Control BuildProfileCard()
    {
        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 3; i++)
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

        AddDetail(details, 0, "Barangay", _barangay);
        AddDetail(details, 1, "City", _city);
        AddDetail(details, 2, "Province", _province);
        return BuildCard("Barangay profile", details);
    }

    private Control BuildAccountsCard()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Surface,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        header.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "User accounts",
            Font = AppTheme.SubheadFont,
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        }, 0, 0);

        var create = UiFactory.PrimaryButton("Create staff account", 180);
        create.AutoSize = false;
        create.Dock = DockStyle.Fill;
        create.Margin = new Padding(0, 0, 0, 4);
        create.Click += (_, _) => CreateStaffAccount();
        header.Controls.Add(create, 1, 0);
        content.Controls.Add(header, 0, 0);

        UiFactory.StyleGrid(_usersGrid);
        var gridHost = UiFactory.CardHost(_usersGrid);
        gridHost.Margin = new Padding(0);
        content.Controls.Add(gridHost, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };

        var edit = UiFactory.SecondaryButton("Edit", 90);
        edit.AutoSize = false;
        edit.Click += (_, _) => EditSelected();
        var reset = UiFactory.SecondaryButton("Reset password", 140);
        reset.AutoSize = false;
        reset.Click += (_, _) => ResetSelected();
        var archive = UiFactory.SecondaryButton("Archive", 110);
        archive.AutoSize = false;
        archive.Click += (_, _) => ArchiveSelected();
        var delete = UiFactory.DangerButton("Delete", 100);
        delete.AutoSize = false;
        delete.Click += (_, _) => DeleteSelected();

        actions.Controls.Add(edit);
        actions.Controls.Add(reset);
        actions.Controls.Add(archive);
        actions.Controls.Add(delete);
        content.Controls.Add(actions, 0, 2);

        return UiFactory.CardHost(content);
    }

    private Control BuildSecurityCard()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var logout = UiFactory.SecondaryButton("Log out", 110);
        logout.AutoSize = false;
        logout.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);
        actions.Controls.Add(logout);
        return BuildCard("Security", actions);
    }

    private void LoadUsers()
    {
        try
        {
            _usersGrid.DataSource = _users.ListUsers()
                .OrderByDescending(u => u.Role)
                .ThenBy(u => u.Username)
                .Select(u => new
                {
                    ID = u.UserId,
                    Username = u.Username,
                    Name = u.FullName,
                    Role = u.Role.ToString(),
                    Status = u.IsArchived ? "Archived" : "Active"
                })
                .ToList();
            if (_usersGrid.Columns["ID"] is { } idColumn)
                idColumn.Visible = false;
        }
        catch (RepositoryException ex)
        {
            _usersGrid.DataSource = null;
            Dialog.Error("Could not load the account list.\n\n" + ex.Message);
        }
    }

    private UserAccount? SelectedUser()
    {
        if (_usersGrid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return _users.ListUsers().FirstOrDefault(u => u.UserId == id);
    }

    private static bool IsProtected(UserAccount account) =>
        string.Equals(account.Username, DefaultAdmin.Username, StringComparison.OrdinalIgnoreCase);

    private bool IsLastActiveAdmin(UserAccount account)
    {
        if (account.Role != UserRole.Admin || account.IsArchived) return false;
        return _users.ListUsers().Count(u => !u.IsArchived && u.Role == UserRole.Admin) <= 1;
    }

    private void CreateStaffAccount()
    {
        using var form = new CreateAccountForm(_users);
        if (form.ShowDialog(this) == DialogResult.OK)
            LoadUsers();
    }

    private void EditSelected()
    {
        var account = SelectedUser();
        if (account is null) { Dialog.SelectFirst("account"); return; }
        if (IsProtected(account))
        {
            Dialog.Info("The default administrator account is protected and cannot be edited here.");
            return;
        }

        bool self = account.UserId == _me.UserId;
        using var form = new AccountEditForm(_users, account, allowRoleChange: !self, lockUsername: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadUsers();
            if (self) RefreshMyAccount();
        }
    }

    private void ResetSelected()
    {
        var account = SelectedUser();
        if (account is null) { Dialog.SelectFirst("account"); return; }
        if (IsProtected(account))
        {
            Dialog.Info("The default administrator account is protected and cannot be reset here.");
            return;
        }

        if (account.UserId == _me.UserId)
        {
            using var form = new ChangePasswordForm(_users, account);
            if (form.ShowDialog(this) == DialogResult.OK)
                RefreshMyAccount();
            return;
        }

        using var reset = new ResetPasswordForm(_users, account);
        if (reset.ShowDialog(this) == DialogResult.OK)
            LoadUsers();
    }

    private void ArchiveSelected()
    {
        var account = SelectedUser();
        if (account is null) { Dialog.SelectFirst("account"); return; }
        if (IsProtected(account))
        {
            Dialog.Info("The default administrator account is protected and cannot be archived.");
            return;
        }
        if (account.UserId == _me.UserId)
        {
            Dialog.Warn("You cannot archive your own account.");
            return;
        }
        if (!account.IsArchived && IsLastActiveAdmin(account))
        {
            Dialog.Warn("The last active administrator cannot be archived.");
            return;
        }

        bool restoring = account.IsArchived;
        if (!Dialog.Confirm($"{(restoring ? "Restore" : "Archive")} account \u201c{account.Username}\u201d ({account.FullName})?",
                $"{((restoring) ? "Restore" : "Archive")} account"))
            return;

        try
        {
            _users.SetArchived(account.UserId, !account.IsArchived);
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        LoadUsers();
        SetStatus($"Account \u201c{account.Username}\u201d was {(restoring ? "restored" : "archived")}.");
    }

    private void DeleteSelected()
    {
        var account = SelectedUser();
        if (account is null) { Dialog.SelectFirst("account"); return; }
        if (IsProtected(account))
        {
            Dialog.Info("The default administrator account is protected and cannot be deleted.");
            return;
        }
        if (account.UserId == _me.UserId)
        {
            Dialog.Warn("You cannot delete your own account.");
            return;
        }
        if (!account.IsArchived && IsLastActiveAdmin(account))
        {
            Dialog.Warn("The last active administrator cannot be deleted.");
            return;
        }

        if (!Dialog.Confirm($"Permanently delete account \u201c{account.Username}\u201d ({account.FullName})?\n\nThis cannot be undone.",
                "Delete account"))
            return;

        try
        {
            _users.Delete(account.UserId);
        }
        catch (RepositoryException ex)
        {
            Dialog.Error(ex.Message);
            return;
        }

        LoadUsers();
        SetStatus($"Account \u201c{account.Username}\u201d was deleted.");
    }

    private static void AddDetail(TableLayoutPanel table, int row, string caption, Label value)
    {
        table.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = caption,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.BodyBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0)
        }, 0, row);

        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.ForeColor = AppTheme.TextPrimary;
        value.AutoEllipsis = true;
        value.Margin = new Padding(0);
        table.Controls.Add(value, 1, row);
    }

    private static Control BuildCard(string title, Control details)
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Surface,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = AppTheme.SubheadFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0)
        }, 0, 0);
        content.Controls.Add(details, 0, 1);
        return UiFactory.CardHost(content);
    }
}

public class ActivityLogView : ViewBase
{
    private readonly DataGridView _grid = new();
    private readonly DateTimePicker _day = UiFactory.DayFilter("activityDay");
    private readonly Label _emptyLabel = new();

    public override string Title => "Activity Log";

    public ActivityLogView(IBarangayRepository repository) : base(repository)
    {
        _day.Enabled = true;
        _day.ValueChanged += (_, _) => RefreshData();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var bar = UiFactory.FilterBar();
        bar.Controls.Add(UiFactory.FilterField("Day", _day));
        root.Controls.Add(bar, 0, 0);

        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        var host = UiFactory.CardHost(_grid);
        host.Margin = new Padding(0);

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = AppTheme.BodyFont;
        _emptyLabel.ForeColor = AppTheme.TextMuted;
        _emptyLabel.BackColor = AppTheme.Surface;
        host.Controls.Add(_emptyLabel);
        _emptyLabel.BringToFront();

        root.Controls.Add(host, 0, 1);
        Controls.Add(root);
    }

    public override void RefreshData()
    {
        var entries = Repository.GetActivity(_day.Value.Date);
        _grid.DataSource = entries
            .Select(e => new
            {
                Time = e.LoggedOn.ToString("HH:mm:ss"),
                User = e.Username,
                Action = e.Action,
                Details = e.Details
            })
            .ToList();

        _emptyLabel.Text = $"No activity logged on {_day.Value:MMM dd, yyyy}";
        _emptyLabel.Visible = entries.Count == 0;
    }
}
