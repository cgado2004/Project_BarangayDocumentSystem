#nullable enable
using System;
using System.Collections.Generic;
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
                Dependents = r.Dependents.Count,
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

/// <summary>
/// The activity log: everything done in the system, shown one day at a time.
/// Pick a day (or step with Prev / Today / Next), then optionally narrow it
/// to one kind of action or search the text.
/// </summary>
public class ActivityLogView : ViewBase
{
    private const string AllActions = "All activities";

    private readonly DataGridView _grid = new();
    private readonly DateTimePicker _day = new();
    private readonly ComboBox _actionFilter = new();
    private readonly TextBox _search = new();
    private readonly Label _summary = new();
    private readonly Label _emptyLabel = new();
    private bool _loading;

    public override string Title => "Activity Log";
    public override string Subtitle => "Everything done in the system, one day at a time";

    public ActivityLogView(IBarangayRepository repository) : base(repository)
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
        root.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = UiFactory.Toolbar();
        bar.Margin = new Padding(0, 0, 0, 12);

        // The bar is 72 px tall with 10 px padding, so 52 px are left for controls.
        int buttonTop = Math.Max(0, (52 - AppTheme.ControlHeight) / 2);
        const int fieldTop = 13;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var dayLabel = new Label
        {
            Text = "Day",
            AutoSize = false,
            Width = 40,
            Height = 52,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.BodyBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0)
        };

        _day.Font = AppTheme.BodyFont;
        _day.Format = DateTimePickerFormat.Custom;
        _day.CustomFormat = "ddd, MMM d, yyyy";
        _day.Width = 180;
        _day.Value = DateTime.Today;
        _day.Margin = new Padding(0, fieldTop, 12, 0);
        _day.ValueChanged += (_, _) => { if (!_loading) RefreshData(); };

        var prev = UiFactory.SecondaryButton("\u2039 Prev", 80);
        prev.Margin = new Padding(0, buttonTop, 8, 0);
        prev.Click += (_, _) => _day.Value = _day.Value.Date.AddDays(-1);

        var today = UiFactory.SecondaryButton("Today", 80);
        today.Margin = new Padding(0, buttonTop, 8, 0);
        today.Click += (_, _) => _day.Value = DateTime.Today;

        var next = UiFactory.SecondaryButton("Next \u203a", 80);
        next.Margin = new Padding(0, buttonTop, 20, 0);
        next.Click += (_, _) => _day.Value = _day.Value.Date.AddDays(1);

        // Same height as the date box so everything on the bar lines up.
        prev.Height = today.Height = next.Height = _day.Height;

        _actionFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _actionFilter.Font = AppTheme.BodyFont;
        _actionFilter.Width = 180;
        _actionFilter.Margin = new Padding(0, fieldTop, 12, 0);
        _actionFilter.Items.Add(AllActions);
        _actionFilter.SelectedIndex = 0;
        _actionFilter.SelectedIndexChanged += (_, _) => { if (!_loading) RefreshData(); };

        _search.Font = AppTheme.BodyFont;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.BackColor = AppTheme.Background;
        _search.Width = 240;
        _search.Margin = new Padding(0, fieldTop, 0, 0);
        CueBanner.Set(_search, "Search user, action or details\u2026");
        _search.TextChanged += (_, _) => { if (!_loading) RefreshData(); };

        flow.Controls.Add(dayLabel);
        flow.Controls.Add(_day);
        flow.Controls.Add(prev);
        flow.Controls.Add(today);
        flow.Controls.Add(next);
        flow.Controls.Add(_actionFilter);
        flow.Controls.Add(_search);

        bar.Controls.Add(flow);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;

        var host = UiFactory.CardHost(_grid);
        host.Margin = new Padding(0, 0, 0, 8);

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

    private Control BuildFooter()
    {
        _summary.Dock = DockStyle.Fill;
        _summary.TextAlign = ContentAlignment.MiddleLeft;
        _summary.Font = AppTheme.BodyBoldFont;
        _summary.ForeColor = AppTheme.TextSecondary;
        _summary.BackColor = AppTheme.Background;
        return _summary;
    }

    public override void RefreshData()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            DateTime day = _day.Value.Date;
            IReadOnlyList<ActivityEntry> entries =
                AttemptGet(() => Repository.GetActivity(day)) ?? new List<ActivityEntry>();

            // Offer only the kinds of action that happened on this day.
            string chosen = _actionFilter.SelectedItem as string ?? AllActions;
            var kinds = entries.Select(e => e.Action).Distinct().OrderBy(a => a).ToList();
            _actionFilter.Items.Clear();
            _actionFilter.Items.Add(AllActions);
            foreach (string kind in kinds) _actionFilter.Items.Add(kind);
            _actionFilter.SelectedItem = kinds.Contains(chosen) ? chosen : AllActions;
            chosen = (string)_actionFilter.SelectedItem!;

            string term = _search.Text.Trim();
            var shown = entries
                .Where(e => chosen == AllActions || e.Action == chosen)
                .Where(e => term.Length == 0
                    || e.Username.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || e.Action.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || e.Details.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            _grid.DataSource = shown
                .Select(e => new
                {
                    Time = e.LoggedOn.ToString("h:mm:ss tt"),
                    User = e.Username,
                    Activity = e.Action,
                    Details = e.Details
                })
                .ToList();

            if (_grid.Columns["Time"] is { } time) time.FillWeight = 14;
            if (_grid.Columns["User"] is { } user) user.FillWeight = 18;
            if (_grid.Columns["Activity"] is { } activity) activity.FillWeight = 18;
            if (_grid.Columns["Details"] is { } details) details.FillWeight = 50;

            string dayText = day.ToString("dddd, MMMM d, yyyy");
            _summary.Text = shown.Count == entries.Count
                ? $"  {entries.Count} activit{(entries.Count == 1 ? "y" : "ies")} on {dayText}"
                : $"  Showing {shown.Count} of {entries.Count} activities on {dayText}";

            _emptyLabel.Text = entries.Count == 0
                ? $"Nothing was logged on {dayText}"
                : "No activities match the filter";
            _emptyLabel.Visible = shown.Count == 0;
        }
        finally
        {
            _loading = false;
        }
    }
}
