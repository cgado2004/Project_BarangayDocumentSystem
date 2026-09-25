using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Views;

/// <summary>Draft's six summary cards and tabular breakdowns, without duplicate fee logic.</summary>
public sealed class DashboardView : ViewBase
{
    private readonly IBarangayRepository _repository;
    private readonly Dictionary<string, Button> _figures = new();
    private readonly DataGridView _statuses = new();
    private readonly DataGridView _types = new();
    private readonly DataGridView _puroks = new();
    public event EventHandler<(string View, string? Filter)>? RequestNavigate;

    public DashboardView(IBarangayRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 192, ColumnCount = 3, RowCount = 2 };
        for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        for (int i = 0; i < 2; i++) cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        AddFigure(cards, "Residents", "residents", null);
        AddFigure(cards, "Requests", "requests", null);
        AddFigure(cards, "Pending", "requests", "Pending");
        AddFigure(cards, "Ready for release", "requests", "ReadyForRelease");
        AddFigure(cards, "Issued free", "requests", null);
        AddFigure(cards, "Collected", "requests", null);

        var tables = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        for (int i = 0; i < 3; i++) tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        AddTable(tables, _statuses, "Requests by status");
        AddTable(tables, _types, "Requests by document");
        AddTable(tables, _puroks, "Residents by purok");
        _statuses.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Go("requests", Convert.ToString(_statuses.Rows[e.RowIndex].Cells[0].Value)); };
        _puroks.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Go("residents", Convert.ToString(_puroks.Rows[e.RowIndex].Cells[0].Value)); };
        Controls.Add(tables);
        Controls.Add(cards);
    }

    private void Go(string view, string? filter) => RequestNavigate?.Invoke(this, (view, filter));

    private void AddFigure(TableLayoutPanel panel, string label, string view, string? filter)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill, Margin = new Padding(4, 4, 8, 8), FlatStyle = FlatStyle.Flat,
            BackColor = Surface, ForeColor = Primary, Font = Subhead,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 8, 8, 8),
            Cursor = Cursors.Hand, AccessibleName = label
        };
        button.FlatAppearance.BorderColor = Border;
        button.Click += (_, _) => Go(view, filter);
        _figures.Add(label, button);
        panel.Controls.Add(button);
    }

    private static void AddTable(TableLayoutPanel panel, DataGridView grid, string title)
    {
        var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4, 12, 8, 4), BackColor = Surface };
        ResidentsView.StyleGrid(grid);
        grid.Dock = DockStyle.Fill;
        grid.AccessibleName = title;
        host.Controls.Add(grid);
        host.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), Font = SmallBold, ForeColor = Ink });
        panel.Controls.Add(host);
    }

    public override void OnShown()
    {
        var s = _repository.GetStatistics();
        SetFigure("Residents", s.TotalResidents.ToString());
        SetFigure("Requests", s.TotalRequests.ToString());
        SetFigure("Pending", s.Pending.ToString());
        SetFigure("Ready for release", s.ReadyForRelease.ToString());
        SetFigure("Issued free", s.IssuedFreeOfCharge.ToString());
        SetFigure("Collected", DisplayFormat.Peso(s.TotalCollected));
        _types.DataSource = s.RequestsByDocumentType.Select(x => new { Document = x.Key, Count = x.Value }).ToList();
        _puroks.DataSource = s.ResidentsByPurok.Select(x => new { Purok = x.Key, Count = x.Value }).ToList();
        _statuses.DataSource = new[]
        {
            new { Status = nameof(RequestStatus.Pending), Count = s.Pending },
            new { Status = nameof(RequestStatus.Processing), Count = s.Processing },
            new { Status = nameof(RequestStatus.ReadyForRelease), Count = s.ReadyForRelease },
            new { Status = nameof(RequestStatus.Released), Count = s.Released },
            new { Status = nameof(RequestStatus.Rejected), Count = s.TotalRequests - s.Pending - s.Processing - s.ReadyForRelease - s.Released }
        };
    }

    private void SetFigure(string label, string value) => _figures[label].Text = label + "\r\n" + value;
}
