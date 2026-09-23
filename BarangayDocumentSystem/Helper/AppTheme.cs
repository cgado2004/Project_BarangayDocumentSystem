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
    //  v3.1.2 retunes these to the team's dashboard design specification
    //  (the slate/indigo system: canvas #F8FAFC, ink #0F172A, muted
    //  #64748B, and the six tile accents), while keeping the seal's gold
    //  for the brand. The role of every token is unchanged - this is the
    //  one file to edit when the palette moves again.
    // =================================================================

    public static readonly Color Ink        = Color.FromArgb(0x0F, 0x17, 0x2A); // slate 900 - primary text
    public static readonly Color Primary    = Color.FromArgb(0x1E, 0x3A, 0x8A); // blue 800 - residents accent
    public static readonly Color PrimaryDim = Color.FromArgb(0x2E, 0x4A, 0x9E); // lighter sibling
    public static readonly Color Deep       = Color.FromArgb(0x1B, 0x36, 0x5D); // hero banner navy (spec)
    public static readonly Color SlateInk   = Color.FromArgb(0x1E, 0x29, 0x3B); // slate 800 - collected accent
    public static readonly Color Sky        = Color.FromArgb(0x02, 0x84, 0xC7); // sky 600 - ready accent
    public static readonly Color Gold       = Color.FromArgb(0xF2, 0xB1, 0x1B); // sun gold, from the seal
    public static readonly Color GoldSoft   = Color.FromArgb(0xFF, 0xF0, 0xCC);
    public static readonly Color Crimson    = Color.FromArgb(0xDC, 0x26, 0x26); // red 600 - issued-free accent

    public static readonly Color Lavender     = Color.FromArgb(0xD8, 0xE3, 0xFA);
    public static readonly Color LavenderSoft = Color.FromArgb(0xEE, 0xF3, 0xFD);
    public static readonly Color Periwinkle   = Color.FromArgb(0x6C, 0x84, 0xC4);

    public static readonly Color Canvas  = Color.FromArgb(0xF8, 0xFA, 0xFC); // slate 50 - main background
    public static readonly Color Surface = Color.White;
    public static readonly Color Border  = Color.FromArgb(0xE2, 0xE8, 0xF0); // slate 200
    public static readonly Color Muted   = Color.FromArgb(0x64, 0x74, 0x8B); // slate 500 - secondary text
    public static readonly Color MutedSoft = Color.FromArgb(0x94, 0xA3, 0xB8); // slate 400

    // ---- status colours ----
    public static readonly Color Success = Color.FromArgb(0x05, 0x96, 0x69); // emerald 600 - released
    public static readonly Color Warning = Color.FromArgb(0xD9, 0x77, 0x06); // amber 600 - pending
    public static readonly Color Danger  = Color.FromArgb(0xDC, 0x26, 0x26); // red 600
    public static readonly Color Info    = Sky;

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
        // v3.1.3: bundled fonts first. If Assets/fonts carries the brand
        // typefaces (see BundledFonts below), they win over whatever the
        // machine happens to have installed, so the dashboard looks the
        // same on every laptop in the room - typography by design, not
        // by luck.
        UiFamily = FirstUiFont(
            "Inter",                // Google / rsms.me/inter - first choice
            "Inter Display",        // Inter's display cut, newer releases
            "Plus Jakarta Sans",    // spec family #2 (v3.1.2 dashboard spec)
            "SF Pro Display",       // Apple's UI face
            "SF Pro Text",
            "Roboto",               // Google's other workhorse
            "Segoe UI Variable",    // Windows 11's own UI face
            "Segoe UI",             // every Windows since Vista
            "Tahoma");              // last resort

        MonoFamily = FirstUiFont(
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

    // -----------------------------------------------------------------
    //  Bundled fonts, v3.1.3
    //
    //  Typography that depends on what happens to be installed is not
    //  typography, it is luck. The app therefore looks for font files in
    //  Assets/fonts FIRST - drop Inter-Regular.ttf and Inter-Bold.ttf
    //  there and every machine renders the same face, installed or not
    //  (the SIL Open Font License allows shipping them with the app) -
    //  and only falls back to the installed-font probe when the folder
    //  is empty or the machine cannot read it.
    // -----------------------------------------------------------------
    private static System.Drawing.Text.PrivateFontCollection? _bundled;

    private static System.Drawing.Text.PrivateFontCollection BundledFonts()
    {
        if (_bundled is not null) return _bundled;

        var collection = new System.Drawing.Text.PrivateFontCollection();
        try
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "Assets", "fonts");
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                        collection.AddFontFile(file);   // one bad file skips, it does not stop the app
                }
            }
        }
        catch
        {
            // unreadable folder or typeface - the installed probe still runs
        }

        _bundled = collection;
        return _bundled;
    }

    /// <summary>The first family found among the bundled fonts, then the
    /// machine's installed set. The last candidate is always the floor.</summary>
    private static string FirstUiFont(params string[] candidates)
    {
        var bundled = BundledFonts();
        if (bundled.Families.Length > 0)
        {
            var names = bundled.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string name in candidates)
                if (names.Contains(name)) return name;
        }

        return FirstInstalledFont(candidates);
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
    public static Font StatValue => new(UiFamily, 27f, FontStyle.Bold);   // 36px - the dashboard spec's 34-38px band

    /// <summary>The 11px uppercase face for stat-card headers. WinForms has
    /// no ExtraBold weight and no letter-spacing, so Bold carries the role
    /// as far as the platform allows - the size and the casing are exact.</summary>
    public static Font Overline  => new(UiFamily, 8.25f, FontStyle.Bold);  // 11px
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
