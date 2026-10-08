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
    private readonly TableLayoutPanel _root;
    private readonly TableLayoutPanel _cards = new();
    private readonly TableLayoutPanel _tables = new();
    private bool _cardsAreStacked;

    public override string Title => "Dashboard";
    public override string Subtitle => "Operational overview of residents and document requests";

    public DashboardView(IBarangayRepository repository) : base(repository)
    {
        AutoScroll = true;

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 540,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var root = _root;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 156F));
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

        root.Controls.Add(_cards, 0, 0);
        root.Controls.Add(_tables, 0, 2);

        Controls.Add(root);
        root.SizeChanged += (_, _) => ArrangeCards();
        ArrangeCards();
    }

    public override void RefreshData()
    {
        var stats = Repository.GetStatistics();

        ClearHost(_cards);
        AddCard(MetricCard("Pending Requests", stats.Pending.ToString(),
            "Awaiting action", AppTheme.AmberTint, AppTheme.AmberInk, AppTheme.AmberDeep,
            "requests", "Pending"), 0);
        AddCard(MetricCard("Ready for Release", stats.ReadyForRelease.ToString(),
            "Awaiting pickup", AppTheme.SkyTint, AppTheme.SkyInk, AppTheme.SkyDeep,
            "requests", "ReadyForRelease"), 1);
        AddCard(MetricCard("Total Residents", stats.TotalResidents.ToString(),
            $"{stats.RegisteredVoters} registered voters",
            AppTheme.NavyTint, AppTheme.NavyInk, AppTheme.NavyDeep,
            "residents", null), 2);
        AddCard(MetricCard("Revenue Collected", $"₱{stats.TotalCollected:N0}",
            $"{stats.IssuedFreeOfCharge} issued free",
            AppTheme.GreenTint, AppTheme.GreenInk, AppTheme.GreenDeep,
            "requests", "Released"), 3);

        ClearHost(_tables);
        _tables.Controls.Add(BreakdownCard("Requests by Document Type",
            stats.RequestsByDocumentType, new Padding(0, 0, 8, 0)), 0, 0);
        _tables.Controls.Add(BreakdownCard("Residents by Purok",
            stats.ResidentsByPurok, new Padding(8, 0, 0, 0)), 1, 0);
    }

    private void AddCard(Control card, int index) =>
        _cards.Controls.Add(card, index % _cards.ColumnCount, index / _cards.ColumnCount);

    private void ArrangeCards()
    {
        bool stacked = _root.ClientSize.Width < 700;
        int columns = stacked ? 2 : 4;
        if (_cardsAreStacked == stacked && _cards.ColumnCount == columns) return;

        _cardsAreStacked = stacked;
        var cards = _cards.Controls.Cast<Control>().ToArray();
        _cards.SuspendLayout();
        _cards.Controls.Clear();
        _cards.ColumnStyles.Clear();
        _cards.RowStyles.Clear();
        _cards.ColumnCount = columns;
        _cards.RowCount = stacked ? 2 : 1;
        for (int i = 0; i < columns; i++)
            _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        for (int i = 0; i < _cards.RowCount; i++)
            _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / _cards.RowCount));
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Margin = new Padding(0, 0, 14, stacked ? 14 : 0);
            AddCard(cards[i], i);
        }

        _root.RowStyles[0].Height = stacked ? 312F : 156F;
        _root.Height = stacked ? 696 : 540;
        _cards.ResumeLayout();
    }

    private Control MetricCard(string caption, string value, string note,
        Color tint, Color ink, Color deep, string targetKey, string? argument)
    {
        var card = new MetricCardControl
        {
            Caption = caption,
            Value = value,
            Note = note,
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
            grid.Rows.Add("(no data recorded)", "");
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
        public string Note { get; set; } = "";
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

            using (var valBrush = new SolidBrush(Deep))
                g.DrawString(Value, AppTheme.MetricFont, valBrush, new PointF(16, 42));

            using (var noteBrush = new SolidBrush(Color.FromArgb(150, Ink)))
                g.DrawString(Note, AppTheme.BodyFont, noteBrush, new PointF(20, Height - 36));
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