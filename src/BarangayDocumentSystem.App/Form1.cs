using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Controls;
using BarangayDocumentSystem.App.Theme;
using BarangayDocumentSystem.App.Views;
using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.Core.Entities;
using BarangayDocumentSystem.Core.Rules;

namespace BarangayDocumentSystem.App;

/// <summary>
/// My main form - the window the whole application lives in.
///
/// WHY THIS FILE EXISTS AT ALL
/// My earlier version built every screen in code with no form file, which
/// meant Visual Studio's Solution Explorer showed no form to open and the
/// designer had nothing to draw. That was a fair complaint: a WinForms project
/// with no visible form does not look like a WinForms project.
///
/// So the layout now lives in Form1.Designer.cs where Visual Studio expects
/// it, and this file holds only what the form DOES - navigation, loading the
/// figures, and reacting to clicks.
/// </summary>
public partial class Form1 : Form
{
    private readonly IBarangayRepository _repository;
    private readonly FeeSchedule _fees;

    private ResidentsView? _residentsView;
    private RequestsView? _requestsView;

    /// <summary>
    /// I take the repository and the fee rules from outside rather than
    /// creating them here. That is what lets me swap the in-memory store for
    /// MySQL without touching this form, and it is why Program.cs is the only
    /// file that names a concrete class.
    /// </summary>
    public Form1(IBarangayRepository repository, FeeSchedule fees)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fees = fees ?? throw new ArgumentNullException(nameof(fees));

        InitializeComponent();
        ApplyTheme();
        SizeToScreen();
        LoadLogo();

        ShowDashboard();
    }

    /// <summary>
    /// I apply colours and fonts in code rather than hard-coding them into the
    /// designer file.
    ///
    /// My reason: the designer stores a literal colour on every control, so
    /// changing the palette would mean editing dozens of lines. Reading them
    /// from AppTheme means one edit changes the whole window, and it is what
    /// let me swap the font family across the app in a single place.
    /// </summary>
    private void ApplyTheme()
    {
        BackColor = AppTheme.Canvas;

        pnlSidebar.BackColor = AppTheme.Surface;
        pnlStatus.BackColor = AppTheme.Surface;

        lblBrandTop.Font = new Font(AppTheme.UiFamily, 10f, FontStyle.Bold);
        lblBrandTop.ForeColor = AppTheme.Gold;
        lblBrandSub.Font = new Font(AppTheme.UiFamily, 13f, FontStyle.Bold);
        lblBrandSub.ForeColor = AppTheme.Ink;

        lblSidebarFooter.Font = AppTheme.Small;
        lblSidebarFooter.ForeColor = AppTheme.MutedSoft;

        lblPageTitle.Font = AppTheme.Display;
        lblPageTitle.ForeColor = AppTheme.Ink;
        lblPageSubtitle.Font = AppTheme.Body;
        lblPageSubtitle.ForeColor = AppTheme.Muted;

        lblPurokTitle.Font = AppTheme.Subhead;
        lblPurokTitle.ForeColor = AppTheme.Ink;
        lblDocTypesTitle.Font = AppTheme.Subhead;
        lblDocTypesTitle.ForeColor = AppTheme.Ink;

        lblStatus.Font = AppTheme.Small;
        lblStatus.ForeColor = AppTheme.Muted;
    }

    /// <summary>
    /// I load the barangay seal from the Assets folder.
    ///
    /// I wrap it in a try/catch because a missing image file must never stop
    /// the program opening. If the seal is not there the app still runs, just
    /// without the picture - which is far better than a crash on startup in
    /// front of a panel.
    /// </summary>
    private void LoadLogo()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "barangay-logo.png");
            if (File.Exists(path))
            {
                // I read the bytes and build the image from a MemoryStream
                // rather than Image.FromFile, because FromFile keeps the file
                // locked for as long as the image lives.
                byte[] bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                var logo = Image.FromStream(ms);

                picLogo.Image = logo;
                heroBanner.Logo = Image.FromStream(new MemoryStream(bytes));
            }
        }
        catch (Exception)
        {
            // No seal on screen, but the app still works.
        }
    }

    /// <summary>
    /// I size the window against the screen it is actually opening on.
    ///
    /// On a small laptop it fills most of the screen; on a large monitor it
    /// stops at a sensible width instead of stretching to something unusable.
    /// This is the difference between the app working on my machine and
    /// working on everyone's.
    /// </summary>
    private void SizeToScreen()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);

        ClientSize = new Size(
            Math.Min(1360, (int)(area.Width * 0.92)),
            Math.Min(860, (int)(area.Height * 0.92)));

        MinimumSize = new Size(
            Math.Min(1020, area.Width),
            Math.Min(660, area.Height));
    }

    // =================================================================
    //  Navigation
    // =================================================================

    private void btnNavDashboard_Click(object? sender, EventArgs e) => ShowDashboard();
    private void btnNavResidents_Click(object? sender, EventArgs e) => ShowResidents(null);
    private void btnNavRequests_Click(object? sender, EventArgs e) => ShowRequests(null);

    /// <summary>I light up whichever rail button matches the open screen.</summary>
    private void SetActiveNav(NavButton active)
    {
        foreach (var b in new[] { btnNavDashboard, btnNavResidents, btnNavRequests })
        {
            b.Active = ReferenceEquals(b, active);
            b.Invalidate();
        }
    }

    private void SetHeader(string title, string subtitle)
    {
        lblPageTitle.Text = title;
        lblPageSubtitle.Text = subtitle;
    }

    /// <summary>
    /// I swap whichever view is in the content panel.
    ///
    /// I remove the old control rather than disposing it, because my views are
    /// long-lived and get reused every time you navigate back. Disposing one
    /// would destroy its controls and leave a blank screen on the second visit.
    /// </summary>
    private void SwapContent(Control view)
    {
        pnlContent.SuspendLayout();
        pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(view);
        pnlContent.ResumeLayout();
    }

    public void ShowDashboard()
    {
        SetActiveNav(btnNavDashboard);
        SetHeader("Dashboard", "Live figures for the barangay office");
        SwapContent(pnlDashboard);
        RefreshDashboard();
        UpdateStatus();
    }

    public void ShowResidents(string? filter)
    {
        _residentsView ??= BuildResidentsView();

        SetActiveNav(btnNavResidents);
        SetHeader("Residents", "The barangay registry");
        SwapContent(_residentsView);

        if (filter is not null) _residentsView.ApplyFilter(filter);
        _residentsView.OnShown();
        UpdateStatus();
    }

    public void ShowRequests(string? filter)
    {
        _requestsView ??= new RequestsView(_repository);

        SetActiveNav(btnNavRequests);
        SetHeader("Document requests", "Track each request from filing to release");
        SwapContent(_requestsView);

        if (filter is not null) _requestsView.ApplyFilter(filter);
        _requestsView.OnShown();
        UpdateStatus();
    }

    private ResidentsView BuildResidentsView()
    {
        var view = new ResidentsView(_repository, _fees);
        view.RequestNavigate += (_, e) => ShowRequests(e.Filter);
        return view;
    }

    // =================================================================
    //  Dashboard
    // =================================================================

    /// <summary>
    /// I rebuild every figure on the dashboard from the repository.
    ///
    /// Every card and chip I create here is clickable and jumps to the list it
    /// summarises. My rule was that no number should be a dead end - if you
    /// can see it, you can click into it and find out what it is made of.
    /// </summary>
    private void RefreshDashboard()
    {
        var s = _repository.GetStatistics();
        var profile = BarangayProfile.Current;

        heroBanner.Title = profile.BarangayName;
        heroBanner.Subtitle = $"{profile.CityName}, {profile.ProvinceName}";
        heroBanner.Footnote = $"Punong Barangay   ·   {profile.PunongBarangay}";

        // I work the tile width out from the space actually available rather
        // than fixing it at 244px. With six tiles and a fixed width, the last
        // two dropped onto a second row and fell off the bottom on a normal
        // screen - I only saw that when I rendered the layout and looked at it.
        const int tiles = 6;
        const int gap = 14;
        int avail = Math.Max(600, pnlDashboard.ClientSize.Width
                                  - pnlDashboard.Padding.Horizontal);
        int tileW = Math.Max(178, (avail - (gap * (tiles - 1))) / tiles);

        flowStats.Controls.Clear();
        flowStats.Controls.Add(MakeStat("Residents", s.TotalResidents.ToString(),
            "on the registry", AppTheme.Primary, tileW, gap, () => ShowResidents(null)));
        flowStats.Controls.Add(MakeStat("Pending", s.Pending.ToString(),
            "to be processed", AppTheme.Warning, tileW, gap, () => ShowRequests("Pending")));
        flowStats.Controls.Add(MakeStat("Ready", s.ReadyForRelease.ToString(),
            "to be collected", AppTheme.Info, tileW, gap, () => ShowRequests("ReadyForRelease")));
        flowStats.Controls.Add(MakeStat("Released", s.Released.ToString(),
            "issued to date", AppTheme.Success, tileW, gap, () => ShowRequests("Released")));
        flowStats.Controls.Add(MakeStat("Collected", DisplayFormat.Peso(s.TotalCollected),
            "against receipts", AppTheme.Deep, tileW, gap, () => ShowRequests(null)));
        flowStats.Controls.Add(MakeStat("Issued free", s.IssuedFreeOfCharge.ToString(),
            "statutory exemptions", AppTheme.Crimson, tileW, gap, () => ShowRequests(null)));

        flowPuroks.Controls.Clear();
        foreach (var kv in s.ResidentsByPurok)
        {
            string label = $"{kv.Key}  ·  {kv.Value}";
            var chip = new Chip
            {
                Text = label,
                Width = TextRenderer.MeasureText(label, AppTheme.SmallBold).Width + 34,
                Margin = new Padding(0, 0, 8, 8)
            };
            string purok = kv.Key;
            chip.Click += (_, _) => ShowResidents(purok);
            flowPuroks.Controls.Add(chip);
        }

        flowDocTypes.Controls.Clear();
        int max = s.RequestsByDocumentType.Count == 0
            ? 1 : s.RequestsByDocumentType.Values.Max();
        foreach (var kv in s.RequestsByDocumentType)
        {
            flowDocTypes.Controls.Add(new BarRow
            {
                Caption = kv.Key,
                Value = kv.Value,
                Maximum = max,
                Width = Math.Max(240, flowDocTypes.ClientSize.Width - 24),
                Height = 40,
                Margin = new Padding(0, 0, 0, 6)
            });
        }
    }

    /// <summary>One clickable statistic tile.</summary>
    private static Card MakeStat(string label, string value, string caption,
                                 Color accent, int width, int gap, Action onClick)
    {
        var card = new Card
        {
            Width = width,
            Height = 138,
            Margin = new Padding(0, 0, gap, gap),
            Cursor = Cursors.Hand,
            AccentBar = accent
        };

        var lbl = new Label
        {
            Text = label.ToUpperInvariant(),
            Font = AppTheme.SmallBold,
            ForeColor = accent,
            Dock = DockStyle.Top,
            Height = 22,
            BackColor = Color.Transparent
        };
        var val = new Label
        {
            Text = value,
            // A peso amount is much longer than a count, so I step the size
            // down for wide text. Otherwise "₱12,345.00" is simply cut off.
            Font = value.Length > 6
                ? new Font(AppTheme.UiFamily, 19f, FontStyle.Bold)
                : AppTheme.StatValue,
            ForeColor = AppTheme.Ink,
            Dock = DockStyle.Top,
            Height = 50,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        var cap = new Label
        {
            Text = caption,
            Font = AppTheme.Small,
            ForeColor = AppTheme.Muted,
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.Transparent
        };

        card.Controls.Add(cap);
        card.Controls.Add(val);
        card.Controls.Add(lbl);

        // I wire the click to the labels as well as the card. A label sits on
        // top of its parent and swallows the click, so without this, clicking
        // the big number would do nothing and the card would feel broken.
        void Go(object? s, EventArgs e) => onClick();
        card.Click += Go;
        foreach (Control c in card.Controls) { c.Click += Go; c.Cursor = Cursors.Hand; }

        return card;
    }

    /// <summary>
    /// I rebuild the tiles when the window is resized, because their width is
    /// calculated from the space available. Without this they would keep the
    /// size they had when the form first opened.
    /// </summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (pnlDashboard.Visible && flowStats.Controls.Count > 0) RefreshDashboard();
    }

    private void UpdateStatus()
    {
        var s = _repository.GetStatistics();
        lblStatus.Text =
            $"{s.TotalResidents} residents   ·   {s.TotalRequests} requests   ·   "
          + $"{DisplayFormat.Peso(s.TotalCollected)} collected   ·   "
          + $"{BarangayProfile.Current.PunongBarangay}";
    }
}
