#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.CustomControls;

public class NavigationSidebar : Panel
{
    private readonly FlowLayoutPanel _host = new();
    private readonly Dictionary<string, NavItem> _items = new();
    private NavItem? _selected;

    public event EventHandler<string>? NavigationChanged;

    public NavigationSidebar()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.SidebarBg;
        Margin = new Padding(0);
        Padding = new Padding(0);

        var brand = BuildBrand();

        _host.Dock = DockStyle.Fill;
        _host.FlowDirection = FlowDirection.TopDown;
        _host.WrapContents = false;
        _host.AutoScroll = true;
        _host.BackColor = AppTheme.SidebarBg;
        _host.Padding = new Padding(14, 6, 14, 14);
        _host.Margin = new Padding(0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.SidebarBg,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(_host, 0, 1);

        Controls.Add(layout);
    }

    private static Control BuildBrand()
    {
        var brand = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SidebarBg,
            Margin = new Padding(0),
            Padding = new Padding(24, 26, 20, 10)
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.SidebarBg,
            Margin = new Padding(0)
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        var small = new Label
        {
            Text = "MAGUGPO POBLACION",
            Dock = DockStyle.Fill,
            Font = AppTheme.SmallBoldFont,
            ForeColor = AppTheme.SidebarActive,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var big = new Label
        {
            Text = "Barangay System",
            Dock = DockStyle.Fill,
            Font = AppTheme.HeadingFont,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        stack.Controls.Add(small, 0, 0);
        stack.Controls.Add(big, 0, 1);
        brand.Controls.Add(stack);
        return brand;
    }

    public void AddItem(string key, string text, string glyph)
    {
        var item = new NavItem(key, glyph, text)
        {
            Width = _host.ClientSize.Width - 28,
            Height = 46
        };
        item.Click += (_, _) => Navigate(key);
        _items[key] = item;
        _host.Controls.Add(item);
    }

    public void Navigate(string key)
    {
        if (!_items.TryGetValue(key, out var item)) return;

        if (!ReferenceEquals(item, _selected))
        {
            if (_selected is not null)
            {
                _selected.IsSelected = false;
                _selected.Invalidate();
            }
            _selected = item;
            item.IsSelected = true;
            item.Invalidate();
        }

        NavigationChanged?.Invoke(this, key);
    }

    private sealed class NavItem : Control
    {
        public string Key { get; }
        public string Glyph { get; }
        public string Label { get; }

        private bool _hover;
        private bool _selected;

        public bool IsSelected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public NavItem(string key, string glyph, string label)
        {
            Key = key;
            Glyph = glyph;
            Label = label;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 0, 4);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.StandardClick |
                ControlStyles.StandardDoubleClick, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color back = _selected || _hover ? AppTheme.SidebarHover : AppTheme.SidebarBg;

            using (var brush = new SolidBrush(back))
                g.FillRectangle(brush, ClientRectangle);

            if (_selected)
            {
                using var accent = new SolidBrush(AppTheme.SidebarActive);
                g.FillRectangle(accent, 0, 9, 3, Height - 18);
            }

            Color fg = _selected ? AppTheme.SidebarTextActive : AppTheme.SidebarText;

            using (var glyphFont = new Font("Segoe UI Symbol", 11F))
            using (var glyphBrush = new SolidBrush(_selected ? AppTheme.SidebarActive : fg))
            {
                g.DrawString(Glyph, glyphFont, glyphBrush,
                    new PointF(18, (Height - 20) / 2f));
            }

            var font = _selected ? AppTheme.BodyBoldFont : AppTheme.BodyFont;
            using var textBrush = new SolidBrush(fg);
            var format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(Label, font, textBrush,
                new RectangleF(50, 0, Width - 58, Height), format);
        }
    }
}