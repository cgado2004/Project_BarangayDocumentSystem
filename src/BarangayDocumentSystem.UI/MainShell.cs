using BarangayDocumentSystem.UI.Common;
using BarangayDocumentSystem.UI.Theme;
using BarangayDocumentSystem.UI.Views;

namespace BarangayDocumentSystem.UI;

/// <summary>
/// Application shell: sidebar, content header, content area, status bar.
///
/// ── SINGLE RESPONSIBILITY ───────────────────────────────────────────────
/// v1's MainForm was 429 lines and did four jobs. This shell does ONE:
/// arrange the chrome and swap views. It contains no resident logic, no
/// request logic and no statistics — those live in the three views.
///
/// ── OPEN/CLOSED ─────────────────────────────────────────────────────────
/// Views are registered into a dictionary. Adding a page means adding a
/// subclass and one AddView line; the switching code never changes.
///
/// ── LISKOV ──────────────────────────────────────────────────────────────
/// The dictionary holds ViewBase. The shell calls Title, Subtitle and
/// RefreshData() without knowing or caring which concrete view it has.
/// </summary>
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
        Text = "Barangay Resident and Document Request Management System";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1280, 780);
        MinimumSize = new Size(1120, 700);
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;

        BuildChrome();
    }

    private void BuildChrome()
    {
        // ---- status bar ----
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = AppTheme.Surface,
            Padding = new Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0)
        };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Font = AppTheme.SmallFont;
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.Text = "Ready";
        statusBar.Controls.Add(_statusLabel);

        // ---- sidebar ----
        _sidebar.NavigationChanged += (_, key) => ShowView(key);

        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = AppTheme.HeaderHeight,
            BackColor = AppTheme.PrimaryDark,
            Padding = new Padding(AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceSm, 0)
        };

        var brandTitle = new Label
        {
            Text = "BARANGAY",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = AppTheme.TextOnPrimary
        };

        var brandSub = new Label
        {
            Text = "Magugpo Poblacion",
            Dock = DockStyle.Top,
            Height = 22,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.Accent
        };

        brand.Controls.Add(brandSub);
        brand.Controls.Add(brandTitle);

        // ---- content header ----
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = AppTheme.HeaderHeight,
            BackColor = AppTheme.Surface,
            Padding = new Padding(AppTheme.SpaceLg, AppTheme.SpaceSm, AppTheme.SpaceLg, 0)
        };

        _contentTitle.Dock = DockStyle.Top;
        _contentTitle.Height = 32;
        _contentTitle.Font = AppTheme.DisplayFont;
        _contentTitle.ForeColor = AppTheme.TextPrimary;

        _contentSubtitle.Dock = DockStyle.Top;
        _contentSubtitle.Height = 22;
        _contentSubtitle.Font = AppTheme.SmallFont;
        _contentSubtitle.ForeColor = AppTheme.TextSecondary;

        header.Controls.Add(_contentSubtitle);
        header.Controls.Add(_contentTitle);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = AppTheme.Background;

        // Add order matters: Fill must be added before the docked edges it
        // should sit inside.
        Controls.Add(_content);
        Controls.Add(header);
        Controls.Add(_sidebar);
        Controls.Add(statusBar);

        _sidebar.Controls.Add(brand);
        brand.BringToFront();
    }

    /// <summary>
    /// Registers a view and its sidebar entry.
    /// Adding a page touches only the caller — never this class (OCP).
    /// </summary>
    public void AddView(string key, string label, string glyph, ViewBase view)
    {
        view.StatusChanged += (_, message) => SetStatus(message);

        _views[key] = view;
        _sidebar.AddItem(key, label, glyph);
    }

    private void ShowView(string key)
    {
        if (!_views.TryGetValue(key, out var view)) return;

        _content.Controls.Clear();
        _content.Controls.Add(view);

        _contentTitle.Text = view.Title;
        _contentSubtitle.Text = view.Subtitle;

        view.RefreshData();
        _current = view;
    }

    /// <summary>Refreshes every view — used after a cross-cutting change.</summary>
    public void RefreshAll()
    {
        foreach (var view in _views.Values)
            view.RefreshData();
    }

    public ViewBase? Current => _current;

    public void SetStatus(string message) =>
        _statusLabel.Text = $"{DateTime.Now:HH:mm:ss}   {message}";

    /// <summary>Opens the first registered destination once everything is wired.</summary>
    public void Start(string initialKey) => _sidebar.Navigate(initialKey);
}
