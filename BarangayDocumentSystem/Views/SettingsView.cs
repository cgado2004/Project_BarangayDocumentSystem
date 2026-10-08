#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public sealed class SettingsView : ViewBase
{
    private readonly BarangayProfile _profile;
    private readonly Label _barangay = new();
    private readonly Label _city = new();
    private readonly Label _province = new();
    private readonly Label _punongBarangay = new();

    public override string Title => "Settings";
    public override string Subtitle => "Barangay profile and account settings";

    public SettingsView(IBarangayRepository repository, BarangayProfile profile)
        : base(repository)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        AutoScroll = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 578,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        foreach (int height in new[] { 246, 16, 150, 16, 150 })
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        root.Controls.Add(BuildProfileCard(), 0, 0);
        root.Controls.Add(BuildCard("Staff profile", "Account details",
            "Staff name and role will appear here when user accounts are connected."), 0, 2);
        root.Controls.Add(BuildCard("Security", "Sign-in and access",
            "Password changes and permissions will be available after authentication is connected."), 0, 4);

        Controls.Add(root);
        RefreshData();
    }

    public override void RefreshData()
    {
        _barangay.Text = _profile.BarangayName;
        _city.Text = _profile.CityName;
        _province.Text = _profile.ProvinceName;
        _punongBarangay.Text = _profile.PunongBarangay.Contains("[")
            ? "Not set"
            : _profile.PunongBarangay;
    }

    private Control BuildProfileCard()
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
        for (int i = 0; i < 4; i++)
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

        AddDetail(details, 0, "Barangay", _barangay);
        AddDetail(details, 1, "City", _city);
        AddDetail(details, 2, "Province", _province);
        AddDetail(details, 3, "Punong Barangay", _punongBarangay);
        return BuildCard("Barangay profile", "Details used on printed documents", details);
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

    private static Control BuildCard(string title, string description, string message) =>
        BuildCard(title, description, new Label
        {
            Dock = DockStyle.Fill,
            Text = message,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0)
        });

    private static Control BuildCard(string title, string description, Control details)
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
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = AppTheme.SubheadFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0)
        }, 0, 0);
        content.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = description,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0)
        }, 0, 1);
        content.Controls.Add(details, 0, 2);
        return UiFactory.CardHost(content);
    }
}
