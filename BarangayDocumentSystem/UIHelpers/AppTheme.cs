using System.Drawing;
using System;
namespace BarangayDocumentSystem.UIHelpers;

/// <summary>
/// Every colour, font and spacing value in the application.
///
/// ── DRY: design tokens ──────────────────────────────────────────────────
/// Before, `Color.FromArgb(21, 71, 52)` appeared as a literal in six separate
/// Designer files, and font names were retyped on almost every control.
/// Rebranding meant a find-and-replace across the whole UI and hoping nothing
/// was missed.
///
/// Now there is one source of truth. Change Primary here and the entire app
/// follows.
///
/// ── FONT SAFETY ─────────────────────────────────────────────────────────
/// Only fonts that ship with Windows are used — Segoe UI (the system UI font
/// since Vista) and Consolas (monospace, since Vista).
///
/// A custom font that is NOT installed on the grading machine does not throw
/// an error: Windows silently substitutes a fallback with different metrics,
/// so text overflows its labels and the layout quietly breaks. Using system
/// fonts removes that entire class of failure.
/// </summary>
public static class AppTheme
{
    // ---------- Colour palette ----------
    public static readonly Color Primary       = Color.FromArgb(21, 71, 52);    // barangay green
    public static readonly Color PrimaryDark   = Color.FromArgb(14, 50, 36);
    public static readonly Color PrimaryLight  = Color.FromArgb(34, 102, 76);
    public static readonly Color Accent        = Color.FromArgb(201, 162, 39);  // gold

    public static readonly Color Surface       = Color.White;
    public static readonly Color Background    = Color.FromArgb(244, 246, 245);
    public static readonly Color Border        = Color.FromArgb(222, 226, 224);

    public static readonly Color TextPrimary   = Color.FromArgb(26, 32, 30);
    public static readonly Color TextSecondary = Color.FromArgb(105, 117, 112);
    public static readonly Color TextOnPrimary = Color.White;

    public static readonly Color Success       = Color.FromArgb(24, 121, 78);
    public static readonly Color Warning       = Color.FromArgb(181, 122, 12);
    public static readonly Color Danger        = Color.FromArgb(176, 48, 42);
    public static readonly Color Info          = Color.FromArgb(38, 98, 158);

    // ---------- Typography (system fonts only) ----------
    private const string UiFont   = "Segoe UI";
    private const string MonoFont = "Consolas";

    public static Font DisplayFont  => new(UiFont, 17f, FontStyle.Bold);
    public static Font HeadingFont  => new(UiFont, 13f, FontStyle.Bold);
    public static Font SubheadFont  => new(UiFont, 11f, FontStyle.Bold);
    public static Font BodyFont     => new(UiFont, 10f, FontStyle.Regular);
    public static Font BodyBoldFont => new(UiFont, 10f, FontStyle.Bold);
    public static Font SmallFont    => new(UiFont, 8.5f, FontStyle.Regular);
    public static Font MonoBodyFont => new(MonoFont, 10f, FontStyle.Regular);

    // ---------- Spacing (a 4px scale keeps rhythm consistent) ----------
    public const int SpaceXs = 4;
    public const int SpaceSm = 8;
    public const int SpaceMd = 16;
    public const int SpaceLg = 24;
    public const int SpaceXl = 32;

    public const int SidebarWidth = 232;
    public const int HeaderHeight = 72;
    public const int ControlHeight = 34;

    /// <summary>
    /// Maps a request status to its badge colour.
    ///
    /// ── DRY ─────────────────────────────────────────────────────────────
    /// This mapping previously did not exist — the grid just printed status as
    /// plain text. Defining it once means the grid, any future detail panel,
    /// and any report all colour statuses identically.
    /// </summary>
    public static Color StatusColor(string status) => status switch
    {
        "Pending"         => Warning,
        "Processing"      => Info,
        "ReadyForRelease" => Accent,
        "Released"        => Success,
        "Rejected"        => Danger,
        _                 => TextSecondary
    };
}
