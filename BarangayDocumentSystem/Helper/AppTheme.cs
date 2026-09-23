using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BarangayDocumentSystem.Helper;

/// <summary>
/// Every colour, font and spacing value I use in the application, plus the
/// GDI+ drawing helpers that give the app its rounded, soft look.
///
/// MY FONT CHOICE, AND THE v3.1 FIX
/// I originally used Segoe UI, then Bahnschrift - both stock Windows faces,
/// both looking like every other Windows dialog. v3.1 moves to the modern
/// product typography: Google's INTER, with Apple's SF Pro and Google's
/// ROBOTO as the rest of the fallback stack.
///
///     Inter  ->  SF Pro Display  ->  SF Pro Text  ->  Roboto
///             ->  Segoe UI Variable  ->  Segoe UI  ->  Tahoma
///
/// Inter and Roboto are free (SIL Open Font License); SF Pro is what macOS
/// and iOS use. None of them is guaranteed to be installed, so
/// <see cref="Resolve"/> actually checks the machine's font table at
/// startup and picks the first one present - falling all the way back to
/// Segoe UI, which every Windows since Vista has. Install Inter from
/// rsms.me/inter or Google Fonts for the intended look; the app runs
/// correctly either way.
///
/// Text is drawn with ClearType (ClearTypeGridFit) throughout - see
/// <see cref="Draw.Smooth"/> - so the UI text is sub-pixel smoothed rather
/// than the grey jaggedness of the default hint.
///
/// WHY IT IS ALL IN ONE FILE
/// If I typed a colour into a form, changing it later would mean hunting
/// through every form I have written. Here I edit one line and the whole
/// application follows.
/// </summary>
public static class AppTheme
{
    // =================================================================
    //  Colours
    //
    //  I pulled these from the barangay's own seal - the deep blue of the
    //  shield, the gold of the sun's rays, and the red of the flag triangle.
    //  Using the seal's own colours means the app and the logo look like they
    //  belong together, instead of the logo being pasted onto someone else's
    //  colour scheme.
    // =================================================================

    public static readonly Color Ink        = Color.FromArgb(0x0B, 0x14, 0x2B); // near-black navy
    public static readonly Color Primary    = Color.FromArgb(0x1B, 0x3B, 0x8B); // seal blue
    public static readonly Color PrimaryDim = Color.FromArgb(0x2E, 0x52, 0xA8);
    public static readonly Color Deep       = Color.FromArgb(0x0A, 0x1F, 0x54); // darkest navy
    public static readonly Color Gold       = Color.FromArgb(0xF2, 0xB1, 0x1B); // sun gold
    public static readonly Color GoldSoft   = Color.FromArgb(0xFF, 0xF0, 0xCC);
    public static readonly Color Crimson    = Color.FromArgb(0xC8, 0x2A, 0x32); // flag red

    public static readonly Color Lavender     = Color.FromArgb(0xD8, 0xE3, 0xFA);
    public static readonly Color LavenderSoft = Color.FromArgb(0xEE, 0xF3, 0xFD);
    public static readonly Color Periwinkle   = Color.FromArgb(0x6C, 0x84, 0xC4);

    public static readonly Color Canvas  = Color.FromArgb(0xF5, 0xF7, 0xFC); // a hint of blue, not flat white
    public static readonly Color Surface = Color.White;
    public static readonly Color Border  = Color.FromArgb(0xE2, 0xE7, 0xF2);
    public static readonly Color Muted   = Color.FromArgb(0x63, 0x6C, 0x80);
    public static readonly Color MutedSoft = Color.FromArgb(0xA8, 0xB0, 0xC2);

    // ---- status colours ----
    public static readonly Color Success = Color.FromArgb(0x1A, 0x7F, 0x4F);
    public static readonly Color Warning = Color.FromArgb(0xB8, 0x78, 0x00);
    public static readonly Color Danger  = Color.FromArgb(0xC0, 0x31, 0x39);
    public static readonly Color Info    = Primary;

    // =================================================================
    //  Fonts
    // =================================================================

    /// <summary>The UI family I actually resolved at startup.</summary>
    public static string UiFamily { get; private set; } = "Segoe UI";

    /// <summary>The monospaced family, for anything column-aligned.</summary>
    public static string MonoFamily { get; private set; } = "Consolas";

    /// <summary>
    /// I work out which fonts this machine really has, once, at startup.
    ///
    /// I ask Windows for its installed families rather than assuming, because
    /// assuming a font exists is exactly how a layout breaks on somebody
    /// else's computer. Program.cs calls this before any form is created.
    /// </summary>
    public static void Resolve()
    {
        UiFamily = FirstInstalledFont(
            "Inter",                // Google / rsms.me/inter - first choice
            "Inter Display",        // Inter's display cut, newer releases
            "SF Pro Display",       // Apple's UI face
            "SF Pro Text",
            "Roboto",               // Google's other workhorse
            "Segoe UI Variable",    // Windows 11's own UI face
            "Segoe UI",             // every Windows since Vista
            "Tahoma");              // last resort

        MonoFamily = FirstInstalledFont(
            "Cascadia Mono",        // ships with Windows Terminal and VS
            "Consolas",
            "Courier New");
    }

    /// <summary>
    /// The first of the candidates the machine actually has. Public, because
    /// the document renderer resolves its serif family the same way. When I
    /// cannot read the font list at all I return the last candidate rather
    /// than stop the program.
    /// </summary>
    public static string FirstInstalledFont(params string[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Length == 0) return "Segoe UI";

        try
        {
            using var installed = new InstalledFontCollection();
            var names = new HashSet<string>(
                installed.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

            foreach (string name in candidates)
                if (names.Contains(name)) return name;
        }
        catch
        {
            // If I cannot read the font list for any reason, I would rather
            // fall through to the last candidate than stop the program.
        }

        return candidates[^1];
    }

    // I build fonts through these properties so every screen asks for a role
    // ("Title", "Body") rather than naming a font family. That is what let me
    // swap the whole typography over to the Inter stack by editing one method.
    public static Font Hero      => new(UiFamily, 30f, FontStyle.Bold);
    public static Font Display   => new(UiFamily, 23f, FontStyle.Bold);
    public static Font Title     => new(UiFamily, 17f, FontStyle.Bold);
    public static Font Heading   => new(UiFamily, 13f, FontStyle.Bold);
    public static Font Subhead   => new(UiFamily, 11f, FontStyle.Bold);
    public static Font Body      => new(UiFamily, 10.5f, FontStyle.Regular);
    public static Font BodyBold  => new(UiFamily, 10.5f, FontStyle.Bold);
    public static Font Small     => new(UiFamily, 9f, FontStyle.Regular);
    public static Font SmallBold => new(UiFamily, 9f, FontStyle.Bold);
    public static Font StatValue => new(UiFamily, 30f, FontStyle.Bold);
    public static Font MonoBody  => new(MonoFamily, 10f, FontStyle.Regular);

    // =================================================================
    //  Spacing
    // =================================================================
    public const int PagePad    = 28;
    public const int CardPad    = 22;
    public const int Gap        = 18;
    public const int RadiusCard = 16;
    public const int SidebarW   = 248;
}

/// <summary>
/// The drawing helpers that give my app its rounded, soft look.
///
/// WinForms gives me no rounded rectangle and no shadow at all, so I draw
/// both myself with GDI+. Every custom control I wrote leans on these
/// methods. All text drawn through them is ClearType-hinted, which is the
/// v3.1 "ClearType GDI+" fix: crisp sub-pixel text instead of grey
/// jaggedness.
/// </summary>
public static class Draw
{
    /// <summary>I turn on antialiasing and ClearType text hinting here.
    /// Without it every curve I draw comes out visibly jagged and every
    /// glyph comes out grey.</summary>
    public static void Smooth(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
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
