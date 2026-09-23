using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// One entry in the left navigation rail.
///
/// I made this a real control with a default constructor so Visual Studio can
/// place it from the designer file and show it on the design surface. An
/// anonymous panel built in code would not appear there at all.
/// </summary>
public class NavButton : Control
{
    private bool _hover;

    /// <summary>True when this is the screen currently open.</summary>
    public bool Active { get; set; }

    /// <summary>The small symbol I draw to the left of the label.</summary>
    public string Glyph { get; set; } = "\u25A0";

    public NavButton()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 48;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Draw.Smooth(g);

        var r = new Rectangle(0, 2, Width - 1, Height - 5);
        if (r.Width <= 2 || r.Height <= 2) return;

        using var shape = Draw.RoundedRect(r, 12);

        Color fg;

        if (Active)
        {
            // The selected item gets the navy gradient, so it reads as the
            // current place rather than just another item.
            using var brush = new LinearGradientBrush(r, Primary, Deep, 0f);
            g.FillPath(brush, shape);

            // A gold tab down the left edge - the same gold as the seal.
            var tab = new Rectangle(r.X + 3, r.Y + 11, 4, r.Height - 22);
            using var tabBrush = new SolidBrush(Gold);
            using var tabPath = Draw.RoundedRect(tab, 2);
            g.FillPath(tabBrush, tabPath);

            fg = Color.White;
        }
        else if (_hover)
        {
            using var brush = new SolidBrush(Color.FromArgb(22, Primary));
            g.FillPath(brush, shape);
            fg = Primary;
        }
        else
        {
            fg = Muted;
        }

        TextRenderer.DrawText(g, Glyph,
            new Font(UiFamily, 13f, FontStyle.Bold),
            new Rectangle(r.X + 18, r.Y, 26, r.Height), fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

        TextRenderer.DrawText(g, Text,
            new Font(UiFamily, 11f, Active ? FontStyle.Bold : FontStyle.Regular),
            new Rectangle(r.X + 52, r.Y, r.Width - 60, r.Height), fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>
/// The left navigation rail: the seal and the barangay name, the three
/// screens, and the city line at the bottom.
///
/// v3.1 lifts this out of the main form's designer file into its own
/// control, which is what lets MainShell stay a thin shell. The rail talks
/// to the shell through one event - <see cref="Navigate"/> - and the shell
/// tells it which button is current through <see cref="SetActive"/>. Neither
/// knows anything about the other's screens.
/// </summary>
public class NavigationSidebar : Panel
{
    /// <summary>Raised with the key of the screen asked for: "dashboard",
    /// "residents" or "requests".</summary>
    public event EventHandler<string>? Navigate;

    private readonly PictureBox _logo = new();
    private readonly Label _brandTop = new();
    private readonly Label _brandSub = new();
    private readonly NavButton _dashboard = new();
    private readonly NavButton _residents = new();
    private readonly NavButton _requests = new();
    private readonly Label _footer = new();

    public NavigationSidebar()
    {
        Width = SidebarW;
        Dock = DockStyle.Left;
        BackColor = Surface;

        _logo.Name = "picLogo";
        _logo.Location = new Point(24, 26);
        _logo.Size = new Size(64, 64);
        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.BackColor = Color.Transparent;
        _logo.TabStop = false;

        _brandTop.Name = "lblBrandTop";
        _brandTop.AutoSize = false;
        _brandTop.Location = new Point(98, 34);
        _brandTop.Size = new Size(140, 24);
        _brandTop.Text = "BARANGAY";
        _brandTop.BackColor = Color.Transparent;

        _brandSub.Name = "lblBrandSub";
        _brandSub.AutoSize = false;
        _brandSub.Location = new Point(98, 56);
        _brandSub.Size = new Size(140, 36);
        _brandSub.Text = "Magugpo\r\nPoblacion";
        _brandSub.BackColor = Color.Transparent;

        _dashboard.Name = "navDashboard";
        _dashboard.Text = "Dashboard";
        _dashboard.Glyph = "\u25A6";
        _dashboard.Location = new Point(14, 118);
        _dashboard.Size = new Size(220, 48);
        _dashboard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _dashboard.Click += (_, e) => OnNavigate("dashboard", e);

        _residents.Name = "navResidents";
        _residents.Text = "Residents";
        _residents.Glyph = "\u25C9";
        _residents.Location = new Point(14, 172);
        _residents.Size = new Size(220, 48);
        _residents.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _residents.Click += (_, e) => OnNavigate("residents", e);

        _requests.Name = "navRequests";
        _requests.Text = "Document requests";
        _requests.Glyph = "\u25A4";
        _requests.Location = new Point(14, 226);
        _requests.Size = new Size(220, 48);
        _requests.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _requests.Click += (_, e) => OnNavigate("requests", e);

        _footer.Name = "lblSidebarFooter";
        _footer.AutoSize = false;
        _footer.Dock = DockStyle.Bottom;
        _footer.Height = 56;
        _footer.Padding = new Padding(24, 0, 12, 0);
        _footer.TextAlign = ContentAlignment.MiddleLeft;
        _footer.Text = "City of Tagum\r\nDavao del Norte";
        _footer.BackColor = Color.Transparent;

        Controls.Add(_footer);
        Controls.Add(_requests);
        Controls.Add(_residents);
        Controls.Add(_dashboard);
        Controls.Add(_brandSub);
        Controls.Add(_brandTop);
        Controls.Add(_logo);
    }

    /// <summary>I light up whichever rail button matches the open screen.</summary>
    public void SetActive(string key)
    {
        _dashboard.Active = key == "dashboard";
        _residents.Active = key == "residents";
        _requests.Active  = key == "requests";
        _dashboard.Invalidate();
        _residents.Invalidate();
        _requests.Invalidate();
    }

    /// <summary>The barangay seal, shown at the top of the rail. The shell
    /// loads it from Assets and hands it to me.</summary>
    public Image? Logo
    {
        get => _logo.Image;
        set => _logo.Image = value;
    }

    /// <summary>I apply the theme's fonts and colours in code rather than
    /// baking them into the designer, so one edit to AppTheme restyles the
    /// whole rail.</summary>
    public void ApplyTheme()
    {
        BackColor = Surface;

        _brandTop.Font = new Font(UiFamily, 10f, FontStyle.Bold);
        _brandTop.ForeColor = Gold;
        _brandSub.Font = new Font(UiFamily, 13f, FontStyle.Bold);
        _brandSub.ForeColor = Ink;

        _footer.Font = Small;
        _footer.ForeColor = MutedSoft;
    }

    private void OnNavigate(string key, EventArgs e) =>
        Navigate?.Invoke(this, key);
}
