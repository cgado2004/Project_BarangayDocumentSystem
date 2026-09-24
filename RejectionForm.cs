using System;
using System.Drawing;
using System.Windows.Forms;
using BarangayDocumentSystem.Helper;
using BarangayDocumentSystem.Models;
using static BarangayDocumentSystem.Helper.AppTheme;

namespace BarangayDocumentSystem;

/// <summary>
/// The rejection dialog, ported into v3.1.1 from Jonathan F. Del Rosario's
/// Draft branch and rebuilt on this project's dialog conventions.
///
/// It replaces the plain text prompt the request screen used before, for two
/// reasons the old prompt could not carry:
///
/// 1. A rejected request is often one that was ALREADY PAID, so the dialog
///    says plainly that the recorded payment stays in the collection
///    history - this app does not issue refunds. The clerk should read that
///    before confirming, not discover it in the audit afterwards.
/// 2. The reason is validated here (required, at most 300 characters), the
///    same rules the record itself enforces in
///    <see cref="DocumentRequest.Reject"/> - the dialog only mirrors them,
///    exactly as <see cref="RequestsView"/> only mirrors the state machine.
/// </summary>
public partial class RejectionForm : DialogBase
{
    /// <summary>The trimmed rejection reason, when the user confirmed.</summary>
    public string Reason { get; private set; } = string.Empty;

    public RejectionForm()
    {
        InitializeComponent();
        BuildUi();
    }

    /// <summary>I describe exactly what is about to be rejected, and warn
    /// about the money when the request was paid.</summary>
    public RejectionForm(DocumentRequest request) : this()
    {
        ArgumentNullException.ThrowIfNull(request);

        Text = $"Reject request — {request.GetReferenceNumber()}";

        lblSummary.Text =
            $"Reject {request.GetReferenceNumber()} — {request.GetDocumentName()} " +
            $"for {request.Resident.GetFullName()}?" +
            (request.IsPaid
                ? "\n\nThe payment recorded against this request stays in the " +
                  "collection history. This app does not issue refunds."
                : string.Empty);
    }

    /// <summary>The styling the designer half cannot know: theme fonts and
    /// colours, applied here so the dialog follows AppTheme like the rest.</summary>
    private void BuildUi()
    {
        lblSummary.Font = Body;
        lblSummary.ForeColor = Ink;
        lblSummary.BackColor = Color.Transparent;

        lblReasonCaption.Font = Small;
        lblReasonCaption.ForeColor = Muted;
        lblReasonCaption.BackColor = Color.Transparent;

        txtReason.Font = Body;
        txtReason.BackColor = Surface;
        txtReason.ForeColor = Ink;

        btnConfirm.Accent = Danger;
    }

    private void Confirm(object? sender, EventArgs e)
    {
        var check = InputValidator.RequiredText(txtReason.Text, "Reason", maxLength: 300);
        if (!check.Ok)
        {
            Dialog.FieldProblem(this, "Rejection", check.Error);
            return;
        }

        Reason = txtReason.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Cancel(object? sender, EventArgs e) => Close();
}
