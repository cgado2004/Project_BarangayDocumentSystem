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

public class RequestsView : ViewBase
{
    private readonly DocumentRenderer _renderer;
    private readonly FeeSchedule _feeSchedule;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _statusFilter = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Document Requests";
    public override string Subtitle => "Track requests from filing through release";

    public RequestsView(IBarangayRepository repository, DocumentRenderer renderer, FeeSchedule feeSchedule)
        : base(repository)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
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
            Width = 260,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 7, 0, 7)
        };

        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Font = AppTheme.BodyFont;
        _statusFilter.Dock = DockStyle.Fill;
        _statusFilter.FlatStyle = FlatStyle.Flat;
        _statusFilter.BackColor = AppTheme.Background;
        _statusFilter.Items.Add("All statuses");
        // Released documents are kept on the Records page, not here.
        foreach (var name in Enum.GetNames(typeof(RequestStatus)))
            if (name != nameof(RequestStatus.Released))
                _statusFilter.Items.Add(name);
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => RefreshData();

        wrap.Controls.Add(_statusFilter);

        var label = new Label
        {
            Text = "Show",
            Dock = DockStyle.Left,
            Width = 56,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.TextSecondary,
            Padding = new Padding(0, 0, 8, 0)
        };

        bar.Controls.Add(wrap);
        bar.Controls.Add(label);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellFormatting += Grid_CellFormatting;

        var host = UiFactory.CardHost(_grid);
        host.Margin = new Padding(0, 0, 0, 16);

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Font = AppTheme.BodyFont;
        _emptyLabel.ForeColor = AppTheme.TextMuted;
        _emptyLabel.BackColor = AppTheme.Surface;
        _emptyLabel.Text = "No requests match this filter";
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

        // New Document replaces the old "Start Processing" button: filing the
        // request is what starts it.
        var newDocument = UiFactory.PrimaryButton("New Document", 140);
        newDocument.Click += (_, _) => NewDocument();
        actions.Controls.Add(newDocument);

        var markReady = UiFactory.SecondaryButton("Mark Ready", 130);
        markReady.Click += (_, _) => ChangeStatus(r => r.MarkReadyForRelease(), "marked ready for release");
        actions.Controls.Add(markReady);

        // Release shows the finished document first, then asks for confirmation.
        var release = UiFactory.SecondaryButton("Release", 110);
        release.Click += (_, _) => ReleaseDocument();
        actions.Controls.Add(release);

        var pay = UiFactory.SecondaryButton("Record Payment", 140);
        pay.Click += (_, _) => RecordPayment();

        var print = UiFactory.SecondaryButton("View / Print", 120);
        print.Click += (_, _) => PrintDocument();

        var reject = UiFactory.DangerButton("Reject", 90);
        reject.Click += (_, _) => RejectRequest();

        actions.Controls.Add(pay);
        actions.Controls.Add(print);
        actions.Controls.Add(reject);
        return actions;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
        if (_grid.Columns[e.ColumnIndex].Name != "Status") return;
        if (e.Value is not string status) return;

        e.CellStyle!.ForeColor = AppTheme.StatusColor(status);
        e.CellStyle.Font = AppTheme.BodyBoldFont;
    }

    public override void RefreshData()
    {
        RequestStatus? filter = _statusFilter.SelectedIndex <= 0
            ? null
            : (RequestStatus)Enum.Parse(typeof(RequestStatus), _statusFilter.Text);

        _grid.DataSource = Repository.GetRequestsByStatus(filter)
            .Where(r => r.Status != RequestStatus.Released)
            .Select(r => new
            {
                Reference = r.GetReferenceNumber(),
                ID = r.RequestId,
                Resident = r.Resident.GetFullName(),
                Document = r.GetDocumentName(),
                Purpose = r.Purpose,
                Requested = r.DateRequested.ToString("yyyy-MM-dd"),
                Status = r.Status.ToString(),
                Fee = r.Fee > 0 ? $"₱{r.Fee:N2}" : "FREE",
                Paid = r.Fee > 0 ? (r.IsPaid ? "Yes" : "No") : "—"
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;

        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    public void ApplyNavigationArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument)) return;
        int idx = _statusFilter.Items.IndexOf(argument);
        if (idx >= 0) _statusFilter.SelectedIndex = idx;
    }

    private DocumentRequest? Selected()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) return null;
        return Repository.Requests.FirstOrDefault(r => r.RequestId == id);
    }

    /// <summary>
    /// Pick a resident, choose the document, and file it. The request goes
    /// straight into Processing — this is the step "Start Processing" used to be.
    /// </summary>
    private void NewDocument()
    {
        using var picker = new ResidentPickerForm(Repository);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedResident is null) return;
        var resident = picker.SelectedResident;

        using var dialog = new RequestForm(resident, _feeSchedule);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var request = AttemptGet(() =>
        {
            var filed = Repository.CreateRequest(resident, dialog.DocumentType, dialog.Purpose, dialog.IsStudent);
            filed.StartProcessing();
            Repository.SaveRequest(filed);
            return filed;
        });
        if (request is null) return;

        // Make sure the new row is visible whatever the filter was.
        _statusFilter.SelectedIndex = 0;
        RefreshData();

        string fee = request.Fee > 0
            ? $"Fee: ₱{request.Fee:N2}"
            : $"FREE — {request.FeeBasis}";

        SetStatus($"Filed {request.GetReferenceNumber()} — {request.GetDocumentName()} for " +
                  $"{resident.GetFullName()}. {fee}. Now processing.");
    }

    /// <summary>
    /// Preview the document exactly as it will be handed over, then confirm.
    /// Releasing is the one step that cannot be undone.
    /// </summary>
    private void ReleaseDocument()
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        if (!request.CanRelease(out string reason))
        {
            Dialog.Warn(reason, "Cannot perform this action");
            return;
        }

        using (var preview = new DocumentPreviewForm(request.GetDocumentName(), _renderer.Render(request)))
            preview.ShowDialog(this);

        if (!Dialog.Confirm(
                $"Release {request.GetReferenceNumber()} — {request.GetDocumentName()} " +
                $"to {request.Resident.GetFullName()}?\n\n" +
                "Once released, it can no longer be rejected.",
                "Confirm release"))
            return;

        ChangeStatus(r => r.Release(), "released and moved to Records");
    }

    private void ChangeStatus(Action<DocumentRequest> action, string verb)
    {
        var request = Selected();
        if (request is null) { Dialog.SelectFirst("request"); return; }

        try
        {
            action(request);
            if (!Attempt(() => Repository.SaveRequest(request))) return;

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

        if (string.IsNullOrWhiteSpace(reason)) return;

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

        string text = _renderer.Render(request);

        using var dialog = new DocumentPreviewForm(request.GetDocumentName(), text);
        dialog.ShowDialog(this);
    }
}

/// <summary>
/// Released documents. Once a request is released it leaves the Requests page
/// and is kept here as a permanent record that can be searched, viewed and
/// reprinted. Nothing on this page can be edited.
/// </summary>
public class RecordsView : ViewBase
{
    private readonly DocumentRenderer _renderer;
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Records";
    public override string Subtitle => "Released documents — a permanent record of everything issued";

    public RecordsView(IBarangayRepository repository, DocumentRenderer renderer) : base(repository)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
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
        CueBanner.Set(_search, "Search by reference, resident, or document…");
        _search.TextChanged += (_, _) => RefreshData();

        wrap.Controls.Add(_search);
        bar.Controls.Add(wrap);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ViewDocument(); };

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

        var view = UiFactory.PrimaryButton("View / Print", 140);
        view.Click += (_, _) => ViewDocument();

        actions.Controls.Add(view);
        return actions;
    }

    public override void RefreshData()
    {
        string term = _search.Text.Trim();

        var released = Repository.GetRequestsByStatus(RequestStatus.Released)
            .Where(r => term.Length == 0
                     || r.GetReferenceNumber().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                     || r.Resident.GetFullName().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                     || r.GetDocumentName().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(r => r.DateReleased ?? r.DateRequested);

        _grid.DataSource = released
            .Select(r => new
            {
                Reference = r.GetReferenceNumber(),
                ID = r.RequestId,
                Resident = r.Resident.GetFullName(),
                Document = r.GetDocumentName(),
                Purpose = r.Purpose,
                Requested = r.DateRequested.ToString("yyyy-MM-dd"),
                Released = r.DateReleased?.ToString("yyyy-MM-dd HH:mm") ?? "",
                Fee = r.Fee > 0 ? $"₱{r.Fee:N2}" : "FREE",
                OR = r.OfficialReceiptNo
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn)
            idColumn.Visible = false;
        if (_grid.Columns["OR"] is { } orColumn)
            orColumn.HeaderText = "O.R. No.";

        _emptyLabel.Text = term.Length == 0
            ? "No released documents yet — documents you release will be kept here"
            : $"No records match \u201c{term}\u201d";
        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    private void ViewDocument()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) { Dialog.SelectFirst("record"); return; }

        var request = Repository.Requests.FirstOrDefault(r => r.RequestId == id);
        if (request is null) return;

        using var dialog = new DocumentPreviewForm(request.GetDocumentName(), _renderer.Render(request));
        dialog.ShowDialog(this);
    }
}
