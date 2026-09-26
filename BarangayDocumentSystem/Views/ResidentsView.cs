// =====================================================================
//  PART:    Views - the resident registry: search, add, edit, delete, file a request
//  ORIGIN:  Draft - Jonathan F. Del Rosario (the screen: search box, action buttons, grid)
//           Fdraft - Frent Dhieniel Raborar (this file's place in the tree)
//           the code and comments are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.2: writes go through ViewBase.Persist; StyleGrid moved
//           to UiFactory; the base class now holds the repository
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Views;

/// <summary>
/// The resident registry: search, add, edit, delete, and file a request.
///
/// When a row is selected I immediately show that resident's request history
/// underneath the grid. I did that so a clerk can answer "has he already got
/// one of these?" without navigating away and losing their place.
/// </summary>
public sealed class ResidentsView : ViewBase
{
    private readonly FeeSchedule _fees;

    public event EventHandler<(string View, string? Filter)>? RequestNavigate;

    private readonly RoundedTextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly DataGridView _history = new();
    private readonly Label _historyTitle = new();

    private readonly PillButton _btnAdd    = new() { Text = "Add resident" };
    private readonly PillButton _btnEdit   = new() { Text = "Edit",   Look = PillButton.Style.Outline };
    private readonly PillButton _btnDelete = new() { Text = "Delete", Look = PillButton.Style.Outline, Accent = Danger };
    private readonly PillButton _btnNewReq = new() { Text = "New request" };

    public ResidentsView(IBarangayRepository repository, FeeSchedule fees) : base(repository)
    {
        _fees = fees ?? throw new ArgumentNullException(nameof(fees));

        var body = new SmoothPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        // ---- history (docked bottom, so it keeps its height when resizing) ----
        var historyCard = new Card { Dock = DockStyle.Bottom, Height = 190, Margin = new Padding(0, Gap, 0, 0) };
        _historyTitle.Text = "Select a resident to see their requests";
        _historyTitle.Font = Subhead;
        _historyTitle.ForeColor = Ink;
        _historyTitle.Dock = DockStyle.Top;
        _historyTitle.Height = 28;
        _historyTitle.BackColor = Color.Transparent;
        UiFactory.StyleGrid(_history);
        _history.Dock = DockStyle.Fill;
        historyCard.Controls.Add(_history);
        historyCard.Controls.Add(_historyTitle);

        // ---- the main grid ----
        var gridCard = new Card { Dock = DockStyle.Fill };
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => { UpdateButtons(); ShowHistory(); };
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };
        gridCard.Controls.Add(_grid);

        // ---- toolbar ----
        var bar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };

        _search.Width = 320;
        _search.PlaceholderText = "Search name, purok, contact, occupation…";
        _search.Location = new Point(0, 8);
        _search.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        // I filter as the user types, because a search box that needs a
        // button press is not really a search box.
        _search.Inner.TextChanged += (_, _) => LoadGrid();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        foreach (var b in new[] { _btnAdd, _btnNewReq, _btnEdit, _btnDelete })
        {
            b.Width = b == _btnAdd || b == _btnNewReq ? 132 : 92;
            b.Margin = new Padding(8, 0, 0, 0);
            actions.Controls.Add(b);
        }

        _btnAdd.Click    += (_, _) => AddResident();
        _btnEdit.Click   += (_, _) => EditSelected();
        _btnDelete.Click += (_, _) => DeleteSelected();
        _btnNewReq.Click += (_, _) => NewRequestForSelected();

        bar.Controls.Add(_search);
        bar.Controls.Add(actions);

        body.Controls.Add(gridCard);
        body.Controls.Add(historyCard);
        body.Controls.Add(bar);

        Controls.Add(body);
    }

    public override void OnShown() => LoadGrid();

    /// <summary>I apply a purok filter that arrived from the dashboard.</summary>
    public void ApplyFilter(string? filter)
    {
        _search.Text = filter ?? string.Empty;
        LoadGrid();
    }

    private void LoadGrid()
    {
        var rows = Repository.SearchResidents(_search.Text).ToList();

        _grid.DataSource = rows.Select(r => new
        {
            r.ResidentId,
            Name = r.GetSortableName(),
            Age = r.GetAge(),
            r.Purok,
            Contact = r.ContactNumber,
            Classification = r.GetClassificationText(),
            Voter = r.IsRegisteredVoter ? "Yes" : "No"
        }).ToList();

        if (_grid.Columns.Contains("ResidentId"))
        {
            _grid.Columns["ResidentId"]!.HeaderText = "ID";
            _grid.Columns["ResidentId"]!.FillWeight = 40;
            _grid.Columns["Age"]!.FillWeight = 40;
            _grid.Columns["Voter"]!.FillWeight = 45;
            _grid.Columns["Name"]!.FillWeight = 150;
            _grid.Columns["Classification"]!.FillWeight = 130;
        }

        UpdateButtons();
        ShowHistory();
    }

    private Resident? Selected()
    {
        if (_grid.CurrentRow is null) return null;
        var idCell = _grid.CurrentRow.Cells["ResidentId"].Value;
        if (idCell is null) return null;
        int id = Convert.ToInt32(idCell);
        return Repository.FindResident(id);
    }

    private void UpdateButtons()
    {
        bool any = Selected() is not null;
        _btnEdit.Enabled = any;
        _btnDelete.Enabled = any;
        _btnNewReq.Enabled = any;
    }

    private void ShowHistory()
    {
        var r = Selected();
        if (r is null)
        {
            _history.DataSource = null;
            _historyTitle.Text = "Select a resident to see their requests";
            return;
        }

        _historyTitle.Text = $"Requests filed by {r.GetFullName()}";
        _history.DataSource = r.Requests.Select(q => new
        {
            Reference = q.GetReferenceNumber(),
            Document = FeeSchedule.NameOf(q.DocumentType),
            q.Purpose,
            Status = q.Status.ToString(),
            Fee = DisplayFormat.PesoOrFree(q.Fee),
            Paid = q.IsPaid ? "Yes" : (q.Fee == 0 ? "—" : "No")
        }).ToList();
    }

    private void AddResident()
    {
        using var form = new ResidentForm();
        if (form.ShowDialog(this) != DialogResult.OK || form.Result is null) return;

        var details = form.Result;
        Persist(() => Repository.AddResident(details), "The new resident");
        LoadGrid();
    }

    private void EditSelected()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new ResidentForm(r);
        if (form.ShowDialog(this) != DialogResult.OK || form.Result is null) return;

        var details = form.Result;
        Persist(() => Repository.UpdateResident(r, details), "The change to " + r.GetFullName());
        LoadGrid();
    }

    private void DeleteSelected()
    {
        var r = Selected();
        if (r is null) return;

        int count = r.Requests.Count;
        string warning = count > 0
            ? $"\n\nThis will also delete {count} document request(s) on record."
            : string.Empty;

        if (!Dialog.ConfirmDanger(this,
                $"Delete {r.GetFullName()}?{warning}",
                "Confirm deletion"))
            return;

        Persist(() => Repository.RemoveResident(r), "The deletion");
        LoadGrid();
    }

    private void NewRequestForSelected()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new RequestForm(r, _fees);
        if (form.ShowDialog(this) != DialogResult.OK || form.Input is null) return;

        var type = form.SelectedType;
        var purpose = form.Purpose;
        var input = form.Input;

        // The form has already refused a blocked assessment, so the only
        // way this throws is the database - and then I stay on this screen
        // instead of jumping to a queue that does not have the request.
        if (Persist(() => Repository.CreateRequest(r, type, purpose, input), "The request"))
            RequestNavigate?.Invoke(this, ("requests", null));
    }
}
