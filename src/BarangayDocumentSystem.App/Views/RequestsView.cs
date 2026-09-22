using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.App.Controls;
using BarangayDocumentSystem.App.Dialogs;
using BarangayDocumentSystem.App.Theme;
using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.Core.Entities;
using BarangayDocumentSystem.Core.Rules;

namespace BarangayDocumentSystem.App.Views;

/// <summary>
/// The request queue, and the buttons that move a request along.
///
/// I enable and disable the action buttons based on the status of whichever
/// row is selected, so the clerk is only ever offered the step that is legally
/// next.
///
/// I want to be clear that the rules still live in DocumentRequest. This
/// screen only mirrors them so the buttons look right - it does not
/// re-implement them. If I had copied the rules here they would eventually
/// disagree with the real ones.
/// </summary>
public class RequestsView : ViewBase
{
    private readonly IBarangayRepository _repo;

    private readonly DataGridView _grid = new();
    private readonly FlowLayoutPanel _filters = new();
    private RequestStatus? _filter;

    private readonly PillButton _btnProcess = new() { Text = "Start processing", Look = PillButton.Style.Outline };
    private readonly PillButton _btnReady   = new() { Text = "Mark ready",       Look = PillButton.Style.Outline };
    private readonly PillButton _btnPay     = new() { Text = "Record payment" };
    private readonly PillButton _btnRelease = new() { Text = "Release",          Accent = AppTheme.Success };
    private readonly PillButton _btnReject  = new() { Text = "Reject",           Look = PillButton.Style.Outline, Accent = AppTheme.Danger };
    private readonly PillButton _btnPreview = new() { Text = "Preview document", Look = PillButton.Style.Quiet };

    public RequestsView(IBarangayRepository repo)
    {
        _repo = repo;

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var gridCard = new Card { Dock = DockStyle.Fill };
        ResidentsView.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) PreviewSelected(); };
        gridCard.Controls.Add(_grid);

        // ---- action bar ----
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.Transparent,
            WrapContents = true,          // wrap rather than clip on a narrow window
            Padding = new Padding(0, 10, 0, 0)
        };
        foreach (var b in new[] { _btnProcess, _btnReady, _btnPay, _btnRelease, _btnReject, _btnPreview })
        {
            b.Width = 148;
            b.Margin = new Padding(0, 0, 8, 8);
            actions.Controls.Add(b);
        }

        _btnProcess.Click += (_, _) => Step(r => r.StartProcessing());
        _btnReady.Click   += (_, _) => Step(r => r.MarkReadyForRelease());
        _btnRelease.Click += (_, _) => Step(r => r.Release());
        _btnPay.Click     += (_, _) => RecordPayment();
        _btnReject.Click  += (_, _) => Reject();
        _btnPreview.Click += (_, _) => PreviewSelected();

        // ---- status filter chips ----
        _filters.Dock = DockStyle.Top;
        _filters.Height = 48;
        _filters.BackColor = Color.Transparent;
        _filters.WrapContents = true;
        BuildFilters();

        body.Controls.Add(gridCard);
        body.Controls.Add(actions);
        body.Controls.Add(_filters);

        Controls.Add(body);
        Controls.Add(PageHeader("Document requests", "Track each request from filing to release"));
    }

    private void BuildFilters()
    {
        _filters.Controls.Clear();

        void Add(string label, RequestStatus? status)
        {
            var chip = new Chip
            {
                Text = $"  {label}  ",
                Selected = _filter == status,
                Width = TextRenderer.MeasureText($"  {label}  ", AppTheme.SmallBold).Width + 28,
                Margin = new Padding(0, 6, 8, 0)
            };
            chip.Click += (_, _) => { _filter = status; BuildFilters(); LoadGrid(); };
            _filters.Controls.Add(chip);
        }

        Add("All", null);
        Add("Pending", RequestStatus.Pending);
        Add("Processing", RequestStatus.Processing);
        Add("Ready for release", RequestStatus.ReadyForRelease);
        Add("Released", RequestStatus.Released);
        Add("Rejected", RequestStatus.Rejected);
    }

    public override void OnShown() => LoadGrid();

    public void ApplyFilter(string? filter)
    {
        _filter = Enum.TryParse<RequestStatus>(filter, out var s) ? s : null;
        BuildFilters();
        LoadGrid();
    }

    private void LoadGrid()
    {
        var rows = _repo.GetRequestsByStatus(_filter).ToList();

        _grid.DataSource = rows.Select(r => new
        {
            r.RequestId,
            Reference = r.GetReferenceNumber(),
            Resident = r.Resident.GetFullName(),
            Document = FeeSchedule.NameOf(r.DocumentType),
            Status = r.Status.ToString(),
            Fee = DisplayFormat.Peso(r.Fee),
            Paid = r.IsPaid ? "Yes" : (r.Fee == 0 ? "—" : "No"),
            Filed = DisplayFormat.GridDate(r.DateRequested)
        }).ToList();

        if (_grid.Columns.Contains("RequestId"))
        {
            _grid.Columns["RequestId"]!.Visible = false;   // needed to find the row, not useful on screen
            _grid.Columns["Reference"]!.FillWeight = 90;
            _grid.Columns["Resident"]!.FillWeight = 130;
            _grid.Columns["Document"]!.FillWeight = 160;
            _grid.Columns["Fee"]!.FillWeight = 60;
            _grid.Columns["Paid"]!.FillWeight = 50;
        }

        UpdateButtons();
    }

    private DocumentRequest? Selected()
    {
        if (_grid.CurrentRow is null) return null;
        var cell = _grid.CurrentRow.Cells["RequestId"].Value;
        if (cell is null) return null;
        int id = Convert.ToInt32(cell);
        return _repo.Requests.FirstOrDefault(r => r.RequestId == id);
    }

    /// <summary>
    /// I offer only the step that is legal for the selected row.
    ///
    /// This mirrors the guards inside DocumentRequest rather than duplicating
    /// them. The entity would refuse an illegal move anyway; greying the
    /// button out just stops the clerk trying and seeing an error.
    /// </summary>
    private void UpdateButtons()
    {
        var r = Selected();

        bool has = r is not null;
        _btnPreview.Enabled = has;

        _btnProcess.Enabled = has && r!.Status == RequestStatus.Pending;
        _btnReady.Enabled   = has && r!.Status == RequestStatus.Processing;
        _btnPay.Enabled     = has && r!.Fee > 0 && !r.IsPaid && r.Status != RequestStatus.Released
                                  && r.Status != RequestStatus.Rejected;
        _btnRelease.Enabled = has && r!.Status == RequestStatus.ReadyForRelease
                                  && (r.Fee == 0 || r.IsPaid);
        _btnReject.Enabled  = has && r!.Status != RequestStatus.Released
                                  && r.Status != RequestStatus.Rejected;
    }

    /// <summary>
    /// I run a status change and turn any refusal into a readable message,
    /// instead of letting the exception reach the user as a crash. That is
    /// NFR-02 in my documentation.
    /// </summary>
    private void Step(Action<DocumentRequest> action)
    {
        var r = Selected();
        if (r is null) return;

        try
        {
            action(r);
            _repo.SaveRequest(r);
            LoadGrid();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Not allowed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RecordPayment()
    {
        var r = Selected();
        if (r is null) return;

        using var dlg = new PaymentDialog(r);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            r.RecordPayment(dlg.ReceiptNumber);
            _repo.SaveRequest(r);
            LoadGrid();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Not allowed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Reject()
    {
        var r = Selected();
        if (r is null) return;

        using var dlg = new PromptDialog("Reject request",
            "Why is this request being rejected? The resident is entitled to know.");
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            r.Reject(dlg.Value);
            _repo.SaveRequest(r);
            LoadGrid();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Not allowed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PreviewSelected()
    {
        var r = Selected();
        if (r is null) return;

        using var dlg = new DocumentPreviewDialog(r);
        dlg.ShowDialog(this);
    }
}
