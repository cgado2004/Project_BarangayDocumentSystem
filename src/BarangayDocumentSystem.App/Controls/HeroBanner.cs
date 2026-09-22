using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Theme;

namespace BarangayDocumentSystem.App.Controls;

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

        using var shape = Draw.RoundedRect(r, AppTheme.RadiusCard + 4);

        // The navy wash. I run it diagonally so the darker corner sits under
        // the seal and the lighter one under the text.
        using (var brush = new LinearGradientBrush(
                   r, AppTheme.Deep, AppTheme.Primary, 20f))
        {
            g.FillPath(brush, shape);
        }

        // I clip everything that follows to the rounded shape, so the rays and
        // the glow cannot spill past the corners.
        var saved = g.Save();
        g.SetClip(shape);

        int cx = 96;
        int cy = Height / 2;

        // Faint rays fanning from behind the seal, echoing the sun on the
        // barangay's own logo.
        using (var rayPen = new Pen(Color.FromArgb(26, AppTheme.Gold), 2f))
        {
            for (int i = 0; i < 16; i++)
            {
                double angle = i * (Math.PI * 2 / 16);
                g.DrawLine(rayPen, cx, cy,
                    cx + (float)(Math.Cos(angle) * 300),
                    cy + (float)(Math.Sin(angle) * 300));
            }
        }

        // A soft glow behind the seal so it lifts off the navy.
        using (var glow = new GraphicsPath())
        {
            glow.AddEllipse(cx - 58, cy - 58, 116, 116);
            using var pgb = new PathGradientBrush(glow)
            {
                CenterColor = Color.FromArgb(70, Color.White),
                SurroundColors = new[] { Color.FromArgb(0, Color.White) }
            };
            g.FillPath(pgb, glow);
        }

        // A white disc for the seal to sit on. The seal artwork is drawn for a
        // white background, so this keeps its colours true.
        using (var disc = new SolidBrush(Color.FromArgb(240, Color.White)))
        {
            g.FillEllipse(disc, cx - 46, cy - 46, 92, 92);
        }

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
                new Font(AppTheme.UiFamily, 21f, FontStyle.Bold),
                new Rectangle(textLeft, cy - 50, available, 34),
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, Subtitle,
                new Font(AppTheme.UiFamily, 11f, FontStyle.Regular),
                new Rectangle(textLeft, cy - 16, available, 24),
                Color.FromArgb(210, Color.White),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Footnote.Length > 0)
            {
                // A gold rule, then the Punong Barangay's name.
                using (var rule = new Pen(Color.FromArgb(140, AppTheme.Gold), 2f))
                    g.DrawLine(rule, textLeft, cy + 16, textLeft + 46, cy + 16);

                TextRenderer.DrawText(g, Footnote,
                    new Font(AppTheme.UiFamily, 9.5f, FontStyle.Bold),
                    new Rectangle(textLeft, cy + 24, available, 22),
                    AppTheme.GoldSoft,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        g.Restore(saved);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _logo?.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// One labelled bar in my "requests by document type" panel.
///
/// I use a bar rather than a plain number because the eye compares lengths far
/// faster than it compares digits - you can see which document is busiest
/// without reading anything.
/// </summary>
public class BarRow : Control
{
    public string Caption { get; set; } = string.Empty;
    public int Value { get; set; }
    public int Maximum { get; set; } = 1;

    public BarRow()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
        Height = 40;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Draw.Smooth(g);

        TextRenderer.DrawText(g, Caption, AppTheme.Small,
            new Rectangle(0, 0, Width - 40, 18), AppTheme.Ink,
            TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, Value.ToString(), AppTheme.SmallBold,
            new Rectangle(Width - 38, 0, 36, 18), AppTheme.Primary,
            TextFormatFlags.Right);

        var track = new Rectangle(0, 22, Math.Max(1, Width - 2), 8);
        using (var bg = new SolidBrush(AppTheme.LavenderSoft))
        using (var path = Draw.RoundedRect(track, 4))
            g.FillPath(bg, path);

        // I guard against dividing by zero, and I always draw at least a sliver
        // so a count of 1 is still visible.
        int denom = Math.Max(1, Maximum);
        int w = Math.Max(6, (int)(track.Width * (Value / (double)denom)));

        var fill = new Rectangle(track.X, track.Y, w, track.Height);
        using (var brush = new LinearGradientBrush(
                   new Rectangle(fill.X, fill.Y, Math.Max(2, fill.Width), fill.Height),
                   AppTheme.Primary, AppTheme.Periwinkle, 0f))
        using (var path = Draw.RoundedRect(fill, 4))
            g.FillPath(brush, path);
    }
}
