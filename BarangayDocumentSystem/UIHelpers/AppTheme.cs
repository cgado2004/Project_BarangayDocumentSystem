using System.Drawing;
using System.Windows.Forms;

namespace BarangayDocumentSystem.UIHelpers;

public static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(243, 239, 231);
    public static readonly Color Surface = Color.FromArgb(255, 255, 255);
    public static readonly Color SurfaceAlt = Color.FromArgb(251, 248, 242);
    public static readonly Color Hover = Color.FromArgb(238, 233, 222);
    public static readonly Color Border = Color.FromArgb(228, 220, 201);
    public static readonly Color BorderStrong = Color.FromArgb(200, 191, 168);

    public static readonly Color SidebarBg = Color.FromArgb(16, 29, 51);
    public static readonly Color SidebarHover = Color.FromArgb(28, 45, 74);
    public static readonly Color SidebarActive = Color.FromArgb(212, 160, 23);
    public static readonly Color SidebarText = Color.FromArgb(138, 155, 181);
    public static readonly Color SidebarTextActive = Color.White;

    public static readonly Color Primary = Color.FromArgb(11, 37, 69);
    public static readonly Color PrimaryHover = Color.FromArgb(30, 58, 95);
    public static readonly Color PrimaryDark = Color.FromArgb(6, 24, 46);
    public static readonly Color PrimarySoft = Color.FromArgb(230, 237, 247);
    public static readonly Color Accent = Color.FromArgb(201, 162, 39);

    public static readonly Color TextPrimary = Color.FromArgb(31, 39, 51);
    public static readonly Color TextSecondary = Color.FromArgb(107, 114, 128);
    public static readonly Color TextMuted = Color.FromArgb(156, 163, 175);
    public static readonly Color TextOnPrimary = Color.White;

    public static readonly Color Info = Color.FromArgb(3, 105, 161);
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);
    public static readonly Color Success = Color.FromArgb(21, 128, 61);
    public static readonly Color Danger = Color.FromArgb(185, 28, 28);
    public static readonly Color DangerHover = Color.FromArgb(153, 27, 27);
    public static readonly Color DangerDark = Color.FromArgb(127, 29, 29);

    public static readonly Font BodyFont = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
    public static readonly Font BodyBoldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
    public static readonly Font SmallFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
    public static readonly Font SmallBoldFont = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
    public static readonly Font SubheadFont = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
    public static readonly Font HeadingFont = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
    public static readonly Font PageTitleFont = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
    public static readonly Font MetricFont = new Font("Segoe UI", 28F, FontStyle.Bold, GraphicsUnit.Point, 0);

    public const int SidebarWidth = 240;
    public const int SpaceSm = 8;
    public const int SpaceMd = 16;
    public const int SpaceLg = 24;
    public const int ControlHeight = 26;

    public static readonly Color AmberTint = Color.FromArgb(254, 243, 199);
    public static readonly Color AmberInk = Color.FromArgb(146, 64, 14);
    public static readonly Color AmberDeep = Color.FromArgb(180, 83, 9);

    public static readonly Color SkyTint = Color.FromArgb(224, 242, 254);
    public static readonly Color SkyInk = Color.FromArgb(7, 89, 133);
    public static readonly Color SkyDeep = Color.FromArgb(3, 105, 161);

    public static readonly Color NavyTint = Color.FromArgb(230, 237, 247);
    public static readonly Color NavyInk = Color.FromArgb(11, 37, 69);
    public static readonly Color NavyDeep = Color.FromArgb(11, 37, 69);

    public static readonly Color GreenTint = Color.FromArgb(209, 250, 229);
    public static readonly Color GreenInk = Color.FromArgb(6, 95, 70);
    public static readonly Color GreenDeep = Color.FromArgb(21, 128, 61);

    public static Color StatusColor(string status) => status switch
    {
        "Pending" => Warning,
        "Processing" => Info,
        "ReadyForRelease" => SkyDeep,
        "Released" => Success,
        "Rejected" => Danger,
        _ => TextSecondary
    };
}