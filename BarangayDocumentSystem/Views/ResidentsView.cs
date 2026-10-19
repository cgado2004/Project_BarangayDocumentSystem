#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public class ResidentsView : ViewBase
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Residents";
    public override string Subtitle => "Registry of Barangay Magugpo Poblacion";

    public ResidentsView(IBarangayRepository repository)
        : base(repository)
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(), 0, 1);
        root.Controls.Add(BuildActions(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = UiFactory.Toolbar();
        bar.Margin = new Padding(0, 0, 0, 12);

        var wrap = new Panel
        {
            Dock = DockStyle.Left,
            Width = 440,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 7, 0, 7)
        };

        _search.Dock = DockStyle.Fill;
        _search.Font = AppTheme.BodyFont;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.BackColor = AppTheme.Background;
        CueBanner.Set(_search, "Search by name, purok, or contact number…");
        _search.TextChanged += (_, _) => RefreshData();

        wrap.Controls.Add(_search);
        bar.Controls.Add(wrap);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditResident(); };

        var host = UiFactory.CardHost(_grid);
        host.Margin = new Padding(0, 0, 0, 16);

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = AppTheme.BodyFont;
        _emptyLabel.ForeColor = AppTheme.TextMuted;
        _emptyLabel.BackColor = AppTheme.Surface;
        _emptyLabel.Text = "No residents found";
        _emptyLabel.Visible = false;

        host.Controls.Add(_emptyLabel);
        _emptyLabel.BringToFront();

        return host;
    }

    private Control BuildActions()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };

        // Resident-related controls only. Everything about documents lives on
        // the Requests page.
        var register = UiFactory.PrimaryButton("Add Resident", 140);
        register.Click += (_, _) => RegisterResident();

        var edit = UiFactory.SecondaryButton("Edit", 90);
        edit.Click += (_, _) => EditResident();

        var transfer = UiFactory.SecondaryButton("Transfer", 110);
        transfer.Click += (_, _) => TransferResident();

        actions.Controls.Add(register);
        actions.Controls.Add(edit);
        actions.Controls.Add(transfer);
        return actions;
    }

    public override void RefreshData()
    {
        _grid.DataSource = Repository.SearchResidents(_search.Text.Trim())
            .Select(r => new
            {
                ID = r.ResidentId,
                Name = r.GetSortableName(),
                Age = r.GetAge(),
                Gender = r.Gender.ToString(),
                Purok = r.Purok,
                Contact = r.ContactNumber,
                Residency = $"{r.GetMonthsOfResidency()} mo",
                Voter = r.IsRegisteredVoter ? "Yes" : "No",
                Classification = r.GetClassificationText(),
                Requests = r.Requests.Count
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;

        _emptyLabel.Text = string.IsNullOrWhiteSpace(_search.Text)
            ? "No residents registered yet"
            : $"No residents match \u201c{_search.Text.Trim()}\u201d";
        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    private Resident? Selected()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return Repository.Residents.FirstOrDefault(r => r.ResidentId == id);
    }

    private void RegisterResident()
    {
        using var dialog = new ResidentForm();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var resident = AttemptGet(() => Repository.AddResident(dialog.Details));
        if (resident is null) return;

        RefreshData();
        SetStatus($"Registered {resident.GetFullName()} of {resident.Purok}.");
    }

    private void EditResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new ResidentForm(resident);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        if (!Attempt(() => Repository.UpdateResident(resident, dialog.Details))) return;

        RefreshData();
        SetStatus($"Updated the record for {resident.GetFullName()}.");
    }

    /// <summary>
    /// A resident who moves away is archived, not deleted — the record and the
    /// request history stay on file and can be seen on the Archive page.
    /// </summary>
    private void TransferResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new TransferForm(resident);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string name = resident.GetFullName();
        if (!Attempt(() => Repository.ArchiveResident(resident, dialog.Destination, dialog.Reason)))
            return;

        RefreshData();
        SetStatus($"{name} transferred to {dialog.Destination} and moved to the Archive.");
    }
}

/// <summary>
/// Residents who transferred out of the barangay. Archiving replaces deleting:
/// the record and every request they filed are kept. A resident can be
/// restored if they move back.
/// </summary>
public class ArchiveView : ViewBase
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Archived Residents";
    public override string Subtitle => "Residents who transferred out — records are kept, never deleted";

    public ArchiveView(IBarangayRepository repository) : base(repository)
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(), 0, 1);
        root.Controls.Add(BuildActions(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = UiFactory.Toolbar();
        bar.Margin = new Padding(0, 0, 0, 12);

        var wrap = new Panel
        {
            Dock = DockStyle.Left,
            Width = 440,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 7, 0, 7)
        };

        _search.Dock = DockStyle.Fill;
        _search.Font = AppTheme.BodyFont;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.BackColor = AppTheme.Background;
        CueBanner.Set(_search, "Search archived residents by name, purok, or contact number…");
        _search.TextChanged += (_, _) => RefreshData();

        wrap.Controls.Add(_search);
        bar.Controls.Add(wrap);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;

        var host = UiFactory.CardHost(_grid);
        host.Margin = new Padding(0, 0, 0, 16);

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = AppTheme.BodyFont;
        _emptyLabel.ForeColor = AppTheme.TextMuted;
        _emptyLabel.BackColor = AppTheme.Surface;
        _emptyLabel.Visible = false;

        host.Controls.Add(_emptyLabel);
        _emptyLabel.BringToFront();
        return host;
    }

    private Control BuildActions()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0)
        };

        var edit = UiFactory.SecondaryButton("Edit", 90);
        edit.Click += (_, _) => EditResident();

        var restore = UiFactory.PrimaryButton("Restore to Residents", 180);
        restore.Click += (_, _) => RestoreResident();

        actions.Controls.Add(edit);
        actions.Controls.Add(restore);
        return actions;
    }

    public override void RefreshData()
    {
        _grid.DataSource = Repository.SearchArchivedResidents(_search.Text.Trim())
            .Select(r => new
            {
                ID = r.ResidentId,
                Name = r.GetSortableName(),
                Age = r.GetAge(),
                Purok = r.Purok,
                TransferredTo = r.TransferDestination,
                Reason = r.TransferReason,
                Archived = r.ArchivedOn?.ToString("yyyy-MM-dd") ?? "",
                Requests = r.Requests.Count
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;

        _emptyLabel.Text = string.IsNullOrWhiteSpace(_search.Text)
            ? "No archived residents — residents you transfer will appear here"
            : $"No archived residents match \u201c{_search.Text.Trim()}\u201d";
        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    private Resident? Selected()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return Repository.Residents.FirstOrDefault(r => r.ResidentId == id);
    }

    /// <summary>Corrects an archived resident's details without restoring them.</summary>
    private void EditResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new ResidentForm(resident);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        if (!Attempt(() => Repository.UpdateResident(resident, dialog.Details))) return;

        RefreshData();
        SetStatus($"Updated the archived record for {resident.GetFullName()}.");
    }

    /// <summary>
    /// Restoring starts with the edit form, so details that changed while the
    /// resident was away (new address, contact, civil status…) are updated first.
    /// </summary>
    private void RestoreResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new ResidentForm(resident) { ConfirmOnSave = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        if (!Dialog.Confirm(
                $"Save these details and move {resident.GetFullName()} back to the active Residents list?",
                "Restore resident"))
            return;

        string name = resident.GetFullName();
        if (!Attempt(() =>
            {
                Repository.UpdateResident(resident, dialog.Details);
                Repository.RestoreResident(resident);
            }))
            return;

        RefreshData();
        SetStatus($"{name} restored to the Residents list.");
    }
}
