using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Theme;

namespace BarangayDocumentSystem.App.Controls;

/// <summary>
/// A rounded card with an optional soft shadow. This is the box I put almost
/// everything else inside.
/// </summary>
public class Card : Panel
{
    public int CornerRadius { get; set; } = AppTheme.RadiusCard;
    public Color CardColor { get; set; } = AppTheme.Surface;
    public Color BorderColor { get; set; } = AppTheme.Border;
    public bool ShowShadow { get; set; } = true;

    /// <summary>When I set this, the card fills with a lavender gradient
    /// instead of flat white. I use it for the hero panel on my dashboard.</summary>
    public bool Gradient { get; set; }

    /// <summary>When I set this, the card gets a coloured bar down its left
    /// edge. I use it on the dashboard tiles so each statistic has its own
    /// colour without me having to tint the whole card.</summary>
    public Color? AccentBar { get; set; }

    public Card()
    {
        // I turn on double buffering because without it my rounded edges
        // flicker badly every time the window is resized.
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(AppTheme.CardPad);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // I deliberately do not call the base method here. Letting the
        // parent's colour show through the corners is what makes them look
        // genuinely round instead of round-with-square-grey-corners.
        if (Parent is not null)
            e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw.Smooth(e.Graphics);

        // I inset by a few pixels so my shadow has somewhere to be drawn.
        var r = new Rectangle(4, 4, Width - 9, Height - 9);
        if (r.Width <= 0 || r.Height <= 0) return;

        if (ShowShadow) Draw.Shadow(e.Graphics, r, CornerRadius);

        if (Gradient)
        {
            Draw.GradientFill(e.Graphics, r, CornerRadius,
                               AppTheme.LavenderSoft, AppTheme.Lavender);
        }
        else
        {
            using var path = Draw.RoundedRect(r, CornerRadius);
            using var brush = new SolidBrush(CardColor);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(BorderColor, 1f);
            e.Graphics.DrawPath(pen, path);

            if (AccentBar is Color accent)
            {
                // I clip to the card's rounded shape first, so the bar follows
                // the corner instead of poking out of it.
                var saved = e.Graphics.Save();
                e.Graphics.SetClip(path);
                using var bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, r.X, r.Y, 5, r.Height);
                e.Graphics.Restore(saved);
            }
        }

        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();   // I repaint, or my rounded corners smear on resize
    }
}

/// <summary>
/// A pill-shaped button. I gave it three looks - solid, outlined, and quiet
/// (text only) - so I can show the main action, a secondary one and a
/// throwaway one without inventing a new control each time.
/// </summary>
public class PillButton : Button
{
    public enum Style { Solid, Outline, Quiet }

    public Style Look { get; set; } = Style.Solid;
    public Color Accent { get; set; } = AppTheme.Primary;

    private bool _hover;
    private bool _down;

    public PillButton()
    {
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        Font = AppTheme.BodyBold;
        Cursor = Cursors.Hand;
        Height = 42;
        MinimumSize = new Size(0, 36);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true;  Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e)   { _down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw.Smooth(e.Graphics);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 0 || r.Height <= 0) return;

        int radius = Height / 2;                 // half the height gives a true pill
        using var path = Draw.RoundedRect(r, radius);

        Color fill = Accent;
        if (_down)       fill = Draw.Shade(Accent, -0.18);
        else if (_hover) fill = Draw.Shade(Accent, 0.10);

        Color textColor;

        switch (Look)
        {
            case Style.Solid:
                using (var b = new SolidBrush(Enabled ? fill : AppTheme.MutedSoft))
                    e.Graphics.FillPath(b, path);
                textColor = Color.White;
                break;

            case Style.Outline:
                if (_hover && Enabled)
                    using (var b = new SolidBrush(Color.FromArgb(22, Accent)))
                        e.Graphics.FillPath(b, path);
                using (var p = new Pen(Enabled ? Accent : AppTheme.MutedSoft, 1.5f))
                    e.Graphics.DrawPath(p, path);
                textColor = Enabled ? Accent : AppTheme.MutedSoft;
                break;

            default: // Quiet
                if (_hover && Enabled)
                    using (var b = new SolidBrush(Color.FromArgb(18, Accent)))
                        e.Graphics.FillPath(b, path);
                textColor = Enabled ? Accent : AppTheme.MutedSoft;
                break;
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, r, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }
}

/// <summary>A small rounded label I use for a status or a count.</summary>
public class Badge : Label
{
    public Color Accent { get; set; } = AppTheme.Primary;

    public Badge()
    {
        DoubleBuffered = true;
        AutoSize = false;
        BackColor = Color.Transparent;
        Font = AppTheme.SmallBold;
        TextAlign = ContentAlignment.MiddleCenter;
        Height = 26;
        MinimumSize = new Size(64, 24);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw.Smooth(e.Graphics);
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 0 || r.Height <= 0) return;

        using var path = Draw.RoundedRect(r, Height / 2);
        using (var b = new SolidBrush(Color.FromArgb(34, Accent)))
            e.Graphics.FillPath(b, path);
        using (var p = new Pen(Color.FromArgb(110, Accent), 1f))
            e.Graphics.DrawPath(p, path);

        TextRenderer.DrawText(e.Graphics, Text, Font, r, Draw.Shade(Accent, -0.25),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>
/// A clickable chip. I use these for the purok filters and the status
/// filters. A chip can be selected, which is how my Requests screen shows
/// which filter is currently active.
/// </summary>
public class Chip : Control
{
    private bool _hover;

    [DefaultValue(false)]
    public bool Selected { get; set; }

    public Color Accent { get; set; } = AppTheme.Primary;

    public Chip()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Font = AppTheme.SmallBold;
        Cursor = Cursors.Hand;
        Height = 32;
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
        Draw.Smooth(e.Graphics);
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 0 || r.Height <= 0) return;

        using var path = Draw.RoundedRect(r, Height / 2);

        if (Selected)
        {
            using var b = new SolidBrush(Accent);
            e.Graphics.FillPath(b, path);
        }
        else
        {
            using var b = new SolidBrush(_hover
                ? Color.FromArgb(26, Accent)
                : AppTheme.LavenderSoft);
            e.Graphics.FillPath(b, path);
            using var p = new Pen(AppTheme.Border, 1f);
            e.Graphics.DrawPath(p, path);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, r,
            Selected ? Color.White : AppTheme.Deep,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>
/// A text box that matches the rest of my design - rounded, with a border
/// that turns blue when it has focus.
///
/// A WinForms TextBox cannot be rounded at all, so I host a real TextBox
/// inside a panel and let the panel draw the shape around it.
/// </summary>
public class RoundedTextBox : Panel
{
    public TextBox Inner { get; }

    private bool _focused;

    public string PlaceholderText
    {
        get => Inner.PlaceholderText;
        set => Inner.PlaceholderText = value;
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => Inner.Text;
        set => Inner.Text = value ?? string.Empty;
    }

    public RoundedTextBox()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Height = 40;
        Padding = new Padding(14, 0, 14, 0);

        Inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = AppTheme.Body,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.Ink,
            Dock = DockStyle.Fill
        };
        Inner.GotFocus  += (_, _) => { _focused = true;  Invalidate(); };
        Inner.LostFocus += (_, _) => { _focused = false; Invalidate(); };

        // A panel cannot vertically centre a borderless TextBox on its own,
        // so I put it in a host panel whose top padding does the centring.
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent,
                               Padding = new Padding(0, 10, 0, 0) };
        host.Controls.Add(Inner);
        Controls.Add(host);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw.Smooth(e.Graphics);
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (r.Width <= 0 || r.Height <= 0) return;

        using var path = Draw.RoundedRect(r, Height / 2);
        using (var b = new SolidBrush(AppTheme.Surface)) e.Graphics.FillPath(b, path);
        using var pen = new Pen(_focused ? AppTheme.Primary : AppTheme.Border,
                                _focused ? 1.8f : 1f);
        e.Graphics.DrawPath(pen, path);
    }
}
