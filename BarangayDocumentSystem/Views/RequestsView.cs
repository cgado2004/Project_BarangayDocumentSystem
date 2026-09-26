using System.Windows.Forms;
using System.Drawing;
using System;
using System.Linq;
using BarangayDocumentSystem.BusinessRules;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.UIHelpers;

namespace BarangayDocumentSystem.Views;


/// Document requests: filter, list, and move a request through its workflow.

public class RequestsView : ViewBase
{
    private readonly DocumentRenderer _renderer;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _statusFilter = new();

    public override string Title => "Document Requests";
    public override string Subtitle => "Track requests from filing through release";

    public RequestsView(IBarangayRepository repository, DocumentRenderer renderer)
        : base(repository)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        BuildLayout();
    }

    private void BuildLayout()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = AppTheme.Background,
            Padding = new Padding(0, AppTheme.SpaceSm, 0, 0)
        };

        // ── DRY ──────────────────────────────────────────────────────────
        // The three status-transition buttons differ ONLY by the method they
        // call and the verb they report. Building them from a table makes
        // that explicit and removes three near-identical handlers.
        var transitions = new (string Text, Action<DocumentRequest> Action, string Verb)[]
        {
            ("Start Processing", r => r.StartProcessing(),     "moved to processing"),
            ("Mark Ready",       r => r.MarkReadyForRelease(), "marked ready for release"),
            ("Release",          r => r.Release(),             "released")
        };

        foreach (var (text, action, verb) in transitions)
        {
            // "Release" is the terminal, most significant action.
            var button = text == "Release"
                ? UiFactory.PrimaryButton(text, 140)
                : UiFactory.SecondaryButton(text, 150);

            button.Click += (_, _) => ChangeStatus(action, verb);
            actions.Controls.Add(button);
        }

        var pay = UiFactory.SecondaryButton("Record Payment", 160);
        pay.Click += (_, _) => RecordPayment();

        var reject = UiFactory.DangerButton("Reject", 110);
        reject.Click += (_, _) => RejectRequest();

        var print = UiFactory.SecondaryButton("View / Print", 140);
        print.Click += (_, _) => PrintDocument();

        actions.Controls.AddRange(new Control[] { pay, reject, print });

        // --- filter bar ---
        var filterBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = AppTheme.Surface,
            Padding = new Padding(AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceMd, AppTheme.SpaceSm)
        };

        // A ComboBox replaces v1's five RadioButtons: less space, and no need
        // for the CheckedChanged double-fire guard those required.
        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Font = AppTheme.BodyFont;
        _statusFilter.Width = 240;
        _statusFilter.Dock = DockStyle.Left;
        _statusFilter.Items.Add("All statuses");
        foreach (var name in Enum.GetNames(typeof(RequestStatus)))
            _statusFilter.Items.Add(name);
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => RefreshData();

        var filterLabel = new Label
        {
            Text = "Show:  ",
            Dock = DockStyle.Left,
            Width = 60,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextSecondary
        };

        filterBar.Controls.Add(_statusFilter);
        filterBar.Controls.Add(filterLabel);

        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;

        // Colour the Status cell by value — the visual cue v1 lacked.
        _grid.CellFormatting += Grid_CellFormatting;

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        gridHost.Controls.Add(_grid);

        Controls.Add(gridHost);
        Controls.Add(actions);
        Controls.Add(filterBar);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].Name != "Status") return;
        if (e.Value is not string status) return;

        // AppTheme owns the mapping, so every screen colours statuses alike.
        e.CellStyle!.ForeColor = AppTheme.StatusColor(status);
        e.CellStyle.Font = AppTheme.BodyBoldFont;
    }

    public override void RefreshData()
    {
        RequestStatus? filter = _statusFilter.SelectedIndex <= 0
            ? null
            : (RequestStatus)Enum.Parse(typeof(RequestStatus), _statusFilter.Text);

        _grid.DataSource = Repository.GetRequestsByStatus(filter)
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

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;
    }

    private DocumentRequest? Selected()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return Repository.Requests.FirstOrDefault(r => r.RequestId == id);
    }

   
    /// One handler for all three transitions. The domain decides whether a
    /// move is legal and throws if not; we report rather than crash.
  
    private void ChangeStatus(Action<DocumentRequest> action, string verb)
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        try
        {
            action(request);                                   // rule check + change in memory
            if (!Attempt(() => Repository.SaveRequest(request))) return;   // store it

            RefreshData();
            SetStatus($"{request.GetReferenceNumber()} {verb}.");
        }
        catch (InvalidOperationException ex)
        {
            Dialog.Warn(ex.Message, "Cannot perform this action");
        }
    }

    private void RecordPayment()
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        if (request.Fee <= 0)
        {
            Dialog.Info($"This document carries no fee.\n\nBasis: {request.FeeBasis}",
                        "No payment due");
            return;
        }

        if (request.IsPaid)
        {
            Dialog.Info($"Already paid.\n\nO.R. Number: {request.OfficialReceiptNo}",
                        "Already paid");
            return;
        }

        using var dialog = new PaymentForm(request);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            request.RecordPayment(dialog.OfficialReceiptNo);
            if (!Attempt(() => Repository.SaveRequest(request))) return;

            RefreshData();
            SetStatus($"Recorded ₱{request.Fee:N2} for {request.GetReferenceNumber()} " +
                      $"(O.R. {request.OfficialReceiptNo}).");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Dialog.Warn(ex.Message, "Cannot record payment");
        }
    }

    private void RejectRequest()
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        string reason = Prompt.Show(this, "Reason for rejection",
                                    $"Why is {request.GetReferenceNumber()} being rejected?");

        if (string.IsNullOrWhiteSpace(reason)) return;   // cancelled

        try
        {
            request.Reject(reason);
            if (!Attempt(() => Repository.SaveRequest(request))) return;

            RefreshData();
            SetStatus($"{request.GetReferenceNumber()} rejected: {reason}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Dialog.Warn(ex.Message, "Cannot reject");
        }
    }

    private void PrintDocument()
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        // The renderer is injected — this view never names a template class.
        string text = _renderer.Render(request);

        using var dialog = new DocumentPreviewForm(request.GetDocumentName(), text);
        dialog.ShowDialog(this);
    }
}
