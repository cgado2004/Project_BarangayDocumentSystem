using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Main window for the barangay clerk. Three tabs: Residents, Document
/// Requests, Dashboard.
///
/// The form validates input and calls the model. It contains no fee
/// arithmetic and no status-transition rules — those live in
/// <see cref="FeeSchedule"/> and <see cref="DocumentRequest"/> respectively,
/// so they hold no matter what the UI does.
/// </summary>
public partial class MainForm : Form
{
    // FIELDS — each click is a separate event, so shared state must survive
    // between handlers.
    private readonly BarangayRepository _repository = new();
    private readonly DocumentPrinter _printer = new();

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        RefreshAll();
        SetStatus("Loaded sample data for Barangay Magugpo Poblacion.");
    }

    // =================================================================
    //  Refresh
    // =================================================================
    private void RefreshAll()
    {
        RefreshResidents();
        RefreshRequests();
        RefreshDashboard();
    }

    private void RefreshResidents()
    {
        var rows = _repository.SearchResidents(txtSearch.Text.Trim())
            .Select(r => new
            {
                ID             = r.ResidentId,
                Name           = r.GetSortableName(),
                Age            = r.GetAge(),
                Gender         = r.Gender.ToString(),
                Status         = r.CivilStatus.ToString(),
                r.Purok,
                Contact        = r.ContactNumber,
                Residency      = $"{r.GetMonthsOfResidency()} mo",
                Voter          = r.IsRegisteredVoter ? "Yes" : "No",
                Classification = r.GetClassificationText(),
                Requests       = r.Requests.Count
            })
            .ToList();

        dgvResidents.DataSource = rows;
    }

    private void RefreshRequests()
    {
        RequestStatus? filter = null;
        if (radPending.Checked)         filter = RequestStatus.Pending;
        else if (radProcessing.Checked) filter = RequestStatus.Processing;
        else if (radReady.Checked)      filter = RequestStatus.ReadyForRelease;
        else if (radReleased.Checked)   filter = RequestStatus.Released;

        var rows = _repository.GetRequestsByStatus(filter)
            .Select(r => new
            {
                Reference = r.GetReferenceNumber(),
                ID        = r.RequestId,
                Resident  = r.Resident.GetFullName(),
                Document  = r.GetDocumentName(),
                r.Purpose,
                Requested = r.DateRequested.ToString("yyyy-MM-dd"),
                Status    = r.Status.ToString(),
                Fee       = r.Fee > 0 ? $"₱{r.Fee:N2}" : "FREE",
                Paid      = r.Fee > 0 ? (r.IsPaid ? "Yes" : "No") : "—"
            })
            .ToList();

        dgvRequests.DataSource = rows;
    }

    private void RefreshDashboard()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("BARANGAY MAGUGPO POBLACION — CITY OF TAGUM");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        sb.AppendLine("RESIDENT REGISTRY");
        sb.AppendLine($"  Total registered residents .......... {_repository.TotalResidents,6}");
        sb.AppendLine($"  Registered voters ................... {_repository.VoterCount,6}");
        sb.AppendLine($"  Senior citizens ..................... {_repository.SeniorCitizenCount,6}");
        sb.AppendLine();
        sb.AppendLine("DOCUMENT REQUESTS");
        sb.AppendLine($"  Total requests ...................... {_repository.TotalRequests,6}");
        sb.AppendLine($"  Pending ............................. {_repository.PendingCount,6}");
        sb.AppendLine($"  Processing .......................... {_repository.ProcessingCount,6}");
        sb.AppendLine($"  Ready for release ................... {_repository.ReadyCount,6}");
        sb.AppendLine($"  Released ............................ {_repository.ReleasedCount,6}");
        sb.AppendLine();
        sb.AppendLine("REVENUE");
        sb.AppendLine($"  Total collected ..................... ₱{_repository.TotalCollected,8:N2}");
        sb.AppendLine($"  Documents issued free of charge ..... {_repository.WaivedCount,6}");
        sb.AppendLine();
        sb.AppendLine("REQUESTS BY DOCUMENT TYPE");

        foreach (var group in _repository.Requests
                     .GroupBy(r => r.GetDocumentName())
                     .OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"  {group.Key,-42} {group.Count(),4}");
        }

        sb.AppendLine();
        sb.AppendLine("RESIDENTS BY PUROK");
        foreach (var group in _repository.Residents
                     .GroupBy(r => r.Purok)
                     .OrderBy(g => g.Key))
        {
            sb.AppendLine($"  {group.Key,-42} {group.Count(),4}");
        }

        sb.AppendLine();
        sb.AppendLine(new string('=', 60));
        sb.AppendLine("FEE EXEMPTIONS IN FORCE");
        sb.AppendLine("  RA 11261  First-time jobseeker (6 mo residency, once only)");
        sb.AppendLine("  RA 9994   Senior citizens");
        sb.AppendLine("  RA 10754  Persons with disability");
        sb.AppendLine("  DILG MC 2019-177  Certificates of indigency");
        sb.AppendLine();
        sb.AppendLine("  NOTE: fee amounts are placeholders. Replace with the");
        sb.AppendLine("  actual Magugpo Poblacion revenue ordinance rates.");

        txtStats.Text = sb.ToString();
    }

    // =================================================================
    //  Selection helpers
    // =================================================================
    private Resident? GetSelectedResident()
    {
        if (dgvResidents.CurrentRow is null) return null;
        int id = (int)dgvResidents.CurrentRow.Cells["ID"].Value;
        return _repository.Residents.FirstOrDefault(r => r.ResidentId == id);
    }

    private DocumentRequest? GetSelectedRequest()
    {
        if (dgvRequests.CurrentRow is null) return null;
        int id = (int)dgvRequests.CurrentRow.Cells["ID"].Value;
        return _repository.Requests.FirstOrDefault(r => r.RequestId == id);
    }

    // =================================================================
    //  Residents tab
    // =================================================================
    private void btnAddResident_Click(object sender, EventArgs e)
    {
        using var dialog = new ResidentForm();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var resident = _repository.AddResident(
            dialog.FirstName, dialog.MiddleName, dialog.LastName, dialog.Suffix,
            dialog.DateOfBirth, dialog.Gender, dialog.CivilStatus,
            dialog.Purok, dialog.AddressLine, dialog.ContactNumber,
            dialog.Occupation, dialog.DateOfResidency, dialog.IsVoter,
            dialog.Classification);

        RefreshAll();
        SetStatus($"Registered {resident.GetFullName()} of {resident.Purok}.");
    }

    private void btnEditResident_Click(object sender, EventArgs e)
    {
        var resident = GetSelectedResident();
        if (resident is null)
        {
            ShowInfo("Please select a resident first.");
            return;
        }

        using var dialog = new ResidentForm(resident);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        resident.FirstName       = dialog.FirstName;
        resident.MiddleName      = dialog.MiddleName;
        resident.LastName        = dialog.LastName;
        resident.Suffix          = dialog.Suffix;
        resident.DateOfBirth     = dialog.DateOfBirth;
        resident.Gender          = dialog.Gender;
        resident.CivilStatus     = dialog.CivilStatus;
        resident.Purok           = dialog.Purok;
        resident.AddressLine     = dialog.AddressLine;
        resident.ContactNumber   = dialog.ContactNumber;
        resident.Occupation      = dialog.Occupation;
        resident.DateOfResidency = dialog.DateOfResidency;
        resident.IsRegisteredVoter = dialog.IsVoter;
        resident.Classification  = dialog.Classification;

        RefreshAll();
        SetStatus($"Updated the record for {resident.GetFullName()}.");
    }

    private void btnDeleteResident_Click(object sender, EventArgs e)
    {
        var resident = GetSelectedResident();
        if (resident is null)
        {
            ShowInfo("Please select a resident first.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Delete the record for {resident.GetFullName()}?\n\n" +
            $"{resident.Requests.Count} document request(s) will also be removed.\n\n" +
            $"This cannot be undone.",
            "Confirm deletion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        string name = resident.GetFullName();
        _repository.RemoveResident(resident);

        RefreshAll();
        SetStatus($"Deleted the record for {name}.");
    }

    private void dgvResidents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
            btnEditResident_Click(sender, e);
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => RefreshResidents();

    /// <summary>
    /// Files a new document request. Eligibility for the RA 11261 certificate
    /// is checked BEFORE the request is created, so an ineligible resident is
    /// never given a request that would have to be rejected later.
    /// </summary>
    private void btnNewRequest_Click(object sender, EventArgs e)
    {
        var resident = GetSelectedResident();
        if (resident is null)
        {
            ShowInfo("Please select a resident first.");
            return;
        }

        using var dialog = new RequestForm(resident, _repository.FeeSchedule);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var request = _repository.CreateRequest(
            resident, dialog.DocumentType, dialog.Purpose);

        RefreshAll();
        tabMain.SelectedTab = tabRequests;

        string feeText = request.Fee > 0
            ? $"Fee: ₱{request.Fee:N2}"
            : $"FREE — {request.FeeBasis}";

        SetStatus($"Filed {request.GetReferenceNumber()} — {request.GetDocumentName()}. {feeText}");
    }

    // =================================================================
    //  Requests tab
    // =================================================================
    private void btnProcess_Click(object sender, EventArgs e) =>
        ChangeStatus(r => r.StartProcessing(), "moved to processing");

    private void btnReady_Click(object sender, EventArgs e) =>
        ChangeStatus(r => r.MarkReadyForRelease(), "marked ready for release");

    private void btnRelease_Click(object sender, EventArgs e) =>
        ChangeStatus(r => r.Release(), "released");

    /// <summary>
    /// Shared handler for the status transitions. The model decides whether a
    /// transition is legal and throws if it isn't; we surface the message
    /// rather than letting the app crash.
    /// </summary>
    private void ChangeStatus(Action<DocumentRequest> action, string verb)
    {
        var request = GetSelectedRequest();
        if (request is null)
        {
            ShowInfo("Please select a request first.");
            return;
        }

        try
        {
            action(request);
            RefreshAll();
            SetStatus($"{request.GetReferenceNumber()} {verb}.");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cannot perform this action",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnPay_Click(object sender, EventArgs e)
    {
        var request = GetSelectedRequest();
        if (request is null)
        {
            ShowInfo("Please select a request first.");
            return;
        }

        if (request.Fee <= 0)
        {
            MessageBox.Show(
                $"This document carries no fee.\n\nBasis: {request.FeeBasis}",
                "No payment due", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (request.IsPaid)
        {
            MessageBox.Show(
                $"Already paid.\n\nO.R. Number: {request.OfficialReceiptNo}",
                "Already paid", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new PaymentForm(request);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            request.RecordPayment(dialog.OfficialReceiptNo);
            RefreshAll();
            SetStatus($"Recorded ₱{request.Fee:N2} for {request.GetReferenceNumber()} " +
                      $"(O.R. {request.OfficialReceiptNo}).");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(ex.Message, "Cannot record payment",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnReject_Click(object sender, EventArgs e)
    {
        var request = GetSelectedRequest();
        if (request is null)
        {
            ShowInfo("Please select a request first.");
            return;
        }

        string reason = Prompt.Show(this,
            "Reason for rejection",
            $"Why is {request.GetReferenceNumber()} being rejected?");

        if (string.IsNullOrWhiteSpace(reason))
            return;   // cancelled

        try
        {
            request.Reject(reason);
            RefreshAll();
            SetStatus($"{request.GetReferenceNumber()} rejected: {reason}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(ex.Message, "Cannot reject",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        var request = GetSelectedRequest();
        if (request is null)
        {
            ShowInfo("Please select a request first.");
            return;
        }

        string text = _printer.Print(request);

        using var dialog = new DocumentPreviewForm(request.GetDocumentName(), text);
        dialog.ShowDialog(this);
    }

    private void StatusFilter_CheckedChanged(object sender, EventArgs e)
    {
        // CheckedChanged fires twice per change — once for the radio turning
        // off, once for the one turning on. Only act on the switch-ON.
        if (sender is RadioButton { Checked: true })
            RefreshRequests();
    }

    // =================================================================
    //  Utilities
    // =================================================================
    private static void ShowInfo(string message) =>
        MessageBox.Show(message, "No selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void SetStatus(string message) =>
        lblStatus.Text = $"{DateTime.Now:HH:mm:ss}  —  {message}";
}
