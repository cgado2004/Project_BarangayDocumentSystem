// =====================================================================
//  PART:    Views - the dashboard
//  ORIGIN:  Draft - Jonathan F. Del Rosario (the layout: six summary cards
//           three across, then three breakdown tables - requests by status,
//           by document, residents by purok - each row a shortcut)
//  EDITS:   Clint Wood Gado - ported onto my ViewBase and palette; figures
//           come from the repository's GetStatistics instead of being
//           recomputed on the screen; the cards are SummaryCard controls
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.CustomControls;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Views;

/// <summary>
/// Jonathan's dashboard: six figures at a glance, then three tables that
/// break them down. Every card and every table row is a shortcut - click
/// "Pending" and the request queue opens already filtered to Pending;
/// double-click a purok and the registry opens on that purok.
///
/// What the screen does NOT do is arithmetic. Every number comes from one
/// call to <see cref="IBarangayRepository.GetStatistics"/>, so the figure
/// on a card, the total in the status bar and the row in a table can never
/// disagree with each other - they are the same number.
/// </summary>
public sealed class DashboardView : ViewBase
{
    private readonly Dictionary<string, SummaryCard> _cards = new();
    private readonly DataGridView _statuses = new();
    private readonly DataGridView _types = new();
    private readonly DataGridView _puroks = new();

    /// <summary>Raised when a card or a table row is chosen; the shell
    /// navigates. (View, Filter) - "requests" + "Pending", or "residents" +
    /// a purok name.</summary>
    public event EventHandler<(string View, string? Filter)>? RequestNavigate;

    public DashboardView(IBarangayRepository repository) : base(repository)
    {
        // ---- the six cards, three across, two rows ----
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 228, ColumnCount = 3, RowCount = 2,
            BackColor = Color.Transparent
        };
        for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        for (int i = 0; i < 2; i++) cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        AddCard(cards, "Residents",         Primary, "residents", null);
        AddCard(cards, "Requests",          Primary, "requests",  null);
        AddCard(cards, "Pending",           Warning, "requests",  nameof(RequestStatus.Pending));
        AddCard(cards, "Ready for release", Primary, "requests",  nameof(RequestStatus.ReadyForRelease));
        AddCard(cards, "Issued free",       Success, "requests",  nameof(RequestStatus.Released));
        AddCard(cards, "Collected",         Success, "requests",  nameof(RequestStatus.Released));

        // ---- the three breakdown tables ----
        var tables = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Color.Transparent
        };
        for (int i = 0; i < 3; i++) tables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));

        AddTable(tables, _statuses, "Requests by status");
        AddTable(tables, _types,    "Requests by document");
        AddTable(tables, _puroks,   "Residents by purok");

        _statuses.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Go("requests", Convert.ToString(_statuses.Rows[e.RowIndex].Cells[0].Value));
        };
        _puroks.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Go("residents", Convert.ToString(_puroks.Rows[e.RowIndex].Cells[0].Value));
        };

        Controls.Add(tables);
        Controls.Add(cards);
    }

    private void Go(string view, string? filter) => RequestNavigate?.Invoke(this, (view, filter));

    private void AddCard(TableLayoutPanel panel, string heading, Color accent, string view, string? filter)
    {
        var card = new SummaryCard
        {
            Heading = heading,
            Accent = accent,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, Gap, Gap)
        };
        card.Click += (_, _) => Go(view, filter);
        _cards.Add(heading, card);
        panel.Controls.Add(card);
    }

    private static void AddTable(TableLayoutPanel panel, DataGridView grid, string title)
    {
        var host = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Gap, 0), Padding = new Padding(12) };

        UiFactory.StyleGrid(grid);
        grid.Dock = DockStyle.Fill;
        grid.AccessibleName = title;

        host.Controls.Add(grid);
        host.Controls.Add(new Label
        {
            Text = title, Dock = DockStyle.Top, Height = 36, Padding = new Padding(8, 8, 8, 0),
            Font = Subhead, ForeColor = Ink, BackColor = Color.Transparent
        });
        panel.Controls.Add(host);
    }

    public override void OnShown()
    {
        var s = Repository.GetStatistics();

        foreach (var card in _cards.Values) card.RefreshFonts();

        SetCard("Residents",         s.TotalResidents.ToString());
        SetCard("Requests",          s.TotalRequests.ToString());
        SetCard("Pending",           s.Pending.ToString());
        SetCard("Ready for release", s.ReadyForRelease.ToString());
        SetCard("Issued free",       s.IssuedFreeOfCharge.ToString());
        SetCard("Collected",         DisplayFormat.Peso(s.TotalCollected));

        _statuses.DataSource = new[]
        {
            new { Status = nameof(RequestStatus.Pending),         Count = s.Pending },
            new { Status = nameof(RequestStatus.Processing),      Count = s.Processing },
            new { Status = nameof(RequestStatus.ReadyForRelease), Count = s.ReadyForRelease },
            new { Status = nameof(RequestStatus.Released),        Count = s.Released },
            new { Status = nameof(RequestStatus.Rejected),
                  Count = s.TotalRequests - s.Pending - s.Processing - s.ReadyForRelease - s.Released }
        };
        _types.DataSource  = s.RequestsByDocumentType.Select(x => new { Document = x.Key, Count = x.Value }).ToList();
        _puroks.DataSource = s.ResidentsByPurok.Select(x => new { Purok = x.Key, Count = x.Value }).ToList();

        NarrowCountColumn(_statuses);
        NarrowCountColumn(_types);
        NarrowCountColumn(_puroks);
    }

    private void SetCard(string heading, string value) => _cards[heading].Value = value;

    /// <summary>The count is one or two digits; the name deserves the room.</summary>
    private static void NarrowCountColumn(DataGridView grid)
    {
        if (grid.Columns.Contains("Count")) grid.Columns["Count"]!.FillWeight = 35;
    }
}
