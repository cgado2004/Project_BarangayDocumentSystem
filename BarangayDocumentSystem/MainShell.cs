#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.CustomControls;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Views;

namespace BarangayDocumentSystem;

public class MainShell : Form
{
    private readonly Dictionary<string, ViewBase> _views = new();
    private readonly NavigationSidebar _sidebar = new();
    private readonly Panel _content = new();
    private readonly Label _contentTitle = new();
    private readonly Panel _periodHost = new();
    private readonly Label _periodLabel = new();
    private Control? _periodSelector;

    private ViewBase? _current;

    public bool LoggedOut { get; private set; }

    public void LogOut()
    {
        if (!Dialog.Confirm("Log out of the system?", "Log out")) return;
        LoggedOut = true;
        Close();
    }

    public MainShell()
    {
        Text = "Barangay Resident & Document Management";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(1366, 768);
        if (Screen.PrimaryScreen is { } screen)
        {
            Size = new Size(Math.Min(Width, screen.WorkingArea.Width),
                            Math.Min(Height, screen.WorkingArea.Height));
            MinimumSize = new Size(Math.Min(1024, screen.WorkingArea.Width),
                                   Math.Min(640, screen.WorkingArea.Height));
        }
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        DoubleBuffered = true;

        BuildChrome();
    }

    private void BuildChrome()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppTheme.SidebarWidth));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _sidebar.Dock = DockStyle.Fill;
        root.Controls.Add(_sidebar, 0, 0);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        right.Controls.Add(BuildHeader(), 0, 0);
        right.Controls.Add(BuildContentArea(), 0, 1);

        root.Controls.Add(right, 1, 0);
        Controls.Add(root);

        _sidebar.NavigationChanged += (_, key) => ShowView(key);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        header.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(32, 20, 32, 18)
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232F));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

        _contentTitle.Dock = DockStyle.Fill;
        _contentTitle.Font = AppTheme.PageTitleFont;
        _contentTitle.ForeColor = AppTheme.TextPrimary;
        _contentTitle.TextAlign = ContentAlignment.MiddleLeft;
        _contentTitle.Margin = new Padding(0);
        _contentTitle.Text = "Dashboard";

        _periodHost.Dock = DockStyle.Fill;
        _periodHost.BackColor = AppTheme.Surface;
        _periodHost.Margin = new Padding(0);

        _periodLabel.Text = "Period";
        _periodLabel.Font = AppTheme.SmallBoldFont;
        _periodLabel.ForeColor = AppTheme.TextSecondary;
        _periodLabel.Location = new Point(8, 12);
        _periodLabel.AutoSize = true;
        _periodLabel.Visible = false;
        _periodHost.Controls.Add(_periodLabel);

        stack.Controls.Add(_contentTitle, 0, 0);
        stack.Controls.Add(_periodHost, 1, 0);

        header.Controls.Add(stack);
        return header;
    }

    private Control BuildContentArea()
    {
        _content.Dock = DockStyle.Fill;
        _content.BackColor = AppTheme.Background;
        _content.Padding = new Padding(28, 20, 28, 20);
        _content.Margin = new Padding(0);
        return _content;
    }

    public void AddView(string key, string label, string glyph, ViewBase view)
    {
        view.NavigateRequested += (_, req) => NavigateTo(req.Key, req.Argument);
        _views[key] = view;
        _sidebar.AddItem(key, label, glyph);
        view.Visible = false;
        _content.Controls.Add(view);
    }

    public void NavigateTo(string key, string? argument = null)
    {
        _sidebar.Navigate(key);

        if (!_views.TryGetValue(key, out var view)) return;
        if (!ReferenceEquals(_current, view)) ShowView(key);

        if (argument is not null && view is RequestsView rv)
            rv.ApplyNavigationArgument(argument);
    }

    private void ShowView(string key)
    {
        if (!_views.TryGetValue(key, out var view)) return;

        _content.SuspendLayout();
        foreach (var other in _views.Values)
            other.Visible = ReferenceEquals(other, view);
        view.BringToFront();
        _content.ResumeLayout();

        _contentTitle.Text = view.Title;

        _periodHost.SuspendLayout();
        if (_periodSelector is not null && _periodHost.Controls.Contains(_periodSelector))
            _periodHost.Controls.Remove(_periodSelector);
        if (view is DashboardView dashboard)
        {
            _periodSelector = dashboard.PeriodSelector;
            _periodSelector.Location = new Point(72, 5);
            _periodSelector.Size = new Size(150, 28);
            _periodHost.Controls.Add(_periodSelector);
            _periodLabel.Visible = true;
        }
        else
        {
            _periodLabel.Visible = false;
        }
        _periodHost.ResumeLayout();

        view.RefreshData();
        _current = view;
    }

    public void RefreshAll()
    {
        foreach (var view in _views.Values) view.RefreshData();
    }

    public ViewBase? Current => _current;

    public void Start(string initialKey)
    {
        _sidebar.Navigate(initialKey);
        if (_current is null && _views.ContainsKey(initialKey))
            ShowView(initialKey);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (var view in _views.Values) view.Dispose();
        base.Dispose(disposing);
    }
}
