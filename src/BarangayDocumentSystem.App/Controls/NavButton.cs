using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Theme;

namespace BarangayDocumentSystem.App.Controls;

/// <summary>
/// One entry in my left navigation rail.
///
/// I made this a real control with a default constructor so Visual Studio can
/// place it from the designer file and show it on the design surface. An
/// anonymous panel built in code would not appear there at all.
/// </summary>
public class NavButton : Control
{
    private bool _hover;

    /// <summary>True when this is the screen currently open.</summary>
    public bool Active { get; set; }

    /// <summary>The small symbol I draw to the left of the label.</summary>
    public string Glyph { get; set; } = "\u25A0";

    public NavButton()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Height = 48;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Draw.Smooth(g);

        var r = new Rectangle(0, 2, Width - 1, Height - 5);
        if (r.Width <= 2 || r.Height <= 2) return;

        using var shape = Draw.RoundedRect(r, 12);

        Color fg;

        if (Active)
        {
            // The selected item gets the navy gradient, so it reads as the
            // current place rather than just another item.
            using var brush = new LinearGradientBrush(r, AppTheme.Primary, AppTheme.Deep, 0f);
            g.FillPath(brush, shape);

            // A gold tab down the left edge - the same gold as the seal.
            var tab = new Rectangle(r.X + 3, r.Y + 11, 4, r.Height - 22);
            using var tabBrush = new SolidBrush(AppTheme.Gold);
            using var tabPath = Draw.RoundedRect(tab, 2);
            g.FillPath(tabBrush, tabPath);

            fg = Color.White;
        }
        else if (_hover)
        {
            using var brush = new SolidBrush(Color.FromArgb(22, AppTheme.Primary));
            g.FillPath(brush, shape);
            fg = AppTheme.Primary;
        }
        else
        {
            fg = AppTheme.Muted;
        }

        TextRenderer.DrawText(g, Glyph,
            new Font(AppTheme.UiFamily, 13f, FontStyle.Bold),
            new Rectangle(r.X + 18, r.Y, 26, r.Height), fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

        TextRenderer.DrawText(g, Text,
            new Font(AppTheme.UiFamily, 11f, Active ? FontStyle.Bold : FontStyle.Regular),
            new Rectangle(r.X + 52, r.Y, r.Width - 60, r.Height), fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}
