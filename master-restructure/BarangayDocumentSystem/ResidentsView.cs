using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Entities;
using BarangayDocumentSystem.Domain.Services;
using BarangayDocumentSystem.UI.Common;
using BarangayDocumentSystem.UI.Forms;
using BarangayDocumentSystem.UI.Theme;

namespace BarangayDocumentSystem.UI.Views;

/// <summary>
/// Resident registry: search, list, and the actions on a selected resident.
///
/// ── SINGLE RESPONSIBILITY ───────────────────────────────────────────────
/// v1's MainForm was 429 lines handling residents, requests, the dashboard,
/// AND window chrome — four reasons to change in one file. This view has one:
/// presenting residents.
/// </summary>
public class ResidentsView : ViewBase
{
    private readonly FeeSchedule _feeSchedule;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();

    /// <summary>Raised when a request is filed, so the shell can refresh other views.</summary>
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
        // --- action bar (bottom) ---
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = AppTheme.Background,
            Padding = new Padding(0, AppTheme.SpaceSm, 0, 0)
        };

        // UiFactory keeps every button identical (DRY).
        var register = UiFactory.SecondaryButton("Register Resident", 170);
        register.Click += (_, _) => RegisterResident();

        var edit = UiFactory.SecondaryButton("Edit", 110);
        edit.Click += (_, _) => EditResident();

        var delete = UiFactory.DangerButton("Delete", 110);
        delete.Click += (_, _) => DeleteResident();

        var request = UiFactory.PrimaryButton("New Document Request", 220);
        request.Click += (_, _) => FileRequest();

        actions.Controls.AddRange(new Control[] { request, register, edit, delete });

        // --- search bar (top) ---
        var searchBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = AppTheme.Surface,
            Padding = new Padding(AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceMd, AppTheme.SpaceSm)
        };

        _search.Dock = DockStyle.Fill;
        _search.Font = AppTheme.BodyFont;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.PlaceholderText = "Search by name, purok, or contact number…";
        _search.TextChanged += (_, _) => RefreshData();
        searchBar.Controls.Add(_search);

        // --- grid ---
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditResident(); };

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        gridHost.Controls.Add(_grid);

        Controls.Add(gridHost);
        Controls.Add(actions);
        Controls.Add(searchBar);
    }

    public override void RefreshData()
    {
        _grid.DataSource = Repository.SearchResidents(_search.Text.Trim())
            .Select(r => new
            {
                ID             = r.ResidentId,
                Name           = r.GetSortableName(),
                Age            = r.GetAge(),
                Gender         = r.Gender.ToString(),
                r.Purok,
                Contact        = r.ContactNumber,
                Residency      = $"{r.GetMonthsOfResidency()} mo",
                Voter          = r.IsRegisteredVoter ? "Yes" : "No",
                Classification = r.GetClassificationText(),
                Requests       = r.Requests.Count
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;
    }

    private Resident? Selected()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return Repository.Residents.FirstOrDefault(r => r.ResidentId == id);
    }

    // -----------------------------------------------------------------
    private void RegisterResident()
    {
        using var dialog = new ResidentForm();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        // One ResidentDetails object instead of fourteen positional arguments.
        var resident = Repository.AddResident(dialog.Details);

        RefreshData();
        SetStatus($"Registered {resident.GetFullName()} of {resident.Purok}.");
    }

    private void EditResident()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new ResidentForm(resident);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        // v1 repeated all fourteen assignments here by hand. Now one call.
        Repository.UpdateResident(resident, dialog.Details);

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
        Repository.RemoveResident(resident);

        RefreshData();
        SetStatus($"Deleted the record for {name}.");
    }

    private void FileRequest()
    {
        var resident = Selected();
        if (resident is null) { Dialog.SelectFirst("resident"); return; }

        using var dialog = new RequestForm(resident, _feeSchedule);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var request = Repository.CreateRequest(resident, dialog.DocumentType, dialog.Purpose);

        RefreshData();
        RequestFiled?.Invoke(this, EventArgs.Empty);

        string fee = request.Fee > 0
            ? $"Fee: ₱{request.Fee:N2}"
            : $"FREE — {request.FeeBasis}";

        SetStatus($"Filed {request.GetReferenceNumber()} — {request.GetDocumentName()}. {fee}");
    }
}
