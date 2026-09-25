using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// My main window - the shell the whole application lives in.
///
/// v3.1 thins it right down: the navigation rail is the NavigationSidebar
/// control, the dashboard is a view like the other two, and the shell keeps
/// only navigation, theming, sizing and the status bar. SwapContent never
/// disposes a view, because the views are long-lived and get reused every
/// time you navigate back.
///
/// It also owns the first half of the multi-monitor DPI fix (core fix 6):
/// the WM_DPICHANGED handler below applies the window rectangle Windows
/// suggests when the form crosses onto a monitor with a different DPI,
/// restyles the shell's own fonts, and tells the open view to refresh -
/// while WinForms' PerMonitorV2 support rescales the child controls.
/// </summary>
public partial class MainShell : Form
{
    private const int WM_DPICHANGED = 0x02E0;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private readonly IBarangayRepository _repository;
    private readonly FeeSchedule _fees;

    private DashboardView? _dashboardView;
    private ResidentsView? _residentsView;
    private RequestsView? _requestsView;

    /// <summary>
    /// I take the repository and the fee rules from outside rather than
    /// creating them here. That is what lets me swap the in-memory store for
    /// MySQL without touching this form, and it is why Program.cs is the only
    /// file that names a concrete store.
    /// </summary>
    public MainShell(IBarangayRepository repository, FeeSchedule fees)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fees = fees ?? throw new ArgumentNullException(nameof(fees));

        InitializeComponent();
        ApplyTheme();
        SizeToScreen();
        LoadLogo();
        UpdateDpiReadout();

        ShowDashboard();
    }

    /// <summary>
    /// I apply colours and fonts in code rather than hard-coding them into the
    /// designer file.
    ///
    /// My reason: the designer stores a literal colour on every control, so
    /// changing the palette would mean editing dozens of lines. Reading them
    /// from AppTheme means one edit changes the whole window, and it is what
    /// let me swap the typography to the Inter stack in a single place.
    /// </summary>
    private void ApplyTheme()
    {
        BackColor = Canvas;

        sidebar.ApplyTheme();

        pnlStatus.BackColor = Surface;

        lblPageTitle.Font = Display;
        lblPageTitle.ForeColor = Ink;
        lblPageSubtitle.Font = Body;
        lblPageSubtitle.ForeColor = Muted;

        lblStatus.Font = Small;
        lblStatus.ForeColor = Muted;
        lblStatusRight.Font = Small;
        lblStatusRight.ForeColor = MutedSoft;
    }

    /// <summary>
    /// I load the barangay seal from the Assets folder.
    ///
    /// I wrap it in a try/catch because a missing image file must never stop
    /// the program opening. If the seal is not there the app still runs, just
    /// without the picture - which is far better than a crash on startup in
    /// front of a panel.
    ///
    /// The sidebar owns a detached bitmap, so the source file is not locked.
    /// </summary>
    private void LoadLogo()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "barangay-logo.png");
            if (!File.Exists(path)) return;

            using (var source = Image.FromFile(path))
                sidebar.Logo = new Bitmap(source);
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
        var area = Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1280, 800);

        ClientSize = new System.Drawing.Size(
            Math.Min(1360, (int)(area.Width * 0.92)),
            Math.Min(860, (int)(area.Height * 0.92)));

        MinimumSize = new System.Drawing.Size(
            Math.Min(1020, area.Width),
            Math.Min(660, area.Height));
    }

    // =================================================================
    //  Navigation
    // =================================================================

    private void OnSidebarNavigate(object? sender, string key)
    {
        switch (key)
        {
            case "residents": ShowResidents(null); break;
            case "requests":  ShowRequests(null);  break;
            default:          ShowDashboard();     break;
        }
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

    private void SetHeader(string title, string subtitle)
    {
        lblPageTitle.Text = title;
        lblPageSubtitle.Text = subtitle;
    }

    public void ShowDashboard()
    {
        if (_dashboardView is null)
        {
            _dashboardView = new DashboardView(_repository);
            _dashboardView.RequestNavigate += (_, e) =>
            {
                if (e.View == "residents") ShowResidents(e.Filter);
                else ShowRequests(e.Filter);
            };
        }

        sidebar.SetActive("dashboard");
        SetHeader("Dashboard", "Live figures for the barangay office");
        SwapContent(_dashboardView);
        _dashboardView.OnShown();
        UpdateStatus();
    }

    public void ShowResidents(string? filter)
    {
        _residentsView ??= BuildResidentsView();

        sidebar.SetActive("residents");
        SetHeader("Residents", "The barangay registry");
        SwapContent(_residentsView);

        if (filter is not null) _residentsView.ApplyFilter(filter);
        _residentsView.OnShown();
        UpdateStatus();
    }

    public void ShowRequests(string? filter)
    {
        _requestsView ??= new RequestsView(_repository, _fees);

        sidebar.SetActive("requests");
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

    private void UpdateStatus()
    {
        var s = _repository.GetStatistics();
        lblStatus.Text =
            $"{s.TotalResidents} residents   ·   {s.TotalRequests} requests   ·   " +
            $"{DisplayFormat.Peso(s.TotalCollected)} collected   ·   " +
            $"{BarangayProfile.Current.PunongBarangay}";
    }

    private void UpdateDpiReadout()
    {
        try
        {
            lblStatusRight.Text = $"Demo • not saved • {DeviceDpi} DPI";
        }
        catch
        {
            lblStatusRight.Text = string.Empty;
        }
    }

    // =================================================================
    //  WM_DPICHANGED - the per-monitor DPI handler, core fix 6
    // =================================================================

    /// <summary>
    /// Windows sends WM_DPICHANGED when the window is dragged onto a monitor
    /// with a different DPI. wParam's low word is the NEW DPI; lParam points
    /// at the rectangle Windows suggests for the window at that DPI.
    ///
    /// WinForms' PerMonitorV2 support rescales the child controls for me;
    /// what it cannot know is my shell-owned theming and my custom-painted
    /// views, so I do three things here: take the suggested rectangle, apply
    /// the theme again so the shell's own fonts pick up the new scale, and
    /// refresh whichever view is open so its custom-drawn cards repaint at
    /// the new size.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DPICHANGED)
        {
            var rect = Marshal.PtrToStructure<RECT>(m.LParam);
            Bounds = System.Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);

            ApplyTheme();
            UpdateDpiReadout();

            _dashboardView?.OnShown();
            _residentsView?.OnShown();
            _requestsView?.OnShown();
        }

        base.WndProc(ref m);
    }
}
