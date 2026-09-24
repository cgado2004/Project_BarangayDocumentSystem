using System;
using System.Drawing;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Tests
{
    /// <summary>
    /// Checks the navy restyle's foundations: the palette tokens still match
    /// the approved design, fonts resolve and are cached per role, and the
    /// drawing helpers produce valid output. Registered from Program.Main:
    ///
    ///     Run("Navy theme tokens, fonts, and drawing helpers", ThemeTokens.Run);
    ///
    /// Deliberately touches no form, so it runs in the same headless-friendly
    /// pass as the fee and workflow checks.
    /// </summary>
    internal static class ThemeTokens
    {
        public static void Run()
        {
            Check(ModernTheme.Canvas.ToArgb() == Color.FromArgb(0xF8, 0xFA, 0xFC).ToArgb(),
                "Canvas token no longer matches the design (#F8FAFC).");
            Check(ModernTheme.Ink.ToArgb() == Color.FromArgb(0x0F, 0x17, 0x2A).ToArgb(),
                "Ink token no longer matches the design (#0F172A).");
            Check(ModernTheme.Muted.ToArgb() == Color.FromArgb(0x64, 0x74, 0x8B).ToArgb(),
                "Muted token no longer matches the design (#64748B).");
            Check(ModernTheme.PrimaryNavy.ToArgb() == Color.FromArgb(0x1E, 0x3A, 0x8A).ToArgb(),
                "Primary navy token no longer matches the design (#1E3A8A).");
            Check(ModernTheme.HeroNavy.ToArgb() == Color.FromArgb(0x1B, 0x36, 0x5D).ToArgb(),
                "Hero navy token no longer matches the design (#1B365D).");
            Check(ModernTheme.Gold.ToArgb() == Color.FromArgb(0xF2, 0xB1, 0x1B).ToArgb(),
                "Gold token no longer matches the design (#F2B11B).");

            ModernTheme.Resolve();
            Check(!string.IsNullOrEmpty(ModernTheme.UiFamily),
                "Font family resolution returned an empty family.");

            var regular = ModernTheme.F(9f, false);
            var bold = ModernTheme.F(9f, true);
            Check(ReferenceEquals(regular, ModernTheme.F(9f, false)),
                "Regular fonts are not cached per role.");
            Check(ReferenceEquals(bold, ModernTheme.F(9f, true)),
                "Bold fonts are not cached per role.");

            using (var bitmap = new Bitmap(24, 24))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var path = ModernTheme.RoundedRect(new Rectangle(2, 2, 20, 20), 6))
                {
                    Check(path != null && path.PointCount > 0,
                        "RoundedRect produced an empty path.");
                }
                ModernTheme.Shadow(graphics, new Rectangle(2, 2, 18, 18), 6); // must not throw
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
