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
    private readonly Label _contentSubtitle = new();
    private readonly Label _statusLabel = new();

    private ViewBase? _current;

    public MainShell()
    {
        Text = "Barangay Resident & Document Management";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1366, 768);
        MinimumSize = new Size(1180, 720);
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
            RowCount = 3,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        right.Controls.Add(BuildHeader(), 0, 0);
        right.Controls.Add(BuildContentArea(), 0, 1);
        right.Controls.Add(BuildStatusBar(), 0, 2);

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
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(32, 22, 32, 14)
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));

        _contentTitle.Dock = DockStyle.Fill;
        _contentTitle.Font = AppTheme.PageTitleFont;
        _contentTitle.ForeColor = AppTheme.TextPrimary;
        _contentTitle.TextAlign = ContentAlignment.MiddleLeft;
        _contentTitle.Margin = new Padding(0);
        _contentTitle.Text = "Dashboard";

        _contentSubtitle.Dock = DockStyle.Fill;
        _contentSubtitle.Font = AppTheme.SmallFont;
        _contentSubtitle.ForeColor = AppTheme.TextSecondary;
        _contentSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        _contentSubtitle.Margin = new Padding(0);

        stack.Controls.Add(_contentTitle, 0, 0);
        stack.Controls.Add(_contentSubtitle, 0, 1);

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

    private Control BuildStatusBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(28, 0, 28, 0),
            Margin = new Padding(0)
        };

        bar.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Font = AppTheme.SmallFont;
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.Text = "Ready";
        _statusLabel.Margin = new Padding(0);
        bar.Controls.Add(_statusLabel);
        return bar;
    }

    public void AddView(string key, string label, string glyph, ViewBase view)
    {
        view.StatusChanged += (_, message) => SetStatus(message);
        view.NavigateRequested += (_, req) => NavigateTo(req.Key, req.Argument);
        _views[key] = view;
        _sidebar.AddItem(key, label, glyph);
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
        _content.Controls.Clear();
        _content.Controls.Add(view);
        _content.ResumeLayout();

        _contentTitle.Text = view.Title;
        _contentSubtitle.Text = view.Subtitle;

        view.RefreshData();
        _current = view;
    }

    public void RefreshAll()
    {
        foreach (var view in _views.Values) view.RefreshData();
    }

    public ViewBase? Current => _current;

    public void SetStatus(string message) =>
        _statusLabel.Text = $"{DateTime.Now:HH:mm:ss}   {message}";

    public void Start(string initialKey)
    {
        _sidebar.Navigate(initialKey);
        if (_current is null && _views.ContainsKey(initialKey))
            ShowView(initialKey);
    }
}