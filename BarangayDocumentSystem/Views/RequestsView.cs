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
    private readonly string _receivedBy;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _statusFilter = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Document Requests";
    public override string Subtitle => "Track requests from filing through release";

    /// <param name="receivedBy">Signed-in staff name, recorded on every payment.</param>
    public RequestsView(IBarangayRepository repository, DocumentRenderer renderer,
                        FeeSchedule feeSchedule, string receivedBy)
        : base(repository)
    {
        _receivedBy = receivedBy ?? string.Empty;
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

        // New Request replaces the old "Start Processing" button: filing the
        // request is what starts it.
        var newDocument = UiFactory.PrimaryButton("New Request", 140);
        newDocument.Click += (_, _) => NewRequest();
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
    private void NewRequest()
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

        DocumentPrinter.Show(this, request, _renderer);

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

        using var dialog = new PaymentForm(request, Repository.GetNextOfficialReceiptNo());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            request.RecordPayment(dialog.OfficialReceiptNo, _receivedBy);
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

        DocumentPrinter.Show(this, request, _renderer);
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
    private readonly DateRangeFilter _dates = new();
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

        // Filters by the date the document was released.
        _dates.Changed += (_, _) => RefreshData();

        wrap.Controls.Add(_search);
        bar.Controls.Add(wrap);
        bar.Controls.Add(_dates);
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
            .Where(r => _dates.Matches(r.DateReleased ?? r.DateRequested))
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

        _emptyLabel.Text = _dates.Active
            ? "No released documents in the selected dates"
            : term.Length == 0
                ? "No released documents yet — documents you release will be kept here"
                : $"No records match \u201c{term}\u201d";
        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    private void ViewDocument()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) { Dialog.SelectFirst("record"); return; }

        var request = Repository.Requests.FirstOrDefault(r => r.RequestId == id);
        if (request is null) return;

        DocumentPrinter.Show(this, request, _renderer);
    }
}

/// <summary>
/// Official receipts: one row for every payment taken, with the O.R. number,
/// the date, who paid, what for, how much and who received it. Also shows the
/// next O.R. number that is still available. Documents with no fee have no
/// receipt, so they do not appear here.
/// </summary>
public class OfficialReceiptsView : ViewBase
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    private readonly DateRangeFilter _dates = new();
    private readonly Label _availability = new();
    private readonly Label _emptyLabel = new();

    public override string Title => "Official Receipts";
    public override string Subtitle => "Payment records and the next available O.R. number";

    public OfficialReceiptsView(IBarangayRepository repository) : base(repository)
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
            Width = 340,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 7, 0, 7)
        };

        _search.Dock = DockStyle.Fill;
        _search.Font = AppTheme.BodyFont;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.BackColor = AppTheme.Background;
        CueBanner.Set(_search, "Search by O.R. number, resident, or reference…");
        _search.TextChanged += (_, _) => RefreshData();
        wrap.Controls.Add(_search);

        // Filters by the date the payment was made.
        _dates.Changed += (_, _) => RefreshData();

        bar.Controls.Add(wrap);
        bar.Controls.Add(_dates);
        return bar;
    }

    private Control BuildGrid()
    {
        UiFactory.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) ViewReceipt(); };

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

        var view = UiFactory.PrimaryButton("View / Print Receipt", 180);
        view.Click += (_, _) => ViewReceipt();

        actions.Controls.Add(view);

        // The next O.R. number and the day's totals sit at the right of this row.
        _availability.Dock = DockStyle.Right;
        _availability.Width = 460;
        _availability.TextAlign = ContentAlignment.MiddleRight;
        _availability.Font = AppTheme.SmallFont;
        _availability.ForeColor = AppTheme.TextSecondary;

        var row = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background, Margin = new Padding(0) };
        row.Controls.Add(actions);
        row.Controls.Add(_availability);
        return row;
    }

    private static bool HasReceipt(DocumentRequest r) =>
        r.IsPaid && !string.IsNullOrWhiteSpace(r.OfficialReceiptNo);

    public override void RefreshData()
    {
        string term = _search.Text.Trim();

        var paid = Repository.Requests.Where(HasReceipt).ToList();

        var today = DateTime.Today;
        var paidToday = paid.Where(r => r.DatePaid?.Date == today).ToList();
        var inRange = paid.Where(r => _dates.Matches(r.DatePaid ?? r.DateRequested)).ToList();
        _availability.Text =
            $"Next available O.R.:  {Repository.GetNextOfficialReceiptNo()}\n" +
            (_dates.Active
                ? $"In the selected dates: {inRange.Count} receipt(s)   ·   Collected: ₱{inRange.Sum(r => r.Fee):N2}"
                : $"Issued today: {paidToday.Count}   ·   Collected today: ₱{paidToday.Sum(r => r.Fee):N2}");

        var rows = inRange
            .Where(r => term.Length == 0
                     || r.OfficialReceiptNo.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                     || r.Resident.GetFullName().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                     || r.GetReferenceNumber().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(r => r.DatePaid ?? r.DateRequested)
            .ThenByDescending(r => r.RequestId);

        _grid.DataSource = rows
            .Select(r => new
            {
                ORNumber = r.OfficialReceiptNo,
                ID = r.RequestId,
                DatePaid = r.DatePaid?.ToString("yyyy-MM-dd HH:mm") ?? "",
                Reference = r.GetReferenceNumber(),
                Resident = r.Resident.GetFullName(),
                Document = r.GetDocumentName(),
                Amount = $"₱{r.Fee:N2}",
                ReceivedBy = r.ReceivedBy,
                Status = r.Status.ToString()
            })
            .ToList();

        if (_grid.Columns["ID"] is { } idColumn) idColumn.Visible = false;
        if (_grid.Columns["ORNumber"] is { } orColumn) orColumn.HeaderText = "O.R. Number";
        if (_grid.Columns["DatePaid"] is { } dateColumn) dateColumn.HeaderText = "Date Paid";
        if (_grid.Columns["ReceivedBy"] is { } byColumn) byColumn.HeaderText = "Received By";

        _emptyLabel.Text = _dates.Active
            ? "No receipts in the selected dates"
            : term.Length == 0
                ? "No payments recorded yet — payments you record on the Requests page appear here"
                : $"No receipts match \u201c{term}\u201d";
        _emptyLabel.Visible = _grid.Rows.Count == 0;
    }

    private void ViewReceipt()
    {
        if (_grid.CurrentRow?.Cells["ID"].Value is not int id) { Dialog.SelectFirst("receipt"); return; }

        var request = Repository.Requests.FirstOrDefault(r => r.RequestId == id);
        if (request is null) return;

        using var dialog = new DocumentPreviewForm("Official Receipt " + request.OfficialReceiptNo,
                                                   BuildReceiptText(request));
        dialog.ShowDialog(this);
    }

    private static string BuildReceiptText(DocumentRequest r)
    {
        var profile = BarangayProfile.MagugpoPoblacion;
        string line = new string('-', 56);
        string paidOn = r.DatePaid?.ToString("MMMM d, yyyy  h:mm tt") ?? "(not recorded)";

        return string.Join(Environment.NewLine, new[]
        {
            profile.ProvinceName,
            profile.CityName,
            profile.BarangayName,
            "",
            "OFFICIAL RECEIPT",
            line,
            $"O.R. No.       : {r.OfficialReceiptNo}",
            $"Date paid      : {paidOn}",
            line,
            $"Received from  : {r.Resident.GetFullName()}",
            $"Address        : {r.Resident.GetAddressText()}",
            $"Payment for    : {r.GetDocumentName()}",
            $"Reference no.  : {r.GetReferenceNumber()}",
            $"Purpose        : {r.Purpose}",
            line,
            $"AMOUNT PAID    : \u20b1{r.Fee:N2}",
            line,
            $"Received by    : {(string.IsNullOrWhiteSpace(r.ReceivedBy) ? "(not recorded)" : r.ReceivedBy)}",
            "",
            "This receipt is proof of payment of the fee above."
        });
    }
}

/// <summary>
/// "Filter by date" strip for the toolbar: tick the box, pick From and To (both
/// days included). Unticked means every date. The Today button selects today.
/// </summary>
internal sealed class DateRangeFilter : FlowLayoutPanel
{
    private readonly CheckBox _enable = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();

    public event EventHandler? Changed;

    /// <summary>True while a date range is being applied.</summary>
    public bool Active => _enable.Checked;

    public DateRangeFilter()
    {
        Dock = DockStyle.Right;
        FlowDirection = FlowDirection.LeftToRight;
        WrapContents = false;
        BackColor = AppTheme.Surface;
        Margin = new Padding(0);
        Padding = new Padding(0);

        // The toolbar leaves 52 px of height, so everything is centred in that.
        _enable.Text = "Filter by date";
        _enable.AutoSize = false;
        _enable.Width = 130;
        _enable.Height = 52;
        _enable.Font = AppTheme.BodyFont;
        _enable.Margin = new Padding(0);
        _enable.CheckedChanged += (_, _) =>
        {
            _from.Enabled = _to.Enabled = _enable.Checked;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        ConfigurePicker(_from);
        ConfigurePicker(_to);
        _from.Margin = new Padding(0, 13, 10, 0);
        _to.Margin = new Padding(0, 13, 12, 0);
        _from.ValueChanged += (_, _) => { if (_enable.Checked) Changed?.Invoke(this, EventArgs.Empty); };
        _to.ValueChanged += (_, _) => { if (_enable.Checked) Changed?.Invoke(this, EventArgs.Empty); };

        var today = UiFactory.SecondaryButton("Today", 70);
        today.Margin = new Padding(0, 13, 0, 0);
        today.Height = _from.Height;   // same height as the date boxes so they line up
        today.Click += (_, _) =>
        {
            _from.Value = DateTime.Today;
            _to.Value = DateTime.Today;
            _enable.Checked = true;
            Changed?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(_enable);
        Controls.Add(MakeLabel("From", 42));
        Controls.Add(_from);
        Controls.Add(MakeLabel("To", 26));
        Controls.Add(_to);
        Controls.Add(today);

        // Wide enough for everything in the strip (the Today button grows with its text).
        int total = 0;
        foreach (Control c in Controls)
            total += Math.Max(c.Width, c.PreferredSize.Width) + c.Margin.Horizontal;
        Width = total + 8;
    }

    /// <summary>True when the date falls inside the chosen range, or when no range is applied.</summary>
    public bool Matches(DateTime date)
    {
        if (!_enable.Checked) return true;

        DateTime a = _from.Value.Date, b = _to.Value.Date;
        if (a > b) { var t = a; a = b; b = t; }
        return date.Date >= a && date.Date <= b;
    }

    private static void ConfigurePicker(DateTimePicker picker)
    {
        picker.Font = AppTheme.BodyFont;
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "MMM d, yyyy";
        picker.Width = 120;
        picker.Value = DateTime.Today;
        picker.Enabled = false;
    }

    private static Label MakeLabel(string text, int width) => new()
    {
        Text = text,
        AutoSize = false,
        Width = width,
        Height = 52,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = AppTheme.BodyFont,
        ForeColor = AppTheme.TextSecondary,
        Margin = new Padding(0)
    };
}
