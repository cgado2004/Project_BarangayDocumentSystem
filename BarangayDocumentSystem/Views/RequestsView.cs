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
    private readonly DataGridView _grid = new();
    private readonly ComboBox _statusFilter = new();
    private readonly Label _emptyLabel = new();

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
            Width = 260,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 6, 0, 6)
        };

        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Font = AppTheme.BodyFont;
        _statusFilter.Dock = DockStyle.Fill;
        _statusFilter.FlatStyle = FlatStyle.Flat;
        _statusFilter.BackColor = AppTheme.Background;
        _statusFilter.Items.Add("All statuses");
        foreach (var name in Enum.GetNames(typeof(RequestStatus)))
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

        var transitions = new (string Text, Action<DocumentRequest> Action, string Verb)[]
        {
            ("Start Processing", r => r.StartProcessing(), "moved to processing"),
            ("Mark Ready", r => r.MarkReadyForRelease(), "marked ready for release"),
            ("Release", r => r.Release(), "released")
        };

        foreach (var (text, action, verb) in transitions)
        {
            var button = text == "Release"
                ? UiFactory.PrimaryButton(text, 110)
                : UiFactory.SecondaryButton(text, 130);

            button.Click += (_, _) => ChangeStatus(action, verb);
            actions.Controls.Add(button);
        }

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