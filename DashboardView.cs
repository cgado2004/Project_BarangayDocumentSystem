using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// The banner across the top of my dashboard: the barangay seal on a deep
/// navy gradient, with the barangay name beside it.
///
/// WHY I PAINT IT MYSELF
/// I want a gradient, rounded corners, a soft glow behind the seal and
/// hairline rays fanning out from it. WinForms gives me none of those, so the
/// whole thing is drawn with GDI+ in OnPaint.
///
/// I draw the seal at its natural aspect ratio inside a circle so it never
/// looks squashed, which is what happens if you let a PictureBox stretch it.
/// </summary>
public class HeroBanner : Control
{
    private Image? _logo;

    // v3.1.5: cached GDI objects. Every OnPaint used to allocate a
    // gradient brush, a path gradient with its path, a pen and a solid
    // brush - on a resize drag that was dozens of handles a second on the
    // most-painted control in the app. The pen and the disc brush are
    // immutable and built once; the background gradient and the glow
    // depend only on the control's size, so they are rebuilt (old ones
    // disposed) only when the size actually changes.
    private Pen? _rayPen;
    private SolidBrush? _discBrush;
    private LinearGradientBrush? _bgBrush;
    private PathGradientBrush? _glowBrush;
    private GraphicsPath? _glowPath;
    private Size _bgBuiltFor;
    private int _glowBuiltFor;

    /// <summary>The barangay seal. I take ownership and dispose the old one.</summary>
    public Image? Logo
    {
        get => _logo;
        set
        {
            _logo?.Dispose();   // or every refresh leaks the previous bitmap
            _logo = value;
            Invalidate();
        }
    }

    public string Title    { get; set; } = "Barangay Magugpo Poblacion";
    public string Subtitle { get; set; } = "City of Tagum, Davao del Norte";
    public string Footnote { get; set; } = string.Empty;

    public HeroBanner()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
        Height = 158;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();   // the gradient is sized to the control, so it must repaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Draw.Smooth(g);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 2 || r.Height <= 2) return;

        using var shape = Draw.RoundedRect(r, RadiusCard + 4);

        // The navy wash - Deep is now the spec's #1B365D banner navy, and the
        // gradient runs diagonally so the darker corner sits under the seal
        // and the lighter one under the text.
        if (_bgBrush is null || _bgBuiltFor != Size)
        {
            _bgBrush?.Dispose();
            _bgBrush = new LinearGradientBrush(r, Deep, Primary, 20f);
            _bgBuiltFor = Size;
        }
        g.FillPath(_bgBrush, shape);

        // I clip everything that follows to the rounded shape, so the rays and
        // the glow cannot spill past the corners.
        var saved = g.Save();
        g.SetClip(shape);

        int cx = 96;
        int cy = Height / 2;

        // Faint rays fanning from behind the seal, echoing the sun on the
        // barangay's own logo.
        _rayPen ??= new Pen(Color.FromArgb(26, Gold), 2f);
        for (int i = 0; i < 16; i++)
        {
            double angle = i * (Math.PI * 2 / 16);
            g.DrawLine(_rayPen, cx, cy,
                cx + (float)(Math.Cos(angle) * 300),
                cy + (float)(Math.Sin(angle) * 300));
        }

        // A soft glow behind the seal so it lifts off the navy.
        if (_glowBrush is null || _glowBuiltFor != Height)
        {
            _glowBrush?.Dispose();
            _glowPath?.Dispose();
            _glowPath = new GraphicsPath();
            _glowPath.AddEllipse(cx - 58, cy - 58, 116, 116);
            _glowBrush = new PathGradientBrush(_glowPath)
            {
                CenterColor = Color.FromArgb(70, Color.White),
                SurroundColors = new[] { Color.FromArgb(0, Color.White) }
            };
            _glowBuiltFor = Height;
        }
        g.FillPath(_glowBrush, _glowPath);

        // A white disc for the seal to sit on. The seal artwork is drawn for a
        // white background, so this keeps its colours true.
        _discBrush ??= new SolidBrush(Color.FromArgb(240, Color.White));
        g.FillEllipse(_discBrush, cx - 46, cy - 46, 92, 92);

        if (_logo is not null)
        {
            // I fit the seal inside the disc while keeping its aspect ratio.
            const int box = 78;
            float scale = Math.Min((float)box / _logo.Width, (float)box / _logo.Height);
            int w = (int)(_logo.Width * scale);
            int h = (int)(_logo.Height * scale);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_logo, cx - w / 2, cy - h / 2, w, h);
        }

        // ---- the text block ----
        int textLeft = cx + 74;
        int available = Width - textLeft - 28;

        if (available > 80)
        {
            TextRenderer.DrawText(g, Title,
                HeroTitle,   // cached in AppTheme - was reallocated on every paint
                new Rectangle(textLeft, cy - 50, available, 34),
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, Subtitle,
                HeroSubtitle,
                new Rectangle(textLeft, cy - 16, available, 24),
                Color.FromArgb(210, Color.White),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Footnote.Length > 0)
            {
                // A gold rule, then the Punong Barangay's name.
                using (var rule = new Pen(Color.FromArgb(140, Gold), 2f))
                    g.DrawLine(rule, textLeft, cy + 16, textLeft + 46, cy + 16);

                TextRenderer.DrawText(g, Footnote,
                    HeroFootnote,
                    new Rectangle(textLeft, cy + 24, available, 22),
                    GoldSoft,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        g.Restore(saved);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo?.Dispose();
            _rayPen?.Dispose();
            _discBrush?.Dispose();
            _bgBrush?.Dispose();
            _glowBrush?.Dispose();
            _glowPath?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// The dashboard: the hero banner, the clickable statistic cards, residents
/// by purok, and requests by document type.
///
/// v3.1 lifts the dashboard out of the main form into its own view, so the
/// shell is nothing but navigation. The layout keeps the v3 responsive
/// behaviour - the six tiles compute their width from the space actually
/// available, and the two breakdown panels split the row through a
/// percent-sized TableLayoutPanel rather than fixed halves.
///
/// Every card and chip is clickable and jumps to the filtered list it
/// summarises. My rule was that no number should be a dead end - if you can
/// see it, you can click into it and find out what it is made of.
/// </summary>
public class DashboardView : ViewBase
{
    private readonly IBarangayRepository _repository;
    private readonly Service.FeeSchedule _fees;

    /// <summary>Raised with ("residents", filter) or ("requests", filter) so
    /// the shell can open the list behind a number.</summary>
    public event EventHandler<(string View, string? Filter)>? RequestNavigate;

    private readonly HeroBanner _hero = new();
    private int _lastTileW;   // the width the dynamic surfaces were last built for
    private readonly FlowLayoutPanel _stats = new();
    private readonly TableLayoutPanel _breakdown = new();
    private readonly Card _cardPurok = new();
    private readonly Label _purokTitle = new();
    private readonly FlowLayoutPanel _purokChips = new();
    private readonly Card _cardDocTypes = new();
    private readonly Label _docTypesTitle = new();
    private readonly FlowLayoutPanel _docTypeBars = new();

    public DashboardView(IBarangayRepository repository, Service.FeeSchedule fees)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fees = fees ?? throw new ArgumentNullException(nameof(fees));

        _hero.Dock = DockStyle.Top;
        _hero.Height = 158;

        _stats.Name = "flowStats";
        _stats.Dock = DockStyle.Top;
        _stats.AutoSize = true;
        _stats.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _stats.WrapContents = true;

        _purokTitle.Text = "Residents by purok — click one to filter";
        _purokTitle.Dock = DockStyle.Top;
        _purokTitle.Height = 32;

        _purokChips.Dock = DockStyle.Fill;
        _purokChips.AutoScroll = true;
        _purokChips.WrapContents = true;

        _cardPurok.Dock = DockStyle.Fill;
        _cardPurok.Controls.Add(_purokChips);
        _cardPurok.Controls.Add(_purokTitle);

        _docTypesTitle.Text = "Requests by document type";
        _docTypesTitle.Dock = DockStyle.Top;
        _docTypesTitle.Height = 32;

        _docTypeBars.Dock = DockStyle.Fill;
        _docTypeBars.AutoScroll = true;
        _docTypeBars.WrapContents = false;
        _docTypeBars.FlowDirection = FlowDirection.TopDown;

        _cardDocTypes.Dock = DockStyle.Fill;
        _cardDocTypes.Controls.Add(_docTypeBars);
        _cardDocTypes.Controls.Add(_docTypesTitle);

        // The two breakdown cards split the row 50/50 through PERCENTAGE
        // columns, so the split holds at every window width.
        _breakdown.Name = "tblBreakdown";
        _breakdown.Dock = DockStyle.Fill;
        _breakdown.ColumnCount = 2;
        _breakdown.RowCount = 1;
        _breakdown.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _breakdown.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _breakdown.Controls.Add(_cardPurok, 0, 0);
        _breakdown.Controls.Add(_cardDocTypes, 1, 0);

        Controls.Add(_breakdown);
        Controls.Add(_stats);
        Controls.Add(_hero);

        // Structure is set; now the look. Every colour, font and spacing
        // this view owns is applied in one dedicated pass, strictly AFTER
        // the structural setup above - the same contract the designer
        // -backed forms follow with ApplyTheme()/BuildUi() after
        // InitializeComponent().
        ApplyModernUIStyles();
    }

    /// <summary>
    /// The single place this view's modern-UI styling is applied: theme
    /// fonts, colours, spacing and card metrics. No control parenting, no
    /// event wiring, no structure - those live in the constructor, so a
    /// restyle can never accidentally re-parent or re-wire anything.
    /// </summary>
    private void ApplyModernUIStyles()
    {
        // hero banner metrics (the navy gradient itself is painted in HeroBanner.OnPaint)
        _hero.Margin = new Padding(0, 0, 0, Gap);

        // stats strip
        _stats.BackColor = Color.Transparent;
        _stats.Padding = new Padding(0, Gap, 0, 0);

        // breakdown section titles
        _purokTitle.Font = Subhead;
        _purokTitle.ForeColor = Ink;
        _purokTitle.BackColor = Color.Transparent;

        _docTypesTitle.Font = Subhead;
        _docTypesTitle.ForeColor = Ink;
        _docTypesTitle.BackColor = Color.Transparent;

        // breakdown cards and their content surfaces
        _cardPurok.Margin = new Padding(0, 0, 9, 0);
        _cardPurok.Padding = new Padding(18, 14, 14, 14);
        _purokChips.BackColor = Color.Transparent;

        _cardDocTypes.Margin = new Padding(9, 0, 0, 0);
        _cardDocTypes.Padding = new Padding(18, 14, 14, 14);
        _docTypeBars.BackColor = Color.Transparent;

        _breakdown.BackColor = Color.Transparent;
        _breakdown.Padding = new Padding(0, Gap, 0, 0);
    }

    /// <summary>The barangay seal, shown in the hero banner.</summary>
    public Image? Logo
    {
        get => _hero.Logo;
        set => _hero.Logo = value;
    }

    public override void OnShown() => RefreshFigures();

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        // v3.1.5: a resize drag fires this dozens of times a second, and
        // rebuilding the stats, chips and bars on every tick is what made
        // the window rubber-band. The rebuild only NEEDS to happen when
        // the computed tile width actually changes - the figures
        // themselves are refreshed on every OnShown, so nothing is lost.
        if (_stats.Controls.Count == 0) return;
        if (ComputeTileW() != _lastTileW) RefreshFigures();
    }

    private void Go(string view, string? filter) =>
        RequestNavigate?.Invoke(this, (view, filter));

    /// <summary>
    /// The tile width the stat grid builds with right now. The resize
    /// guard consults the SAME maths RefreshFigures uses, so the two can
    /// never drift apart.
    /// </summary>
    private int ComputeTileW()
    {
        const int tiles = 6;
        const int gap = 14;
        int avail = Math.Max(600, ClientSize.Width - Padding.Horizontal);
        return Math.Max(160, (avail - (gap * (tiles - 1))) / tiles);
    }

    /// <summary>
    /// I rebuild every figure on the dashboard from the repository.
    /// </summary>
    private void RefreshFigures()
    {
        var s = _repository.GetStatistics();
        var profile = BarangayProfile.Current;

        // v3.1.5: the three dynamic surfaces are rebuilt here on every
        // show and every resize. Suspending layout across the rebuild
        // means one layout pass instead of one per added control - this
        // method is exactly the "wrap dynamic layout builds" case.
        _stats.SuspendLayout();
        _purokChips.SuspendLayout();
        _docTypeBars.SuspendLayout();
        try
        {

        _hero.Title = profile.BarangayName;
        _hero.Subtitle = $"{profile.CityName}, {profile.ProvinceName}";
        _hero.Footnote = $"Punong Barangay   ·   {profile.PunongBarangay}";

        // I work the tile width out from the space actually available rather
        // than fixing it at 244px. With six tiles and a fixed width, the last
        // two dropped onto a second row and fell off the bottom on a normal
        // screen - I only saw that when I rendered the layout and looked at it.
        //
        // v3.1.5: the floor drops 178 -> 160 because 178 STILL wrapped the
        // sixth tile at the default 1360x860 window: the content pane is
        // 1056px there and six 178px tiles need 1138px. Six 160px tiles need
        // 1030px, which fits - and 160 still clears the widest tile content.
        int tileW = ComputeTileW();
        _lastTileW = tileW;

        _stats.Controls.Clear();
        _stats.Controls.Add(MakeStat("Residents", s.TotalResidents.ToString(),
            "on the registry", Primary, tileW, gap, () => Go("residents", null)));
        // v3.1.3: the Pending tile carries its own urgency - how much of
        // the backlog is already past the RA 11032 standard - so the
        // backlog and its deadline are one glance apart, not two clicks.
        int agedPending = _repository.Requests.Count(r =>
            r.Status == RequestStatus.Pending &&
            r.IsBeyondRA11032Standard(_fees.RA11032SimpleWorkingDays));

        _stats.Controls.Add(MakeStat("Pending", s.Pending.ToString(),
            agedPending > 0 ? $"to be processed \u00b7 {agedPending} aged" : "to be processed",
            Warning, tileW, gap, () => Go("requests", "Pending")));
        _stats.Controls.Add(MakeStat("Ready", s.ReadyForRelease.ToString(),
            "to be collected", Info, tileW, gap, () => Go("requests", "ReadyForRelease")));
        _stats.Controls.Add(MakeStat("Released", s.Released.ToString(),
            "issued to date", Success, tileW, gap, () => Go("requests", "Released")));
        _stats.Controls.Add(MakeStat("Collected", Service.DisplayFormat.Peso(s.TotalCollected),
            "against receipts", SlateInk, tileW, gap, () => Go("requests", null)));
        _stats.Controls.Add(MakeStat("Issued free", s.IssuedFreeOfCharge.ToString(),
            "statutory exemptions", Crimson, tileW, gap, () => Go("requests", null)));

        _purokChips.Controls.Clear();
        foreach (var kv in s.ResidentsByPurok)
        {
            string label = $"{kv.Key}  ·  {kv.Value}";
            var chip = new Chip
            {
                Text = label,
                Width = TextRenderer.MeasureText(label, SmallBold).Width + 34,
                Margin = new Padding(0, 0, 8, 8)
            };
            string purok = kv.Key;
            chip.Click += (_, _) => Go("residents", purok);
            _purokChips.Controls.Add(chip);
        }

        _docTypeBars.Controls.Clear();
        int max = s.RequestsByDocumentType.Count == 0
            ? 1 : s.RequestsByDocumentType.Values.Max();
        foreach (var kv in s.RequestsByDocumentType)
        {
            _docTypeBars.Controls.Add(new BarRow
            {
                Caption = kv.Key,
                Value = kv.Value,
                Maximum = max,
                Width = Math.Max(240, _docTypeBars.ClientSize.Width - 24),
                Height = 40,
                Margin = new Padding(0, 0, 0, 6)
            });
        }
        }
        finally
        {
            _stats.ResumeLayout(true);
            _purokChips.ResumeLayout(true);
            _docTypeBars.ResumeLayout(true);
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
            AccentBar = accent,
            Padding = new Padding(20, 16, 14, 12)
        };

        var lbl = new Label
        {
            // v3.1.2 spec: an 11px uppercase overline above each figure.
            // (WinForms offers no ExtraBold weight and no letter-spacing;
            // Bold at exactly 11px carries the intent as far as it goes.)
            Text = label.ToUpperInvariant(),
            Font = Overline,
            ForeColor = accent,
            Dock = DockStyle.Top,
            Height = 22,
            BackColor = Color.Transparent
        };
        var val = new Label
        {
            Text = value,
            // A peso amount is much longer than a count, so I step the size
            // down for wide text - otherwise "₱12,345.00" is simply cut off.
            // Counts sit at the spec's 36px; peso text at 32px stays inside
            // the tile while keeping the same weight.
            Font = value.Length > 6
                ? StatValueLong
                : StatValue,
            ForeColor = Ink,
            Dock = DockStyle.Top,
            Height = 50,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        var cap = new Label
        {
            Text = caption,
            Font = Small,
            ForeColor = Muted,
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.Transparent
        };

        card.Controls.Add(cap);
        card.Controls.Add(val);
        card.Controls.Add(lbl);

        // v3.1.3: the card is clickable, so it is also one accessible
        // object with a sentence a screen reader can actually say.
        card.AccessibleName = $"{label}: {value}, {caption}.";

        // I wire the click to the labels as well as the card. A label sits on
        // top of its parent and swallows the click, so without this, clicking
        // the big number would do nothing and the card would feel broken.
        void Go(object? s, EventArgs e) => onClick();
        card.Click += Go;
        foreach (Control c in card.Controls) { c.Click += Go; c.Cursor = Cursors.Hand; }

        return card;
    }
}
