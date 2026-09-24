using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using BarangayDocumentSystem.Helpers;

namespace BarangayDocumentSystem.Helpers
{
    /// <summary>
    /// The navy hero banner: gradient hero bar, round white seal disc, title,
    /// subtitle, gold divider and the Punong Barangay line. Painted entirely
    /// with GDI+; the seal is loaded from Assets\barangay-logo.png when present.
    /// </summary>
    public class HeroBanner : Control
    {
        private Image logo;

        public HeroBanner()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Height = 158;
            Dock = DockStyle.Top;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "barangay-logo.png");
            if (File.Exists(path))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    logo = Image.FromStream(stream);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.Smooth(g);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (r.Width <= 2 || r.Height <= 2) return;

            using (var shape = ModernTheme.RoundedRect(r, 20))
            using (var brush = new LinearGradientBrush(r, ModernTheme.HeroNavy, ModernTheme.PrimaryNavy, 20f))
            {
                g.FillPath(brush, shape);
            }

            var saved = g.Save();
            g.SetClip(shape);

            int cx = 96, cy = Height / 2;

            using (var rayPen = new Pen(Color.FromArgb(26, ModernTheme.Gold), 2f))
            {
                for (int i = 0; i < 16; i++)
                {
                    double angle = i * (Math.PI * 2 / 16);
                    g.DrawLine(rayPen, cx, cy,
                        cx + (float)(Math.Cos(angle) * 300),
                        cy + (float)(Math.Sin(angle) * 300));
                }
            }

            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(cx - 58, cy - 58, 116, 116);
                using (var glow = new PathGradientBrush(glowPath))
                {
                    glow.CenterColor = Color.FromArgb(70, Color.White);
                    glow.SurroundColors = new[] { Color.FromArgb(0, Color.White) };
                    g.FillPath(glow, glowPath);
                }
            }

            using (var disc = new SolidBrush(Color.FromArgb(240, Color.White)))
                g.FillEllipse(disc, cx - 46, cy - 46, 92, 92);

            if (logo != null)
            {
                const int box = 78;
                float scale = Math.Min((float)box / logo.Width, (float)box / logo.Height);
                int w = (int)(logo.Width * scale), h = (int)(logo.Height * scale);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, cx - w / 2, cy - h / 2, w, h);
            }

            int textLeft = cx + 74;
            int available = Width - textLeft - 28;
            if (available > 80)
            {
                TextRenderer.DrawText(g, "Barangay Magugpo Poblacion",
                    ModernTheme.F(15f, true),
                    new Rectangle(textLeft, cy - 50, available, 34), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                TextRenderer.DrawText(g, "City of Tagum, Davao del Norte",
                    ModernTheme.F(9.5f, false),
                    new Rectangle(textLeft, cy - 16, available, 24), Color.FromArgb(210, Color.White),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                using (var rule = new Pen(Color.FromArgb(140, ModernTheme.Gold), 2f))
                    g.DrawLine(rule, textLeft, cy + 16, textLeft + 46, cy + 16);

                TextRenderer.DrawText(g, "Punong Barangay   \u00b7   HON. EUGENIA SOLIS HINGPIT, MD",
                    ModernTheme.F(8.5f, true),
                    new Rectangle(textLeft, cy + 24, available, 22), ModernTheme.GoldSoft,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            g.Restore(saved);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && logo != null) logo.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>A rounded pill badge: "Purok Cristo Rey \u00b7 2".</summary>
    public class PillChip : Control
    {
        public PillChip()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Font = ModernTheme.F(8.5f, true);
            Height = 30;
            Cursor = Cursors.Default;   // display only - no click behaviour in a restyle
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.Smooth(g);
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ModernTheme.RoundedRect(r, Height / 2))
            {
                using (var fill = new SolidBrush(Color.FromArgb(0xEE, 0xF3, 0xFD)))
                    g.FillPath(fill, path);
                using (var pen = new Pen(ModernTheme.CardBorder, 1f))
                    g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, Text, Font, r, ModernTheme.SlateInk,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>One horizontal navy bar with a right-aligned count.</summary>
    public class NavyBar : Control
    {
        public string Caption = "";
        public int Value;
        public int Maximum = 1;

        public NavyBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Height = 40;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) e.Graphics.Clear(Parent.BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            TextRenderer.DrawText(g, Caption, ModernTheme.F(8.5f, false),
                new Rectangle(0, 0, Width - 40, 18), ModernTheme.Ink,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, Value.ToString(), ModernTheme.F(8.5f, true),
                new Rectangle(Width - 38, 0, 36, 18), ModernTheme.PrimaryNavy,
                TextFormatFlags.Right);

            var track = new Rectangle(0, 22, Math.Max(1, Width - 2), 8);
            using (var bg = new SolidBrush(Color.FromArgb(0xEE, 0xF3, 0xFD)))
            using (var path = ModernTheme.RoundedRect(track, 4))
                g.FillPath(bg, path);

            int denom = Math.Max(1, Maximum);
            int w = Math.Max(6, (int)(track.Width * (Value / (double)denom)));
            var fillRect = new Rectangle(track.X, track.Y, w, track.Height);
            using (var brush = new LinearGradientBrush(
                       new Rectangle(fillRect.X, fillRect.Y, Math.Max(2, fillRect.Width), fillRect.Height),
                       ModernTheme.PrimaryNavy, Color.FromArgb(0x6C, 0x84, 0xC4), 0f))
            using (var path = ModernTheme.RoundedRect(fillRect, 4))
                g.FillPath(brush, path);
        }
    }
}
