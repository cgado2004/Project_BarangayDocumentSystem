#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;

public class ResidentsView : ViewBase
{
    private readonly FeeSchedule _feeSchedule;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly Label _emptyLabel = new();

    public event EventHandler? RequestFiled;

    public override string Title => "Residents";
    public override string Subtitle => "Registry of Barangay Magugpo Poblacion";

    public ResidentsView(IBarangayRepository repository, FeeSchedule feeSchedule)
        : base(repository)
    {
        _feeSchedule = feeSchedule ?? throw new ArgumentNullException(nameof(feeSchedule));
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
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
        bar.Margin = new Padding(0, 0, 0, 16);

        var wrap = new Panel
        {
            Dock = DockStyle.Left,
            Width = 440,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 6, 0, 6)
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

        var register = UiFactory.SecondaryButton("Register Resident", 150);
        register.Click += (_, _) => RegisterResident();

        var edit = UiFactory.SecondaryButton("Edit", 90);
        edit.Click += (_, _) => EditResident();

        var delete = UiFactory.DangerButton("Delete", 90);
        delete.Click += (_, _) => DeleteResident();

        var request = UiFactory.PrimaryButton("New Document Request", 200);
        request.Click += (_, _) => FileRequest();

        actions.Controls.Add(request);
        actions.Controls.Add(register);
        actions.Controls.Add(edit);
        actions.Controls.Add(delete);
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

    private void DeleteResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        if (!Dialog.ConfirmDestructive(
                $"Delete the record for {resident.GetFullName()}?\n\n" +
                $"{resident.Requests.Count} document request(s) will also be removed.\n\n" +
                "This cannot be undone.",
                "Confirm deletion"))
            return;

        string name = resident.GetFullName();
        if (!Attempt(() => Repository.RemoveResident(resident))) return;

        RefreshData();
        SetStatus($"Deleted the record for {name}.");
    }

    private void FileRequest()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new RequestForm(resident, _feeSchedule);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var request = AttemptGet(() =>
            Repository.CreateRequest(resident, dialog.DocumentType, dialog.Purpose));
        if (request is null) return;

        RefreshData();
        RequestFiled?.Invoke(this, EventArgs.Empty);

        string fee = request.Fee > 0
            ? $"Fee: ₱{request.Fee:N2}"
            : $"FREE — {request.FeeBasis}";

        SetStatus($"Filed {request.GetReferenceNumber()} — {request.GetDocumentName()}. {fee}");
    }
}