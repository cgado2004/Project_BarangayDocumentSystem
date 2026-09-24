using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BarangayDocumentSystem.Helpers
{
    /// <summary>
    /// The navy design system, ported to C# 7.3 / .NET Framework 4.7.2 from the
    /// arena branch's Helper/AppTheme.cs. Tokens, bundled-font resolution with a
    /// per-role cache, and the shared drawing/style helpers. Styling only - no
    /// data, no behavior.
    /// </summary>
    public static class ModernTheme
    {
        // ---- palette (v3.1.2 spec) ----
        public static readonly Color Canvas = Color.FromArgb(0xF8, 0xFA, 0xFC);
        public static readonly Color Ink = Color.FromArgb(0x0F, 0x17, 0x2A);
        public static readonly Color Muted = Color.FromArgb(0x64, 0x74, 0x8B);
        public static readonly Color PrimaryNavy = Color.FromArgb(0x1E, 0x3A, 0x8A);
        public static readonly Color HeroNavy = Color.FromArgb(0x1B, 0x36, 0x5D);
        public static readonly Color Gold = Color.FromArgb(0xF2, 0xB1, 0x1B);
        public static readonly Color GoldSoft = Color.FromArgb(0xFF, 0xF0, 0xCC);
        public static readonly Color Crimson = Color.FromArgb(0xDC, 0x26, 0x26);
        public static readonly Color CardBorder = Color.FromArgb(0xE2, 0xE8, 0xF0);
        public static readonly Color RowAlt = Color.FromArgb(0xF8, 0xFA, 0xFC);
        public static readonly Color SelectedRow = Color.FromArgb(0xD8, 0xE3, 0xFA);
        public static readonly Color SlateInk = Color.FromArgb(0x1E, 0x29, 0x3B);
        public static readonly Color Sky = Color.FromArgb(0x02, 0x84, 0xC7);
        public static readonly Color Emerald = Color.FromArgb(0x05, 0x96, 0x69);
        public static readonly Color Amber = Color.FromArgb(0xD9, 0x77, 0x06);

        // ---- fonts: bundled Assets\fonts first, then installed, then Segoe UI ----
        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();
        private static string uiFamily;
        private static bool resolved;

        public static string UiFamily
        {
            get { if (!resolved) Resolve(); return uiFamily; }
        }

        /// <summary>Called once from Program.cs (and lazily as a safety net).</summary>
        public static void Resolve()
        {
            if (resolved) return;
            resolved = true;

            var bundled = new PrivateFontCollection();
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "fonts");
                if (Directory.Exists(dir))
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".ttf" || ext == ".otf") bundled.AddFontFile(file);
                    }
                }
            }
            catch { /* unreadable fonts fall through to the installed probe */ }

            var candidates = new[]
            {
                "Inter", "Inter Display", "Plus Jakarta Sans",
                "SF Pro Display", "Roboto", "Segoe UI Variable", "Segoe UI", "Tahoma"
            };

            uiFamily = candidates[candidates.Length - 1];
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (bundled.Families.Length > 0)
                    foreach (var f in bundled.Families) names.Add(f.Name);
                else
                    using (var installed = new InstalledFontCollection())
                        foreach (var f in installed.Families) names.Add(f.Name);
            }
            catch { /* keep the last-resort family */ }

            foreach (var name in candidates)
            {
                if (names.Contains(name)) { uiFamily = name; break; }
            }
        }

        private static Font Role(string role, Func<Font> make)
        {
            Font font;
            if (!Cache.TryGetValue(role, out font)) Cache[role] = font = make();
            return font;
        }

        public static Font F(float size, bool bold)
        {
            return Role(size.ToString() + (bold ? "b" : "r"),
                () => new Font(UiFamily, size, bold ? FontStyle.Bold : FontStyle.Regular));
        }

        // ---- drawing helpers ----
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>Soft two-step shadow under a card rectangle.</summary>
        public static void Shadow(Graphics g, Rectangle r, int radius)
        {
            using (var a = RoundedRect(new Rectangle(r.X + 2, r.Y + 2, r.Width, r.Height), radius))
            using (var b = new SolidBrush(Color.FromArgb(12, Ink))) g.FillPath(b, a);
            using (var c = RoundedRect(new Rectangle(r.X + 3, r.Y + 3, r.Width, r.Height), radius))
            using (var d = new SolidBrush(Color.FromArgb(10, Ink))) g.FillPath(d, c);
        }

        // ---- shared styling ----
        /// <summary>Navy-header table: white body, light-blue selection, no row headers.</summary>
        public static void StyleGrid(DataGridView g)
        {
            typeof(DataGridView).GetProperty("DoubleBuffered")
                .SetValue(g, true, null);

            g.BackgroundColor = Canvas;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = CardBorder;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = PrimaryNavy;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = F(9f, true);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimaryNavy;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 38;
            g.RowHeadersVisible = false;
            g.RowTemplate.Height = 32;
            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = Ink;
            g.DefaultCellStyle.SelectionBackColor = SelectedRow;
            g.DefaultCellStyle.SelectionForeColor = Ink;
            g.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            g.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectedRow;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = Ink;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        /// <summary>Navy solid primary action; outlined secondary.</summary>
        public static void StylePrimary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = PrimaryNavy;
            b.ForeColor = Color.White;
            b.Font = F(9f, true);
            b.Cursor = Cursors.Hand;
        }

        public static void StyleSecondary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = PrimaryNavy;
            b.FlatAppearance.MouseOverBackColor = SelectedRow;
            b.BackColor = Color.White;
            b.ForeColor = PrimaryNavy;
            b.Font = F(9f, false);
            b.Cursor = Cursors.Hand;
        }

        /// <summary>White stat card with the 4px accent on the left edge,
        /// an 11px uppercase heading in the accent colour and a 27pt value.
        /// Painted on the EXISTING panel - nothing is re-parented.</summary>
        public static void StyleStatCard(Panel card, Label heading, Label value, Color accent)
        {
            card.BackColor = Color.White;
            card.Padding = new Padding(18, 12, 10, 8);
            card.Resize += delegate { card.Invalidate(); };
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                var g = e.Graphics;
                Smooth(g);
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var p = new Pen(CardBorder, 1f)) g.DrawRectangle(p, r);
                using (var bar = new SolidBrush(accent))
                    g.FillRectangle(bar, 0, 0, 4, card.Height);
            };

            heading.BackColor = Color.White;
            heading.Font = F(8.25f, true);
            heading.ForeColor = accent;
            heading.Text = heading.Text.ToUpperInvariant();
            heading.Padding = new Padding(6, 0, 0, 0);

            value.BackColor = Color.White;
            value.Font = F(20f, true);
            value.ForeColor = Ink;
            value.Padding = new Padding(4, 0, 0, 0);
        }

        /// <summary>The sidebar: white, seal, gold BRAND overline, bold name,
        /// navy/gold nav buttons. Restyles the EXISTING controls by name and
        /// adds only the seal picture box and the bold brand label.</summary>
        public static void StyleSidebar(Form form, Panel sidebar, Label brandOverline,
                                        Control sealAnchor, Button[] nav, Button active)
        {
            sidebar.BackColor = Color.White;
            sidebar.Padding = new Padding(14, 18, 14, 12);

            brandOverline.BackColor = Color.White;
            brandOverline.Font = F(9f, true);
            brandOverline.ForeColor = Gold;
            brandOverline.Text = "B A R A N G A Y";

            var brand = new Label
            {
                Text = "Magugpo\nPoblacion",
                Font = F(13f, true),
                ForeColor = Ink,
                BackColor = Color.White,
                AutoSize = false,
                Size = new Size(180, 56),
                Location = new Point(sealAnchor.Left, sealAnchor.Bottom + 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            sidebar.Controls.Add(brand);
            brand.BringToFront();

            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "barangay-logo.png");
            if (File.Exists(logoPath))
            {
                var seal = new PictureBox
                {
                    Size = new Size(56, 56),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.White,
                    Location = new Point(sealAnchor.Left, Math.Max(8, sealAnchor.Top - 60))
                };
                using (var stream = new FileStream(logoPath, FileMode.Open, FileAccess.Read))
                    seal.Image = Image.FromStream(stream);
                sidebar.Controls.Add(seal);
                seal.BringToFront();
            }

            foreach (var button in nav) StyleNavButton(button, button == active);

            // the sidebar's quiet sign-off, bottom-left, like the reference
            var city = new Label
            {
                Text = "City of Tagum",
                AutoSize = false,
                Size = new Size(180, 18),
                Location = new Point(16, sidebar.Height - 48),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = F(8.5f, false),
                ForeColor = Muted,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var province = new Label
            {
                Text = "Davao del Norte",
                AutoSize = false,
                Size = new Size(180, 18),
                Location = new Point(16, sidebar.Height - 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = F(8.5f, false),
                ForeColor = Muted,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            sidebar.Controls.Add(city);
            sidebar.Controls.Add(province);
        }

        public static void StyleNavButton(Button b, bool active)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(40, 0, 0, 0);   // room for the glyph
            b.Font = F(10f, active);
            b.Cursor = Cursors.Hand;
            b.BackColor = Color.White;
            b.ForeColor = active ? Color.White : Muted;

            b.Paint -= PaintNavIcon;
            b.Paint -= PaintNavPill;
            if (active)
                b.Paint += PaintNavPill;   // paints gradient pill + gold bar + white glyph + white text
            else
                b.Paint += PaintNavIcon;   // paints the muted glyph only
        }

        private static Pen NavPen(Color c)
        {
            return new Pen(c, 1.6f);
        }

        /// <summary>The small line-icon in front of each nav label, drawn
        /// from simple strokes - a grid square, a person, a document.</summary>
        private static void DrawNavGlyph(Graphics g, Button b, Color color)
        {
            string kind = b.Name ?? string.Empty;
            int x = 14, cy = b.Height / 2;
            using (var pen = NavPen(color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (kind.IndexOf("Dashboard", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    g.DrawRectangle(pen, x, cy - 6, 12, 12);
                    g.DrawLine(pen, x, cy, x + 12, cy);
                    g.DrawLine(pen, x + 6, cy - 6, x + 6, cy + 6);
                }
                else if (kind.IndexOf("Resident", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    g.DrawEllipse(pen, x + 3, cy - 8, 7, 7);
                    g.DrawArc(pen, x, cy + 1, 13, 9, 180, 180);
                }
                else
                {
                    g.DrawRectangle(pen, x + 1, cy - 7, 10, 14);
                    g.DrawLine(pen, x + 3, cy - 3, x + 9, cy - 3);
                    g.DrawLine(pen, x + 3, cy, x + 9, cy);
                    g.DrawLine(pen, x + 3, cy + 3, x + 7, cy + 3);
                }
            }
        }

        private static void PaintNavIcon(object sender, PaintEventArgs e)
        {
            DrawNavGlyph(e.Graphics, (Button)sender, Muted);
        }

        /// <summary>The active item: a rounded navy gradient pill with the
        /// gold left bar. Painted over the button's own flat surface (which
        /// hides the base text), then the text and glyph are drawn back on
        /// top in white - no designer change, no new control.</summary>
        private static void PaintNavPill(object sender, PaintEventArgs e)
        {
            var b = (Button)sender;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pill = new Rectangle(2, 2, b.Width - 5, b.Height - 5);
            using (var path = RoundedRect(pill, 10))
            using (var brush = new LinearGradientBrush(
                       new Rectangle(pill.X, pill.Y, Math.Max(2, pill.Width), Math.Max(2, pill.Height)),
                       PrimaryNavy, Color.FromArgb(0x2B, 0x4C, 0xA8), 0f))
            {
                g.FillPath(brush, path);
            }
            using (var bar = new SolidBrush(Gold))
                g.FillRectangle(bar, pill.X + 7, pill.Y + (pill.Height - 18) / 2, 4, 18);

            DrawNavGlyph(g, b, Color.White);
            TextRenderer.DrawText(g, b.Text, b.Font,
                new Rectangle(38, 0, b.Width - 44, b.Height), Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
