#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public class DashboardView : ViewBase
{
    private readonly TableLayoutPanel _cards = new();
    private readonly TableLayoutPanel _tables = new();
    private ComboBox _period = new();
    private DashboardPeriod _currentPeriod = DashboardPeriod.AllTime;

    public override string Title => "Dashboard";

    public ComboBox PeriodSelector => _period;

    public DashboardView(IBarangayRepository repository) : base(repository)
    {
        AutoScroll = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 512,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _cards.Dock = DockStyle.Fill;
        _cards.ColumnCount = 4;
        _cards.RowCount = 1;
        _cards.BackColor = AppTheme.Background;
        _cards.Margin = new Padding(0);
        for (int i = 0; i < 4; i++)
            _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _tables.Dock = DockStyle.Fill;
        _tables.ColumnCount = 2;
        _tables.RowCount = 1;
        _tables.BackColor = AppTheme.Background;
        _tables.Margin = new Padding(0);
        _tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _tables.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _period = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.BodyFont,
            Width = 160
        };
        _period.Items.AddRange(new object[]
        {
            "All time", "Today", "This week", "This month", "This year", "Last year"
        });
        _period.SelectedIndex = 0;
        _period.SelectedIndexChanged += (_, _) =>
        {
            _currentPeriod = (DashboardPeriod)_period.SelectedIndex;
            RefreshData();
        };

        root.Controls.Add(_cards, 0, 0);
        root.Controls.Add(_tables, 0, 2);

        Controls.Add(root);
        Resize += (_, _) => ArrangeCards(root);
    }

    private enum DashboardPeriod
    {
        AllTime,
        Today,
        ThisWeek,
        ThisMonth,
        ThisYear,
        LastYear
    }

    private static (DateTime From, DateTime To) PeriodRange(DashboardPeriod period)
    {
        DateTime today = DateTime.Today;
        switch (period)
        {
            case DashboardPeriod.Today:
            {
                return (today, today.AddDays(1).AddTicks(-1));
            }
            case DashboardPeriod.ThisWeek:
            {
                DateTime monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                return (monday, monday.AddDays(7).AddTicks(-1));
            }
            case DashboardPeriod.ThisMonth:
            {
                DateTime first = new DateTime(today.Year, today.Month, 1);
                return (first, first.AddMonths(1).AddTicks(-1));
            }
            case DashboardPeriod.ThisYear:
            {
                DateTime jan1 = new DateTime(today.Year, 1, 1);
                return (jan1, jan1.AddYears(1).AddTicks(-1));
            }
            case DashboardPeriod.LastYear:
            {
                DateTime lastJan1 = new DateTime(today.Year - 1, 1, 1);
                return (lastJan1, lastJan1.AddYears(1).AddTicks(-1));
            }
            default:
            {
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }
    }

    public override void RefreshData()
    {
        var stats = Repository.GetStatistics();

        (DateTime from, DateTime to) = PeriodRange(_currentPeriod);
        decimal revenue;
        IReadOnlyDictionary<string, int> byType;
        if (_currentPeriod == DashboardPeriod.AllTime)
        {
            revenue = stats.TotalCollected;
            byType = stats.RequestsByDocumentType;
        }
        else
        {
            revenue = Repository.Requests
                .Where(r => r.IsPaid && r.DatePaid >= from && r.DatePaid <= to)
                .Sum(r => r.Fee);
            byType = Repository.Requests
                .Where(r => r.DateRequested >= from && r.DateRequested <= to)
                .GroupBy(r => r.GetDocumentName())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        ClearHost(_cards);
        int columns = _cards.ColumnCount;
        _cards.Controls.Add(MetricCard("Pending Requests", stats.Pending.ToString(),
            AppTheme.AmberTint, AppTheme.AmberInk, AppTheme.AmberDeep,
            "requests", "Pending"), 0 % columns, 0 / columns);
        _cards.Controls.Add(MetricCard("Ready for Release", stats.ReadyForRelease.ToString(),
            AppTheme.SkyTint, AppTheme.SkyInk, AppTheme.SkyDeep,
            "requests", "ReadyForRelease"), 1 % columns, 1 / columns);
        _cards.Controls.Add(MetricCard("Total Residents", stats.TotalResidents.ToString(),
            AppTheme.NavyTint, AppTheme.NavyInk, AppTheme.NavyDeep,
            "residents", null), 2 % columns, 2 / columns);
        _cards.Controls.Add(MetricCard("Revenue Collected", $"₱{revenue:N0}",
            AppTheme.GreenTint, AppTheme.GreenInk, AppTheme.GreenDeep,
            "records", null), 3 % columns, 3 / columns);

        ArrangeCards((TableLayoutPanel)_cards.Parent);

        ClearHost(_tables);
        _tables.Controls.Add(BreakdownCard("Requests by Document Type",
            byType, new Padding(0, 0, 8, 0)), 0, 0);
        _tables.Controls.Add(BreakdownCard("Residents by Purok",
            stats.ResidentsByPurok, new Padding(8, 0, 0, 0)), 1, 0);
    }

    private void ArrangeCards(TableLayoutPanel root)
    {
        if (_cards.Controls.Count != 4) return;

        bool narrow = ClientSize.Width < 900;
        int columns = narrow ? 2 : 4;

        if (_cards.ColumnCount != columns)
        {
            _cards.SuspendLayout();
            _cards.ColumnStyles.Clear();
            _cards.RowStyles.Clear();
            _cards.ColumnCount = columns;
            _cards.RowCount = narrow ? 2 : 1;
            for (int i = 0; i < columns; i++)
                _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            for (int i = 0; i < _cards.RowCount; i++)
                _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _cards.RowCount));
            for (int i = 0; i < _cards.Controls.Count; i++)
                _cards.SetCellPosition(_cards.Controls[i], new TableLayoutPanelCellPosition(i % columns, i / columns));
            _cards.ResumeLayout();
        }

        int tableColumns = narrow ? 1 : 2;
        if (_tables.ColumnCount != tableColumns)
        {
            _tables.SuspendLayout();
            _tables.ColumnStyles.Clear();
            _tables.RowStyles.Clear();
            _tables.ColumnCount = tableColumns;
            _tables.RowCount = narrow ? 2 : 1;
            _tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (!narrow) _tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < _tables.RowCount; i++)
                _tables.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _tables.RowCount));
            for (int i = 0; i < _tables.Controls.Count; i++)
            {
                _tables.Controls[i].Margin = narrow
                    ? new Padding(0, 0, 0, i == 0 ? 8 : 0)
                    : new Padding(i % 2 == 0 ? 0 : 8, 0, i % 2 == 0 ? 8 : 0, 0);
                _tables.SetCellPosition(_tables.Controls[i],
                    new TableLayoutPanelCellPosition(i % tableColumns, i / tableColumns));
            }
            _tables.ResumeLayout();
        }

        root.RowStyles[0].Height = narrow ? 272F : 128F;
        root.Height = narrow ? 792 : 512;
    }

    private Control MetricCard(string caption, string value,
        Color tint, Color ink, Color deep, string targetKey, string? argument)
    {
        var card = new MetricCardControl
        {
            Caption = caption,
            Value = value,
            Tint = tint,
            Ink = ink,
            Deep = deep,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0)
        };
        card.Click += (_, _) => NavigateTo(targetKey, argument);
        return card;
    }

    private static Control BreakdownCard(
        string heading, IReadOnlyDictionary<string, int> data, Padding margin)
    {
        var card = new BorderedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            BorderColor = AppTheme.Border,
            Padding = new Padding(1),
            Margin = margin
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Font = AppTheme.SubheadFont,
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Margin = new Padding(0),
            Text = heading
        };

        var grid = new DataGridView();
        UiFactory.StyleGrid(grid);
        grid.Dock = DockStyle.Fill;
        grid.ColumnHeadersVisible = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        grid.Columns.Add("Category", "Category");
        grid.Columns.Add("Count", "Count");
        grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        grid.Columns[1].Width = 80;

        if (data.Count == 0)
            grid.Rows.Add("No data", "");
        else
            foreach (var pair in data.OrderByDescending(p => p.Value).ThenBy(p => p.Key))
                grid.Rows.Add(pair.Key, pair.Value.ToString());

        card.Controls.Add(grid);
        card.Controls.Add(title);
        return card;
    }

    private static void ClearHost(Control parent)
    {
        foreach (Control child in parent.Controls.Cast<Control>().ToList())
        {
            parent.Controls.Remove(child);
            child.Dispose();
        }
    }

    private sealed class MetricCardControl : Control
    {
        public string Caption { get; set; } = "";
        public string Value { get; set; } = "";
        public Color Tint { get; set; } = AppTheme.Surface;
        public Color Ink { get; set; } = AppTheme.TextPrimary;
        public Color Deep { get; set; } = AppTheme.Primary;

        private bool _hover;

        public MetricCardControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
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

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = UiFactory.RoundedPath(rect, 12))
            using (var brush = new SolidBrush(Tint))
                g.FillPath(brush, path);

            if (_hover)
            {
                using var path = UiFactory.RoundedPath(rect, 12);
                using var pen = new Pen(Color.FromArgb(90, Deep), 1.5f);
                g.DrawPath(pen, path);
            }

            using (var capBrush = new SolidBrush(Ink))
                g.DrawString(Caption.ToUpperInvariant(), AppTheme.SmallBoldFont, capBrush,
                    new PointF(20, 20));

            int maxValueWidth = rect.Width - 40;
            using var valBrush = new SolidBrush(Deep);
            if (g.MeasureString(Value, AppTheme.MetricFont).Width <= maxValueWidth)
            {
                g.DrawString(Value, AppTheme.MetricFont, valBrush, new PointF(16, 42));
            }
            else
            {
                using var shrunk = ShrinkToFit(g, Value, maxValueWidth);
                g.DrawString(Value, shrunk, valBrush, new PointF(16, 42));
            }

        }

        private static Font ShrinkToFit(Graphics g, string text, int maxWidth)
        {
            const float minSize = 16F;
            float size = AppTheme.MetricFont.Size;
            Font font = AppTheme.MetricFont;
            while (size > minSize && g.MeasureString(text, font).Width > maxWidth)
            {
                if (!ReferenceEquals(font, AppTheme.MetricFont)) font.Dispose();
                size -= 1F;
                font = new Font(AppTheme.MetricFont.FontFamily, size, FontStyle.Bold);
            }
            return font;
        }
    }

    private sealed class BorderedPanel : Panel
    {
        public Color BorderColor { get; set; } = AppTheme.Border;

        public BorderedPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);
            using var pen = new Pen(BorderColor);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
