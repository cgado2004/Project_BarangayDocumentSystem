// =====================================================================
//  PART:    CustomControls - one dashboard tile (heading + big figure)
//  ORIGIN:  Draft - Jonathan F. Del Rosario (the six-card dashboard: a small
//           heading over a 25-point figure, three across, two rows)
//  EDITS:   Clint Wood Gado - rebuilt as one owner-painted control in my
//           palette and type stack instead of two designer labels per card;
//           clickable, keyboard-focusable, and repaints on DPI change
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.CustomControls;

/// <summary>
/// A dashboard tile: a quiet uppercase heading and one large figure under
/// it, drawn as a rounded white card in my palette.
///
/// Jonathan's dashboard drew this with two Labels per card, twelve labels
/// in all, each with its own font and colour set in the designer. I turned
/// the pair into one control so the dashboard creates six of these and the
/// look is decided in exactly one place - the same reason PillButton and
/// Card exist. It is a Button underneath, which gives me keyboard focus,
/// a Click event and an accessible name for free.
/// </summary>
public sealed class SummaryCard : Button
{
    private string _heading = string.Empty;
    private string _value = "\u2014";
    private bool _hover;

    private Font _headingFont;
    private Font _valueFont;

    public SummaryCard()
    {
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);

        _headingFont = SmallBold;
        _valueFont = new Font(UiFamily, 24f, FontStyle.Bold);
    }

    /// <summary>The label, drawn in small capitals: "PENDING", "COLLECTED".</summary>
    public string Heading
    {
        get => _heading;
        set { _heading = value ?? string.Empty; AccessibleName = _heading; Invalidate(); }
    }

    /// <summary>The figure: "7", "₱1,255.00". Whatever the caller formats.</summary>
    public string Value
    {
        get => _value;
        set { _value = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Colour of the figure. Primary by default; the dashboard
    /// uses Success for money and Warning for the pending count.</summary>
    public Color Accent { get; set; } = Primary;

    /// <summary>Called after a DPI change so the fonts are rebuilt at the
    /// new scale. Cheap enough to call on every OnShown.</summary>
    public void RefreshFonts()
    {
        _headingFont.Dispose();
        _valueFont.Dispose();
        _headingFont = SmallBold;
        _valueFont = new Font(UiFamily, 24f, FontStyle.Bold);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // The parent's colour shows through the corners, so they look
        // genuinely round instead of round-with-square-grey-corners.
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw.Smooth(e.Graphics);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 0 || r.Height <= 0) return;

        // The card: white, hairline border, a lavender wash on hover so it
        // reads as clickable, and the primary outline when it has focus.
        using (var path = Draw.RoundedRect(r, RadiusCard))
        {
            using (var b = new SolidBrush(_hover ? LavenderSoft : Surface))
                e.Graphics.FillPath(b, path);
            using (var p = new Pen(Focused ? Primary : AppTheme.Border, Focused ? 1.5f : 1f))
                e.Graphics.DrawPath(p, path);
        }

        var inner = Rectangle.Inflate(r, -CardPad, -16);
        if (inner.Width <= 0 || inner.Height <= 0) return;

        var headingBox = new Rectangle(inner.X, inner.Y, inner.Width, _headingFont.Height + 2);
        var valueBox = new Rectangle(inner.X, headingBox.Bottom + 4, inner.Width,
                                     Math.Max(0, inner.Bottom - headingBox.Bottom - 4));

        TextRenderer.DrawText(e.Graphics, _heading.ToUpperInvariant(), _headingFont, headingBox, Muted,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        TextRenderer.DrawText(e.Graphics, _value, _valueFont, valueBox, Accent,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _headingFont.Dispose();
            _valueFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
