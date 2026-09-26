// =====================================================================
//  PART:    Views - the request queue and the buttons that move a request along
//  ORIGIN:  Draft - Jonathan F. Del Rosario (the screen: status filters over a grid, actions below)
//           Fdraft - Frent Dhieniel Raborar (this file's place in the tree)
//           the code and comments are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - v3.2: writes go through ViewBase.Persist; RecordPayment now
//           records the payment on the request (the bug PR #3 flagged); UiFactory.StyleGrid
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
public sealed class RequestsView : ViewBase
{
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

    public RequestsView(IBarangayRepository repository, FeeSchedule fees) : base(repository)
    {
        _fees = fees ?? throw new ArgumentNullException(nameof(fees));

        var body = new SmoothPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var gridCard = new Card { Dock = DockStyle.Fill };
        UiFactory.StyleGrid(_grid);
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
        foreach (RequestStatus status in ((RequestStatus[])Enum.GetValues(typeof(RequestStatus))))
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
        int index = status is null ? 0 : Array.IndexOf(((RequestStatus[])Enum.GetValues(typeof(RequestStatus))), status) + 1;
        foreach (Control c in _filters.Controls)
            if (c is Chip chip) chip.Selected = ReferenceEquals(c, _filters.Controls[index]);
    }

    private void LoadGrid()
    {
        var rows = Repository.GetRequestsByStatus(_filter)
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
        return Repository.Requests.FirstOrDefault(r => r.GetReferenceNumber() == reference);
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
    /// The shared wrapper for every change to a request: apply the move,
    /// then save it.
    ///
    /// Two different things can go wrong, and I treat them differently. The
    /// state machine may throw because the move is illegal - I show the
    /// reason as a warning, because the reason is exactly what the clerk
    /// needs to read, and nothing has changed. Or the database may refuse
    /// the save - Persist shows that and reloads, so the grid goes back to
    /// what is really stored rather than showing a move that did not stick.
    /// </summary>
    private void Step(Action<DocumentRequest> move)
    {
        var r = Selected();
        if (r is null) return;

        try
        {
            move(r);
        }
        catch (InvalidOperationException ex)
        {
            Dialog.Warn(this, ex.Message, "Not allowed");
            return;
        }

        Persist(() => Repository.SaveRequest(r), "The change to " + r.GetReferenceNumber());
        LoadGrid();
    }

    /// <summary>
    /// Take the official receipt number from the payment dialog, then record
    /// the payment through the request's own rule (which refuses a second
    /// payment or a payment on a free document) and save it.
    ///
    /// Earlier versions saved the request without ever calling
    /// RecordPayment, so the receipt was printed and the row still said
    /// unpaid. The dialog only collects and validates the number; the
    /// request is the only thing that may mark itself paid.
    /// </summary>
    private void RecordPayment()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new PaymentForm(r);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        string receipt = form.ReceiptNumber;
        Step(x => x.RecordPayment(receipt));
    }

    private void Reject()
    {
        var r = Selected();
        if (r is null) return;

        string? reason = Prompt.Text(this, "Reject request",
            $"Why is {r.GetReferenceNumber()} being rejected?\n" +
            "The resident is entitled to be told the reason.");
        if (reason is null) return;

        Step(x => x.Reject(reason!));
    }

    private void PreviewSelected()
    {
        var r = Selected();
        if (r is null) return;

        using var form = new DocumentPreviewForm(r);
        form.ShowDialog(this);
    }
}
