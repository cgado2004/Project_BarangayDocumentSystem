using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

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
///
/// v3.1 adds the RA 11032 aging: the queue counts the working days each open
/// request has been waiting and flags anything past the three-working-day
/// standard the Ease of Doing Business Act prescribes for a simple frontline
/// transaction. A barangay document is one.
/// </summary>
public class RequestsView : ViewBase
{
    private readonly IBarangayRepository _repo;
    private readonly FeeSchedule _fees;

    private readonly DataGridView _grid = new();
    private readonly FlowLayoutPanel _filters = new();
    private readonly Label _agingNote = new();
    private RequestStatus? _filter;

    private readonly PillButton _btnProcess = new() { Text = "Start processing", Look = PillButton.Style.Outline };
    private readonly PillButton _btnReady   = new() { Text = "Mark ready",       Look = PillButton.Style.Outline };
    private readonly PillButton _btnPay     = new() { Text = "Record payment" };
    private readonly PillButton _btnRelease = new() { Text = "Release",          Accent = Success };
    private readonly PillButton _btnReject  = new() { Text = "Reject",           Look = PillButton.Style.Outline, Accent = Danger };
    private readonly PillButton _btnPreview = new() { Text = "Preview document", Look = PillButton.Style.Quiet };

    public RequestsView(IBarangayRepository repo, FeeSchedule fees)
    {
        _repo = repo;
        _fees = fees;

        var body = new SmoothPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

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
        _filters.Height = 44;
        _filters.WrapContents = false;
        _filters.BackColor = Color.Transparent;
        _filters.Padding = new Padding(0, 6, 0, 0);

        _filters.Controls.Add(MakeChip("All", null));
        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
            _filters.Controls.Add(MakeChip(status.ToString(), status));

        // ---- the RA 11032 note under the header ----
        _agingNote.Dock = DockStyle.Top;
        _agingNote.Height = 30;
        _agingNote.Font = Small;
        _agingNote.ForeColor = Muted;
        _agingNote.BackColor = Color.Transparent;
        _agingNote.Text =
            $"  RA 11032 standard: {_fees.RA11032SimpleWorkingDays} working days for a simple transaction. " +
            "Aged requests are highlighted.";

        body.Controls.Add(gridCard);
        body.Controls.Add(actions);
        body.Controls.Add(_filters);
        body.Controls.Add(_agingNote);

        Controls.Add(body);
        Controls.Add(PageHeader("Document requests",
                                "Track each request from filing to release"));
    }

    private Chip MakeChip(string text, RequestStatus? status)
    {
        var chip = new Chip
        {
            Text = text,
            Width = 86,
            Margin = new Padding(0, 0, 8, 6),
            Accent = Primary
        };
        chip.Click += (_, _) =>
        {
            _filter = status;
            foreach (Control c in _filters.Controls)
                if (c is Chip other) other.Selected = ReferenceEquals(other, chip);
            LoadGrid();
        };
        return chip;
    }

    public override void OnShown()
    {
        if (_filters.Controls.Count > 0 && _filters.Controls[0] is Chip first)
        {
            first.Selected = true;
            _filter = null;
        }
        LoadGrid();
    }

    /// <summary>I apply a status filter that arrived from the dashboard.</summary>
    public void ApplyFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            _filter = null;
            SelectChip(null);
        }
        else if (Enum.TryParse<RequestStatus>(filter, out var status))
        {
            _filter = status;
            SelectChip(status);
        }
        LoadGrid();
    }

    private void SelectChip(RequestStatus? status)
    {
        int index = status is null ? 0 : Array.IndexOf(Enum.GetValues<RequestStatus>(), status) + 1;
        foreach (Control c in _filters.Controls)
            if (c is Chip chip) chip.Selected = ReferenceEquals(c, _filters.Controls[index]);
    }

    private void LoadGrid()
    {
        var rows = _repo.GetRequestsByStatus(_filter)
            .OrderByDescending(r => r.DateRequested)
            .ToList();

        _grid.DataSource = rows.Select(r => new
        {
            Reference = r.GetReferenceNumber(),
            Document = FeeSchedule.NameOf(r.DocumentType),
            Resident = r.Resident.GetSortableName(),
            Filed = DisplayFormat.GridDate(r.DateRequested),
            r.Status,
            Fee = DisplayFormat.PesoOrFree(r.Fee),
            Paid = r.IsPaid ? "Yes" : (r.Fee == 0 ? "—" : "No"),
            Queue = DescribeQueue(r)
        }).ToList();

        if (_grid.Columns.Contains("Reference"))
        {
            _grid.Columns["Reference"]!.FillWeight = 95;
            _grid.Columns["Document"]!.FillWeight = 120;
            _grid.Columns["Resident"]!.FillWeight = 110;
            _grid.Columns["Filed"]!.FillWeight = 60;
            _grid.Columns["Queue"]!.FillWeight = 60;
        }

        HighlightAgedRequests(rows);
        UpdateButtons();
    }

    /// <summary>
    /// The queue column: "2 wd" for an open request, "—" once it is out of
    /// the queue. "wd" reads as working days without needing a wider column.
    /// </summary>
    private string DescribeQueue(DocumentRequest r)
    {
        if (r.Status is RequestStatus.Released or RequestStatus.Rejected) return "—";
        int days = r.WorkingDaysInQueue();
        return days == 1 ? "1 wd" : $"{days} wd";
    }

    /// <summary>
    /// I paint the aged rows. RA 11032 gives a simple frontline transaction
    /// three working days; anything still open past that is shown in the
    /// danger colour so it cannot be forgotten, the way a deadline should
    /// not be forgettable.
    /// </summary>
    private void HighlightAgedRequests(List<DocumentRequest> rows)
    {
        _grid.ClearSelection();
        for (int i = 0; i < rows.Count && i < _grid.Rows.Count; i++)
        {
            if (!rows[i].IsBeyondRA11032Standard(_fees.RA11032SimpleWorkingDays)) continue;

            _grid.Rows[i].DefaultCellStyle.ForeColor = Danger;
            _grid.Rows[i].DefaultCellStyle.SelectionForeColor = Danger;
            _grid.Rows[i].Cells["Queue"].ToolTipText =
                "Past the RA 11032 standard of " +
                $"{_fees.RA11032SimpleWorkingDays} working days for a simple transaction.";
        }
    }

    private DocumentRequest? Selected()
    {
        if (_grid.CurrentRow is null) return null;
        var reference = _grid.CurrentRow.Cells["Reference"].Value?.ToString();
        if (string.IsNullOrEmpty(reference)) return null;
        return _repo.Requests.FirstOrDefault(r => r.GetReferenceNumber() == reference);
    }

    private void UpdateButtons()
    {
        var r = Selected();
        bool any = r is not null;

        _btnProcess.Enabled = any && r!.Status == RequestStatus.Pending;
        _btnReady.Enabled   = any && r!.Status == RequestStatus.Processing;
        _btnPay.Enabled     = any && r!.Status is RequestStatus.Processing or RequestStatus.ReadyForRelease
                                   && r!.Fee > 0 && !r!.IsPaid;
        _btnRelease.Enabled = any && r!.Status == RequestStatus.ReadyForRelease;
        _btnReject.Enabled  = any && r!.Status != RequestStatus.Released;
        _btnPreview.Enabled = any;
    }

    /// <summary>
    /// The shared wrapper for every status move. The state machine may throw
    /// if the move is illegal - and when it does, I show the reason rather
    /// than let the program die, because the reason is exactly what the
    /// clerk needs to read. v3.1.1: the store is asked inside the same
    /// guard, because the store can refuse a move too (a receipt number
    /// already recorded on another request is a store decision, not a
    /// status-machine one).
    /// </summary>
    private void Step(Action<DocumentRequest> move)
    {
        var r = Selected();
        if (r is null) return;

        try
        {
            move(r);
            _repo.SaveRequest(r);
        }
        catch (InvalidOperationException ex)
        {
            Dialog.Warn(this, ex.Message, "Not allowed");
            return;
        }

        LoadGrid();
    }

    private void RecordPayment()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new PaymentForm(r);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            // v3.1.1 FIX: before this version, the OK click never actually
            // recorded anything - the request was saved without
            // RecordPayment having been applied, so the queue quietly kept
            // saying "unpaid" no matter what the clerk typed. The receipt
            // number is now written onto the request, after the store is
            // asked whether the number is already on file.
            if (_repo.ReceiptNumberExists(form.ReceiptNumber, r))
            {
                Dialog.Warn(this,
                    $"Official receipt number {form.ReceiptNumber} is already " +
                    "recorded on another request. A receipt number identifies " +
                    "exactly one payment.",
                    "Not allowed");
                return;
            }

            r.RecordPayment(form.ReceiptNumber);
            _repo.SaveRequest(r);
        }
        catch (InvalidOperationException ex)
        {
            Dialog.Warn(this, ex.Message, "Not allowed");
            return;
        }

        LoadGrid();
    }

    private void Reject()
    {
        var r = Selected();
        if (r is null) return;

        // v3.1.1: the dedicated rejection dialog, ported from Jonathan Del
        // Rosario's Draft branch. It tells the clerk up front that a paid
        // request keeps its payment in the collection history - this app
        // does not issue refunds - and validates the reason the record
        // requires anyway.
        using var form = new RejectionForm(r);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        Step(x => x.Reject(form.Reason));
    }

    private void PreviewSelected()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new DocumentPreviewForm(r);
        form.ShowDialog(this);
    }
}
