using System.Drawing;
using System.Drawing.Drawing2D;

namespace BarangayDocumentSystem.App.Theme;

/// <summary>
/// The drawing helpers that give my app its rounded, soft look.
///
/// WinForms gives me no rounded rectangle and no shadow at all, so I draw both
/// myself with GDI+. Every custom control I wrote leans on these methods.
/// </summary>
public static class Draw
{
    /// <summary>I turn on antialiasing here. Without it every curve I draw
    /// comes out visibly jagged.</summary>
    public static void Smooth(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }

    /// <summary>
    /// A rectangle with rounded corners, which I build from four arcs joined
    /// together. If I pass a radius bigger than the box I get a pill, and that
    /// is exactly what my buttons and chips use.
    /// </summary>
    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();

        if (r.Width <= 0 || r.Height <= 0) { path.AddRectangle(r); return path; }

        // I never let the radius exceed half the shorter side. If I do, the
        // arcs overlap each other and the shape turns inside out on screen.
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { path.AddRectangle(r); return path; }

        path.AddArc(r.X,               r.Y,                d, d, 180, 90);
        path.AddArc(r.Right  - d,      r.Y,                d, d, 270, 90);
        path.AddArc(r.Right  - d,      r.Bottom - d,       d, d,   0, 90);
        path.AddArc(r.X,               r.Bottom - d,       d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// A soft drop shadow, which I fake by stacking translucent outlines
    /// outwards. WinForms cannot blur anything, so this is the closest honest
    /// approximation I could manage.
    /// </summary>
    public static void Shadow(Graphics g, Rectangle bounds, int radius, int depth = 5, int startAlpha = 16)
    {
        for (int i = depth; i > 0; i--)
        {
            int alpha = Math.Max(1, startAlpha - (i * 2));
            var r = new Rectangle(bounds.X - i, bounds.Y - i + 1,
                                  bounds.Width + (i * 2), bounds.Height + (i * 2));
            using var pen = new Pen(Color.FromArgb(alpha, AppTheme.Deep), 1.6f);
            using var path = RoundedRect(r, radius + i);
            g.DrawPath(pen, path);
        }
    }

    /// <summary>The lavender gradient wash I use on the hero panel.</summary>
    public static void GradientFill(Graphics g, Rectangle r, int radius,
                                    Color from, Color to, float angle = 135f)
    {
        if (r.Width <= 0 || r.Height <= 0) return;
        using var path = RoundedRect(r, radius);
        using var brush = new LinearGradientBrush(r, from, to, angle);
        g.FillPath(brush, path);
    }

    /// <summary>I lighten or darken a colour with this. A positive amount
    /// brightens it, which is how I get my hover and pressed states from one
    /// base colour instead of defining three.</summary>
    public static Color Shade(Color c, double amount)
    {
        int Ch(int v) => Math.Max(0, Math.Min(255,
            (int)(amount >= 0 ? v + ((255 - v) * amount) : v * (1 + amount))));
        return Color.FromArgb(c.A, Ch(c.R), Ch(c.G), Ch(c.B));
    }
}
