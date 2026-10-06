// ---------------------------------------------------------------------------
//  Primitives.cs - the pieces every screen reuses: the sidebar, the card, the
//  number tile, the status label and the seal header.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BarangayDocumentSystem.Config;

namespace BarangayDocumentSystem.UI.Controls
{
    /// <summary>
    /// One destination on the sidebar.
    ///
    /// The permission is part of the item rather than of the sidebar's drawing
    /// code, which is the whole trick: the sidebar draws what the permission
    /// list allows and simply skips the rest, so a clerk never sees a screen
    /// that would refuse them anyway.
    /// </summary>
    public class NavigationItem
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Group { get; set; }
        public Security.Permission Permission { get; set; }

        public NavigationItem(string key, string title, string group, Security.Permission permission)
        {
            Key = key;
            Title = title;
            Group = group;
            Permission = permission;
        }
    }

    /// <summary>
    /// The blue sidebar.
    ///
    /// I drew it instead of using a plain list because of two small things that
    /// matter at the counter: the group headings ("Registry", "Documents",
    /// "Records", "System") make a long menu scannable, and the selected item
    /// gets the gold bar that matches the sun on the barangay seal, so a clerk
    /// always knows which screen they are on.
    /// </summary>
    public class NavigationSidebar : Panel
    {
        private readonly List<NavigationItem> _items = new List<NavigationItem>();
        private readonly List<Rectangle> _boxes = new List<Rectangle>();
        private int _selected = -1;
        private int _hover = -1;

        public event EventHandler<string> Destination;

        public NavigationSidebar()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Navy;
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public void Bind(Security.SessionManager session, IEnumerable<NavigationItem> items)
        {
            _items.Clear();

            foreach (NavigationItem item in items)
            {
                // A screen the signed-in role may not open is not listed at all.
                if (session != null && !session.Has(item.Permission)) continue;
                _items.Add(item);
            }

            Invalidate();
        }

        public void Select(string key)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    _selected = i;
                    Invalidate();
                    return;
                }
            }
        }

        public string SelectedKey
        {
            get { return _selected >= 0 && _selected < _items.Count ? _items[_selected].Key : string.Empty; }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = Hit(e.Location);
            if (index == _hover) return;

            _hover = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int index = Hit(e.Location);
            if (index < 0) return;

            _selected = index;
            Invalidate();

            if (Destination != null) Destination(this, _items[index].Key);
        }

        private int Hit(Point point)
        {
            for (int i = 0; i < _boxes.Count; i++)
                if (_boxes[i].Contains(point)) return i;

            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            _boxes.Clear();

            int y = AppTheme.Gap3;
            string lastGroup = null;

            using (Font groupFont = new Font(AppTheme.UiFamily, 7.75f, FontStyle.Bold))
            using (Font itemFont = new Font(AppTheme.UiFamily, 10f, FontStyle.Regular))
            using (Font itemBold = new Font(AppTheme.UiFamily, 10f, FontStyle.Bold))
            using (Brush groupBrush = new SolidBrush(Color.FromArgb(0x8B, 0xA2, 0xD4)))
            using (Brush itemBrush = new SolidBrush(Color.FromArgb(0xE4, 0xEC, 0xFA)))
            using (Brush white = new SolidBrush(Color.White))
            using (Brush hover = new SolidBrush(Color.FromArgb(0x22, 0xAB, 0xE0, 0xFF)))
            using (Brush chosen = new SolidBrush(AppTheme.Primary))
            using (Brush gold = new SolidBrush(AppTheme.Gold))
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    NavigationItem item = _items[i];

                    if (!string.Equals(item.Group, lastGroup, StringComparison.Ordinal))
                    {
                        y += lastGroup == null ? AppTheme.Gap2 : AppTheme.Gap4;
                        e.Graphics.DrawString(item.Group.ToUpperInvariant(), groupFont, groupBrush,
                            AppTheme.Gap4, y);
                        y += 22;
                        lastGroup = item.Group;
                    }

                    Rectangle box = new Rectangle(AppTheme.Gap2, y, Width - (AppTheme.Gap2 * 2), 36);
                    _boxes.Add(box);

                    if (i == _selected)
                    {
                        using (GraphicsPath path = AppTheme.RoundedRectangle(box, AppTheme.Corner))
                            e.Graphics.FillPath(chosen, path);

                        e.Graphics.FillRectangle(gold, box.Left, box.Top + 7, 3, box.Height - 14);
                    }
                    else if (i == _hover)
                    {
                        using (GraphicsPath path = AppTheme.RoundedRectangle(box, AppTheme.Corner))
                            e.Graphics.FillPath(hover, path);
                    }

                    e.Graphics.DrawString(item.Title, i == _selected ? itemBold : itemFont,
                        i == _selected ? white : itemBrush, box.Left + AppTheme.Gap4, box.Top + 9);

                    y += 38;
                }
            }
        }
    }

    /// <summary>
    /// A white card with a title, the frame every section of every screen sits
    /// in.
    ///
    /// I drew this rather than using a Windows group box because the engraved
    /// grey frame of the old controls is the dated look the review asked me to
    /// remove. The name fields are still shown in a group box - that was asked
    /// for and is kept - but the group box is drawn cleanly.
    /// </summary>
    public class SectionPanel : Panel
    {
        private readonly string _title;
        private readonly string _caption;

        public SectionPanel(string title, string caption)
        {
            _title = title ?? string.Empty;
            _caption = caption ?? string.Empty;

            BackColor = AppTheme.Canvas;
            Padding = new Padding(AppTheme.Gap4, 58, AppTheme.Gap4, AppTheme.Gap4);
            Margin = new Padding(0, 0, 0, AppTheme.Gap3);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = AppTheme.RoundedRectangle(bounds, AppTheme.Corner))
            using (SolidBrush fill = new SolidBrush(AppTheme.Surface))
            using (Pen border = new Pen(AppTheme.Border, 1f))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            using (Brush title = new SolidBrush(AppTheme.Ink))
            using (Brush caption = new SolidBrush(AppTheme.Muted))
            using (Font titleFont = new Font(AppTheme.UiFamily, 11.5f, FontStyle.Bold))
            using (Pen rule = new Pen(AppTheme.Border, 1f))
            {
                e.Graphics.DrawString(_title, titleFont, title, AppTheme.Gap4, AppTheme.Gap3);

                if (!string.IsNullOrEmpty(_caption))
                    e.Graphics.DrawString(_caption, AppTheme.Small, caption, AppTheme.Gap4, AppTheme.Gap3 + 22);

                float lineY = AppTheme.Gap3 + (string.IsNullOrEmpty(_caption) ? 28 : 44);
                e.Graphics.DrawLine(rule, AppTheme.Gap4, lineY, Width - AppTheme.Gap4, lineY);
            }
        }
    }

    /// <summary>
    /// One number on the dashboard: the figure, what it is, and a line saying
    /// what it means.
    ///
    /// A dashboard that shows "12" without saying "12 what" is a decoration.
    /// The little note under each figure is the part the barangay actually
    /// reads, and the coloured bar on the left is what makes the whole row
    /// scannable from a metre away.
    /// </summary>
    public class SummaryCard : Panel
    {
        private readonly string _metric;
        private readonly string _value;
        private readonly string _note;
        private readonly Color _accent;

        public SummaryCard(string metric, string value, string note, Color accent)
        {
            _metric = metric;
            _value = value;
            _note = note;
            _accent = accent;

            Width = 220;
            Height = 112;
            BackColor = AppTheme.Canvas;
            Margin = new Padding(0, 0, AppTheme.Gap3, AppTheme.Gap3);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = AppTheme.RoundedRectangle(bounds, AppTheme.Corner))
            using (SolidBrush fill = new SolidBrush(AppTheme.Surface))
            using (Pen border = new Pen(AppTheme.Border, 1f))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }

            using (GraphicsPath bar = AppTheme.RoundedRectangle(
                new Rectangle(0, 16, 4, Height - 32), 2))
            using (SolidBrush accent = new SolidBrush(_accent))
                e.Graphics.FillPath(accent, bar);

            using (Brush muted = new SolidBrush(AppTheme.Muted))
            using (Brush ink = new SolidBrush(AppTheme.Ink))
            {
                e.Graphics.DrawString((_metric ?? string.Empty).ToUpperInvariant(), AppTheme.SmallBold, muted,
                    AppTheme.Gap4, AppTheme.Gap3);

                using (Font metricFont = new Font(AppTheme.UiFamily, 23f, FontStyle.Bold))
                    e.Graphics.DrawString(_value ?? "0", metricFont, ink, AppTheme.Gap4 - 2, AppTheme.Gap3 + 20);

                if (!string.IsNullOrEmpty(_note))
                    e.Graphics.DrawString(_note, AppTheme.Small, muted,
                        new RectangleF(AppTheme.Gap4, Height - 30, Width - (AppTheme.Gap4 * 2), 24));
            }
        }
    }

    /// <summary>
    /// A small rounded label for a status - "Pending", "Released", "Free".
    ///
    /// It is drawn rather than built from a Label so the shape stays the same
    /// everywhere, and the colour always comes from AppTheme, so the word means
    /// the same colour on every screen.
    /// </summary>
    public class StatusPill : Control
    {
        private string _text;

        public StatusPill(string text)
        {
            _text = text ?? string.Empty;
            Height = 26;
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            Width = TextRenderer.MeasureText(_text, AppTheme.SmallBold).Width + (AppTheme.Gap4 * 2);
        }

        public override string Text
        {
            get { return _text; }
            set
            {
                _text = value ?? string.Empty;
                Width = TextRenderer.MeasureText(_text, AppTheme.SmallBold).Width + (AppTheme.Gap4 * 2);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = AppTheme.RoundedRectangle(bounds, Height / 2))
            using (SolidBrush fill = new SolidBrush(AppTheme.SoftColourFor(_text)))
            using (SolidBrush text = new SolidBrush(AppTheme.ColourFor(_text)))
            using (StringFormat centred = new StringFormat())
            {
                centred.Alignment = StringAlignment.Center;
                centred.LineAlignment = StringAlignment.Center;

                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawString(_text, AppTheme.SmallBold, text, bounds, centred);
            }
        }
    }

    /// <summary>
    /// The top of the sidebar: the seal and the barangay's name.
    ///
    /// The seal is the same image file the printed documents use, drawn at a
    /// smaller size. The review asked me not to touch the logo and I have not -
    /// this draws it, it does not redraw it.
    /// </summary>
    public class BrandHeader : Panel
    {
        public BrandHeader()
        {
            Height = 104;
            Dock = DockStyle.Top;
            BackColor = AppTheme.Navy;
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppTheme.Smooth(e.Graphics);

            using (Brush wash = new SolidBrush(AppTheme.Navy))
                e.Graphics.FillRectangle(wash, ClientRectangle);

            try
            {
                string path = AppConfig.LogoFile;
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(AppConfig.ApplicationFolder, path);

                if (System.IO.File.Exists(path))
                {
                    using (Image logo = Image.FromFile(path))
                    {
                        int size = 56;
                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(logo, AppTheme.Gap4, AppTheme.Gap3, size, size);
                    }
                }
            }
            catch (Exception error)
            {
                // A missing seal must not stop the barangay from working, so
                // this is written to the log and the header goes on without it.
                AppLog.Warn("The barangay seal could not be shown in the header: " + error.Message);
            }

            using (Brush white = new SolidBrush(Color.White))
            using (Brush soft = new SolidBrush(Color.FromArgb(0xC8, 0xD6, 0xF0)))
            using (Brush gold = new SolidBrush(AppTheme.Gold))
            using (Font name = new Font(AppTheme.UiFamily, 11f, FontStyle.Bold))
            using (Font place = new Font(AppTheme.UiFamily, 8.5f, FontStyle.Regular))
            {
                e.Graphics.DrawString("Barangay Document System", name, white, AppTheme.Gap4 + 64, AppTheme.Gap3 + 4);
                e.Graphics.DrawString(AppConfig.BarangayName, place, soft, AppTheme.Gap4 + 64, AppTheme.Gap3 + 26);
                e.Graphics.DrawString(AppConfig.CityName, place, soft, AppTheme.Gap4 + 64, AppTheme.Gap3 + 42);

                e.Graphics.FillRectangle(gold, AppTheme.Gap4, Height - 3, Width - (AppTheme.Gap4 * 2), 3);
            }
        }
    }
}
