// ---------------------------------------------------------------------------
//  AppTheme.cs - the colours, the lettering and the gaps of the interface.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BarangayDocumentSystem.UI
{
    /// <summary>
    /// The one place that decides how the program looks.
    ///
    /// I wrote it because of a complaint I have heard about barangay systems
    /// and because of one about this program: the screens were cramped and the
    /// lettering was inconsistent. So everything visual goes through here - the
    /// colour of a status, the size of a heading, the gap between two boxes -
    /// and there is one method, Resolve(), that works out which fonts this
    /// particular computer actually has before a single screen is built.
    ///
    /// The colours come from the barangay seal: the deep blue of the shield,
    /// the gold of the sun, the red of the flag. I am not redrawing the logo -
    /// it is the same image file it has always been - but the interface around
    /// it now looks like it belongs to the same barangay.
    /// </summary>
    public static class AppTheme
    {
        // ==================================================================
        //  Colours
        // ==================================================================

        public static readonly Color Ink = Color.FromArgb(0x16, 0x1D, 0x2C);
        public static readonly Color Navy = Color.FromArgb(0x14, 0x2A, 0x5C);
        public static readonly Color Primary = Color.FromArgb(0x1E, 0x47, 0x9B);
        public static readonly Color PrimaryLight = Color.FromArgb(0x4A, 0x76, 0xCE);
        public static readonly Color Gold = Color.FromArgb(0xE0, 0xA0, 0x1C);
        public static readonly Color Crimson = Color.FromArgb(0xB3, 0x2B, 0x2B);

        public static readonly Color Muted = Color.FromArgb(0x5C, 0x66, 0x78);
        public static readonly Color MutedSoft = Color.FromArgb(0x9E, 0xA7, 0xB4);
        public static readonly Color Surface = Color.White;
        public static readonly Color Canvas = Color.FromArgb(0xF4, 0xF6, 0xFA);
        public static readonly Color Border = Color.FromArgb(0xDD, 0xE2, 0xEB);
        public static readonly Color Selection = Color.FromArgb(0xE8, 0xEF, 0xFD);

        public static readonly Color Success = Color.FromArgb(0x1B, 0x7F, 0x4B);
        public static readonly Color SuccessSoft = Color.FromArgb(0xE4, 0xF3, 0xE9);
        public static readonly Color Warning = Color.FromArgb(0xB5, 0x74, 0x00);
        public static readonly Color WarningSoft = Color.FromArgb(0xFD, 0xF2, 0xDF);
        public static readonly Color Danger = Color.FromArgb(0xB3, 0x2B, 0x2B);
        public static readonly Color DangerSoft = Color.FromArgb(0xFB, 0xE9, 0xE9);
        public static readonly Color InfoSoft = Color.FromArgb(0xE8, 0xEF, 0xFD);

        // ==================================================================
        //  Gaps - the 8 point grid
        //
        //  Every space in the interface is one of these numbers. That is the
        //  whole answer to the spacing note: nothing is placed by eye, so
        //  nothing is three pixels out from the thing beside it.
        // ==================================================================

        public const int Gap1 = 4;
        public const int Gap2 = 8;
        public const int Gap3 = 12;
        public const int Gap4 = 16;
        public const int Gap5 = 24;

        public const int PageMargin = 24;
        public const int FieldHeight = 30;
        public const int ButtonHeight = 36;
        public const int RowHeight = 32;
        public const int HeaderHeight = 36;
        public const int SidebarWidth = 236;
        public const int Corner = 8;
        public const int TopBarHeight = 68;

        // ==================================================================
        //  Lettering
        // ==================================================================

        /// <summary>The family the whole program uses. Chosen at startup from
        /// the fonts this computer really has - never hard-coded, because a
        /// font that is not there is drawn by Windows as something else, and
        /// that is how a neat layout turns into a mess on somebody else's
        /// machine.</summary>
        public static string UiFamily { get; private set; }

        public static string MonoFamily { get; private set; }

        public static Font Heading { get; private set; }
        public static Font SubHeading { get; private set; }
        public static Font Body { get; private set; }
        public static Font BodyBold { get; private set; }
        public static Font Small { get; private set; }
        public static Font SmallBold { get; private set; }
        public static Font Metric { get; private set; }
        public static Font Mono { get; private set; }

        /// <summary>
        /// Works out the fonts and keeps them for the life of the program.
        /// Program.cs calls this before it creates any window, so no screen is
        /// ever measured with one font and drawn with another.
        /// </summary>
        public static void Resolve()
        {
            UiFamily = FirstAvailable("Inter", "Segoe UI Variable Text", "Segoe UI", "Tahoma", "Arial");
            MonoFamily = FirstAvailable("Cascadia Mono", "Consolas", "Courier New");

            Release();

            Heading = new Font(UiFamily, 17f, FontStyle.Bold);
            SubHeading = new Font(UiFamily, 12f, FontStyle.Bold);
            Body = new Font(UiFamily, 10f, FontStyle.Regular);
            BodyBold = new Font(UiFamily, 10f, FontStyle.Bold);
            Small = new Font(UiFamily, 8.75f, FontStyle.Regular);
            SmallBold = new Font(UiFamily, 8.75f, FontStyle.Bold);
            Metric = new Font(UiFamily, 24f, FontStyle.Bold);
            Mono = new Font(MonoFamily, 9f, FontStyle.Regular);
        }

        /// <summary>Hands the fonts back to Windows when the program closes.
        /// A program that holds on to them is a program that leaks a little
        /// every time somebody opens it.</summary>
        public static void Release()
        {
            Font[] fonts = { Heading, SubHeading, Body, BodyBold, Small, SmallBold, Metric, Mono };

            foreach (Font font in fonts)
                if (font != null) font.Dispose();
        }

        private static string FirstAvailable(params string[] candidates)
        {
            List<string> installed = new List<string>();

            using (InstalledFontCollection collection = new InstalledFontCollection())
                foreach (FontFamily family in collection.Families) installed.Add(family.Name);

            foreach (string candidate in candidates)
                if (installed.Contains(candidate)) return candidate;

            return FontFamily.GenericSansSerif.Name;
        }

        // ==================================================================
        //  Shared drawing helpers
        // ==================================================================

        public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            if (radius <= 0 || diameter > bounds.Width || diameter > bounds.Height)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();

            return path;
        }

        public static void Smooth(Graphics graphics)
        {
            if (graphics == null) return;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>
        /// The colour of a status, decided in one place.
        ///
        /// A clerk should be able to read a list at a glance: green means done,
        /// amber means the barangay is still waiting for something, blue means
        /// it is clear and legal, red means refused. Because every screen asks
        /// this method, "Pending" is the same amber on the dashboard, in the
        /// queue and on the printed report.
        /// </summary>
        public static Color ColourFor(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Muted;

            string text = status.ToLowerInvariant();

            if (text.Contains("release") || text.Contains("ready")) return Primary;
            if (text.Contains("released") || text.Contains("done") || text.Contains("paid")
                || text.Contains("valid") || text.Contains("active") || text.Contains("permanent"))
                return Success;
            if (text.Contains("cleared") || text.Contains("free") || text.Contains("newcomer")) return Primary;
            if (text.Contains("pending") || text.Contains("processing") || text.Contains("temporary")
                || text.Contains("waits") || text.Contains("locked") || text.Contains("must change"))
                return Warning;
            if (text.Contains("reject") || text.Contains("void") || text.Contains("inactive")
                || text.Contains("refus") || text.Contains("short"))
                return Danger;

            return Muted;
        }

        public static Color SoftColourFor(string status)
        {
            Color colour = ColourFor(status);

            if (colour == Success) return SuccessSoft;
            if (colour == Warning) return WarningSoft;
            if (colour == Danger) return DangerSoft;
            if (colour == Primary) return InfoSoft;

            return Color.FromArgb(0xF1, 0xF3, 0xF7);
        }
    }
}
