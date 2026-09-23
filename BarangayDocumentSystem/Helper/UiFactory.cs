using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem.Helper;

/// <summary>
/// The responsive layout toolkit, v3.1 core fix 4.
///
/// Every form in the app builds its interface through this factory. The
/// important part is <see cref="Grid"/>: a TableLayoutPanel whose columns
/// are PERCENTAGE widths, so fields share the available width in fixed
/// proportions and the layout re-flows when the dialog or the window is
/// resized, instead of overflowing the way fixed-pixel coordinates do.
///
/// The factory is also where the app's custom controls live - Card,
/// PillButton, Badge, Chip, RoundedTextBox, BarRow and the SmoothPanel -
/// so "a control that looks right" is one using away in every form.
/// </summary>
public static class UiFactory
{
    // =================================================================
    //  Standard controls, pre-styled
    // =================================================================

    /// <summary>A small grey label like the ones over my fields.</summary>
    public static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = SmallBold,
        ForeColor = Muted,
        AutoSize = false,
        Height = 20,
        BackColor = Color.Transparent
    };

    /// <summary>An ordinary reading label.</summary>
    public static Label Label(string text, Font? font = null, Color? color = null) => new()
    {
        Text = text,
        Font = font ?? Body,
        ForeColor = color ?? Ink,
        AutoSize = true,
        BackColor = Color.Transparent
    };

    /// <summary>A text input that matches the design.</summary>
    public static RoundedTextBox TextBox(int width = 0)
    {
        var box = new RoundedTextBox();
        if (width > 0) box.Width = width;
        return box;
    }

    /// <summary>A closed dropdown pre-filled from a list.</summary>
    public static ComboBox ComboBox(params object[] items)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Body,
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface,
            ForeColor = Ink,
            Dock = DockStyle.Fill
        };
        if (items.Length > 0) combo.Items.AddRange(items);
        return combo;
    }

    /// <summary>An editable dropdown - the "type or pick" box I use for the
    /// law-violated field on a business clearance.</summary>
    public static ComboBox EditableComboBox(params object[] items)
    {
        var combo = ComboBox(items);
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        return combo;
    }

    /// <summary>A money or count input with spin buttons, clamped to a
    /// sensible range.</summary>
    public static NumericUpDown Number(decimal min, decimal max, decimal value,
                                       decimal increment = 1m, int decimals = 2)
    {
        var input = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Increment = increment,
            DecimalPlaces = decimals,
            ThousandsSeparator = true,
            Font = Body,
            ForeColor = Ink,
            BackColor = Surface,
            Dock = DockStyle.Fill
        };
        return input;
    }

    public static DateTimePicker DatePicker()
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = Body,
            Dock = DockStyle.Fill,
            CalendarMonthBackground = Surface
        };
        return picker;
    }

    public static CheckBox CheckBox(string text)
    {
        var box = new CheckBox
        {
            Text = text,
            Font = Body,
            ForeColor = Ink,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        return box;
    }

    // =================================================================
    //  Responsive grids - the v3.1 fix
    // =================================================================

    /// <summary>
    /// A TableLayoutPanel with STAR-SIZED (percentage) columns.
    ///
    /// A 4-column grid of 25% each gives me four fields that always split
    /// the row evenly, at any dialog width. Fixed pixel widths are what made
    /// the v2 dialogs clip their right-hand fields on a small laptop - the
    /// row was simply wider than the form and WinForms happily drew it off
    /// the edge. Percentages cannot do that: the columns share whatever
    /// width exists.
    /// </summary>
    public static TableLayoutPanel Grid(int columns, params float[] percents)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = columns,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent
        };

        if (percents.Length == 0)
        {
            float even = 100f / Math.Max(1, columns);
            for (int i = 0; i < columns; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, even));
        }
        else
        {
            foreach (float p in percents)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, p));
        }

        return grid;
    }

    /// <summary>
    /// One label-above-field cell for a grid. The control fills its cell, so
    /// a percent-sized column automatically means a percent-sized field.
    /// </summary>
    public static Control Field(string caption, Control input, int height = 58)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Height = height,
            BackColor = Color.Transparent,
            Margin = new Padding(4, 4, 8, 4)
        };

        var label = FieldLabel(caption);
        label.Dock = DockStyle.Top;

        input.Dock = DockStyle.Top;

        host.Controls.Add(input);
        host.Controls.Add(label);
        return host;
    }

    /// <summary>
    /// A right-aligned button bar. Buttons arrive right-to-left, the way a
    /// dialog reads: the primary action sits at the bottom right.
    /// </summary>
    public static Panel ButtonBar(params Button[] buttons)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0),
            WrapContents = false
        };

        foreach (Button b in buttons)
        {
            b.Margin = new Padding(8, 0, 0, 0);
            bar.Controls.Add(b);
        }

        return bar;
    }

    /// <summary>A primary pill button.</summary>
    public static PillButton PrimaryButton(string text, int width = 120) => new()
    {
        Text = text, Width = width
    };

    /// <summary>A secondary (outlined) pill button.</summary>
    public static PillButton SecondaryButton(string text, int width = 110,
                                              Color? accent = null) => new()
    {
        Text = text,
        Width = width,
        Look = PillButton.Style.Outline,
        Accent = accent ?? Primary
    };
}

/// <summary>
/// A panel that repaints without flicker, v3.1 core fix 5.
///
/// WinForms paints a scrolling panel in two passes - background, then
/// children - and the gap between them is visible as flicker whenever the
/// user scrolls. I switch on WS_EX_COMPOSITED (0x02000000), which makes
/// Windows paint the whole window into one buffer off-screen and blit it
/// once, and I double-buffer the panel itself. Every scrolling surface in
/// the app - the three views, the dialog bodies - is a SmoothPanel, so
/// scrolling looks like it is supposed to: smooth.
/// </summary>
public class SmoothPanel : Panel
{
    private const int WS_EX_COMPOSITED = 0x02000000;

    public SmoothPanel()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
        UpdateStyles();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_COMPOSITED;
            return cp;
        }
    }
}

/// <summary>
/// A rounded card with an optional soft shadow. This is the box I put almost
/// everything else inside.
/// </summary>
public class Card : Panel
{
    public int CornerRadius { get; set; } = RadiusCard;
    public Color CardColor { get; set; } = Surface;
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
        Padding = new Padding(CardPad);
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
                              LavenderSoft, Lavender);
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
                e.Graphics.FillRectangle(bar, r.X, r.Y, 4, r.Height);
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
    public Color Accent { get; set; } = Primary;

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
        Font = BodyBold;
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
                using (var b = new SolidBrush(Enabled ? fill : MutedSoft))
                    e.Graphics.FillPath(b, path);
                textColor = Color.White;
                break;

            case Style.Outline:
                if (_hover && Enabled)
                    using (var b = new SolidBrush(Color.FromArgb(22, Accent)))
                        e.Graphics.FillPath(b, path);
                using (var p = new Pen(Enabled ? Accent : MutedSoft, 1.5f))
                    e.Graphics.DrawPath(p, path);
                textColor = Enabled ? Accent : MutedSoft;
                break;

            default: // Quiet
                if (_hover && Enabled)
                    using (var b = new SolidBrush(Color.FromArgb(18, Accent)))
                        e.Graphics.FillPath(b, path);
                textColor = Enabled ? Accent : MutedSoft;
                break;
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, r, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
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

    public Color Accent { get; set; } = Primary;

    public Chip()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Font = SmallBold;
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
                : LavenderSoft);
            e.Graphics.FillPath(b, path);
            using var p = new Pen(AppTheme.Border, 1f);
            e.Graphics.DrawPath(p, path);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, r,
            Selected ? Color.White : Deep,
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
            Font = Body,
            BackColor = Surface,
            ForeColor = Ink,
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
        using (var b = new SolidBrush(Surface)) e.Graphics.FillPath(b, path);
        using var pen = new Pen(_focused ? Primary : AppTheme.Border,
                                _focused ? 1.8f : 1f);
        e.Graphics.DrawPath(pen, path);
    }
}

/// <summary>
/// One labelled bar in my "requests by document type" panel.
///
/// I use a bar rather than a plain number because the eye compares lengths far
/// faster than it compares digits - you can see which document is busiest
/// without reading anything.
/// </summary>
public class BarRow : Control
{
    public string Caption { get; set; } = string.Empty;
    public int Value { get; set; }
    public int Maximum { get; set; } = 1;

    public BarRow()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
        Height = 40;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Draw.Smooth(g);

        TextRenderer.DrawText(g, Caption, Small,
            new Rectangle(0, 0, Width - 40, 18), Ink,
            TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, Value.ToString(), SmallBold,
            new Rectangle(Width - 38, 0, 36, 18), Primary,
            TextFormatFlags.Right);

        var track = new Rectangle(0, 22, Math.Max(1, Width - 2), 8);
        using (var bg = new SolidBrush(LavenderSoft))
        using (var path = Draw.RoundedRect(track, 4))
            g.FillPath(bg, path);

        // I guard against dividing by zero, and I always draw at least a sliver
        // so a count of 1 is still visible.
        int denom = Math.Max(1, Maximum);
        int w = Math.Max(6, (int)(track.Width * (Value / (double)denom)));

        var fill = new Rectangle(track.X, track.Y, w, track.Height);
        using (var brush = new LinearGradientBrush(
                   new Rectangle(fill.X, fill.Y, Math.Max(2, fill.Width), fill.Height),
                   Primary, Periwinkle, 0f))
        using (var path = Draw.RoundedRect(fill, 4))
            g.FillPath(brush, path);
    }
}
