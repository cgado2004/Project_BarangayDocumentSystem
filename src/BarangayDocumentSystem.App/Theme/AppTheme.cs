using System.Drawing;
using System.Drawing.Text;

namespace BarangayDocumentSystem.App.Theme;

/// <summary>
/// Every colour, font and spacing value I use in the application.
///
/// MY FONT CHOICE, AND WHY I CHANGED IT
/// I originally used Segoe UI here. It works, but it is the font every stock
/// Windows program already uses, so my app looked like a settings dialog
/// rather than something designed. I was asked twice to make it look modern
/// and I did not act on it, which was my mistake.
///
/// I now lead with Bahnschrift. It is Microsoft's take on DIN 1451, the
/// German road-sign typeface - geometric, wide, confident, and genuinely
/// different from the usual Windows look. Crucially it SHIPS WITH WINDOWS
/// (10 version 1709 and later, and all of Windows 11), so I am not asking
/// anyone to install anything, and my NFR-08 promise about system fonts still
/// holds.
///
/// I do not simply trust that it is there. ResolveFont below actually checks
/// the installed font list at startup and falls back through Segoe UI Variable
/// to Segoe UI if it is missing. A machine on an old Windows 10 build gets a
/// slightly plainer app instead of a crash.
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

    /// <summary>The monospaced family, for the certificate preview.</summary>
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
        UiFamily = FirstAvailable(
            "Bahnschrift",          // my first choice - modern, geometric, ships with Windows
            "Segoe UI Variable",    // Windows 11's own UI face
            "Segoe UI",             // every Windows since Vista
            "Tahoma");              // last resort

        MonoFamily = FirstAvailable(
            "Cascadia Mono",        // ships with Windows Terminal and VS
            "Consolas",
            "Courier New");
    }

    private static string FirstAvailable(params string[] candidates)
    {
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
    // swap Segoe UI for Bahnschrift by editing one method above.
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
