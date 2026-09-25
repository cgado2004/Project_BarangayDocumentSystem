using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Views;
using BarangayDocumentSystem.CustomControls;
using System.Windows.Forms;
using BarangayDocumentSystem.UIHelpers;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;
using static BarangayDocumentSystem.UIHelpers.AppTheme;

namespace BarangayDocumentSystem.Forms;

/// <summary>
/// Filing a new request.
///
/// I show the fee live as the document type changes, so the clerk can tell the
/// resident the price before anything is filed - and when the law forbids the
/// document outright (a second RA 11261 claim, a cedula for a minor), I
/// disable the button and print the reason rather than letting the request
/// in to fail later.
///
/// THE v3.1 PART: four documents are priced by circumstance, not by a posted
/// flat rate, so the form grows the right input for whichever one is chosen:
///
///   Business clearance .. the assessed amount + the law or ordinance violated
///   Cedula .............. the sworn gross annual income (₱5 + ₱1/₱1,000)
///   Facility use ........ the hours (billed at ₱200 per hour or part)
///   Taripa items ........ the assessed amount + the Taripa line
///
/// and for a Barangay Clearance, a first-time jobseeker may claim the RA
/// 11261 one-time waiver - the law covers the clearance as well as the
/// certificate - so the checkbox appears for a qualifying resident.
/// </summary>
public partial class RequestForm : DialogBase
{
    /// <summary>The request input built on confirm. Null when cancelled.</summary>
    public RequestInput? Input { get; private set; }

    /// <summary>The chosen document type, set on confirm.</summary>
    public DocumentType SelectedType { get; private set; }

    /// <summary>The stated purpose, set on confirm.</summary>
    public string Purpose { get; private set; } = string.Empty;

    private readonly Resident? _resident;
    private readonly FeeSchedule? _fees;

    private readonly ComboBox _type = UiFactory.ComboBox();
    private readonly ComboBox _scope = UiFactory.ComboBox("Local employment", "For work abroad");
    private readonly TextBox _purpose = new();

    // ---- the variable-fee inputs, shown only when they apply ----
    private readonly Panel _variableArea = new();
    private readonly NumericUpDown _amount = UiFactory.Number(0m, 100_000m, 0m, increment: 50m);
    private readonly ComboBox _violatedLaw = UiFactory.EditableComboBox(
        "Operating without a barangay clearance",
        "Operating beyond the approved business line",
        "Late renewal of the business clearance",
        "City Revenue Code violation",
        "Barangay ordinance violation");
    private readonly NumericUpDown _income = UiFactory.Number(0m, 10_000_000m, 0m, increment: 10_000m, decimals: 0);
    private readonly NumericUpDown _hours = UiFactory.Number(0.25m, 720m, 1m, increment: 0.25m);
    private readonly TextBox _detail = new();

    // ---- the RA 11261 claim on a barangay clearance ----
    private readonly CheckBox _jobseekerWaiver = UiFactory.CheckBox(
        "Issue under RA 11261 — first-time jobseeker, one-time waiver");

    // ---- the live assessment ----
    private readonly Label _feeLine = new();
    private readonly Label _blockLine = new();

    public RequestForm()
    {
        InitializeComponent();
        BuildUi();
    }

    public RequestForm(Resident resident, FeeSchedule fees) : this()
    {
        _resident = resident;
        _fees = fees;

        Text = $"New request — {resident.GetFullName()}";
        RebuildVariableArea();   // the handlers above are not attached yet when
        Recalculate();          // BuildUi sets SelectedIndex, so I prime both here
    }

    private void BuildUi()
    {
        foreach (var t in ((DocumentType[])Enum.GetValues(typeof(DocumentType))))
            _type.Items.Add(FeeSchedule.NameOf(t));
        _type.SelectedIndex = 0;

        _purpose.Font = Body;
        _purpose.BackColor = Surface;
        _purpose.ForeColor = Ink;
        _purpose.Dock = DockStyle.Top;
        _purpose.Multiline = true;
        _purpose.Height = 56;

        _feeLine.Font = Heading;
        _feeLine.ForeColor = Primary;
        _feeLine.Dock = DockStyle.Top;
        _feeLine.Height = 34;
        _feeLine.BackColor = System.Drawing.Color.Transparent;

        _blockLine.Font = Body;
        _blockLine.ForeColor = Danger;
        _blockLine.Dock = DockStyle.Top;
        _blockLine.Height = 58;
        _blockLine.BackColor = System.Drawing.Color.Transparent;

        _variableArea.Dock = DockStyle.Top;
        _variableArea.BackColor = System.Drawing.Color.Transparent;
        _variableArea.Height = 0;
        _variableArea.AutoScroll = false;

        _jobseekerWaiver.Dock = DockStyle.Top;
        _jobseekerWaiver.Height = 34;
        _jobseekerWaiver.Visible = false;
        _jobseekerWaiver.ForeColor = Ink;

        // One column of fields in a percent grid: the dialog reflows, the
        // fields never clip.
        var grid = UiFactory.Grid(1);
        grid.Controls.Add(UiFactory.Field("Document type *", _type));
        grid.Controls.Add(UiFactory.Field("Clearance is for", _scope));
        grid.Controls.Add(UiFactory.Field("Purpose *", _purpose));
        grid.Controls.Add(_jobseekerWaiver);
        grid.Controls.Add(_variableArea);
        grid.Controls.Add(_feeLine);
        grid.Controls.Add(_blockLine);
        grid.Dock = DockStyle.Top;

        requestScroll.Controls.Add(grid);

        _type.SelectedIndexChanged += (_, _) => { RebuildVariableArea(); Recalculate(); };
        _scope.SelectedIndexChanged += (_, _) => Recalculate();
        _purpose.TextChanged        += (_, _) => Recalculate();
        _amount.ValueChanged        += (_, _) => Recalculate();
        _violatedLaw.TextChanged    += (_, _) => Recalculate();
        _income.ValueChanged        += (_, _) => Recalculate();
        _hours.ValueChanged         += (_, _) => Recalculate();
        _detail.TextChanged         += (_, _) => Recalculate();
        _jobseekerWaiver.CheckedChanged += (_, _) => Recalculate();
    }

    private DocumentType CurrentType()
    {
        int index = Math.Max(0, _type.SelectedIndex);
        var values = ((DocumentType[])Enum.GetValues(typeof(DocumentType)));
        return values.Length > 0 ? values[Math.Min(index, values.Length - 1)] : DocumentType.OtherCertification;
    }

    /// <summary>
    /// I rebuild the variable-fee input area for the chosen document. The
    /// area is one host panel; I clear it and add the fields the document
    /// needs, so at most one set of inputs is ever on screen.
    /// </summary>
    private void RebuildVariableArea()
    {
        _variableArea.Controls.Clear();
        _variableArea.Height = 0;

        var type = CurrentType();
        _scope.Enabled = type == DocumentType.BarangayClearance;

        // The RA 11261 claim only exists for a barangay clearance, and only
        // for a resident the law can still help: never availed, and past the
        // six-month residency test.
        bool waiverPossible = _resident is not null
            && type == DocumentType.BarangayClearance
            && !_resident.HasAvailedFirstTimeJobseeker
            && _resident.GetMonthsOfResidency() >= (_fees?.JobseekerResidencyMonths ?? 6);
        _jobseekerWaiver.Visible = waiverPossible;
        if (!waiverPossible) _jobseekerWaiver.Checked = false;

        Control? added = null;
        int height = 0;

        switch (type)
        {
            case DocumentType.BarangayBusinessClearance:
                _amount.Value = Math.Max(_amount.Value, _fees?.BusinessClearanceStandard ?? 200m);
                added = VariablePanel(
                    UiFactory.Field("Assessed amount (₱) *", _amount),
                    UiFactory.Field("Law or ordinance violated", _violatedLaw));
                height = 128;
                break;

            case DocumentType.CommunityTaxCertificate:
                added = VariablePanel(
                    UiFactory.Field("Sworn gross annual income (₱)", _income),
                    UiFactory.Label("Computed: ₱5.00 basic + ₱1.00 for every ₱1,000 " +
                                    "of income (RA 7160, Sec. 156)", Small, Muted));
                height = 128;
                break;

            case DocumentType.BarangayFacilityRental:
                added = VariablePanel(
                    UiFactory.Field("Hours of use *", _hours),
                    UiFactory.Field("Facility", _detail));
                height = 128;
                break;

            case DocumentType.OtherTarifaProcessingFee:
                added = VariablePanel(
                    UiFactory.Field("Assessed amount (₱) *", _amount),
                    UiFactory.Field("Barangay Taripa item *", _detail));
                height = 128;
                break;
        }

        if (added is not null)
        {
            _variableArea.Controls.Add(added);
            _variableArea.Height = height;
        }
    }

    /// <summary>The two-up host for a variable-fee pair: a 50/50 percent
    /// grid, so the two inputs share the row evenly at any width.</summary>
    private static Control VariablePanel(Control left, Control right)
    {
        var pair = UiFactory.Grid(2);
        pair.Controls.Add(left, 0, 0);
        pair.Controls.Add(right, 1, 0);
        pair.Dock = DockStyle.Top;
        return pair;
    }

    /// <summary>The request input exactly as the form stands right now. The
    /// same builder feeds the live assessment and the eventual filing, so
    /// the fee line can never show a price the repository will not
    /// reproduce.</summary>
    private RequestInput CurrentInput()
    {
        var type = CurrentType();

        var scope = _scope.SelectedIndex == 1
            ? ClearanceScope.Abroad
            : ClearanceScope.Local;

        return new RequestInput(
            Scope: type == DocumentType.BarangayClearance ? scope : ClearanceScope.Local,
            Amount: type is DocumentType.BarangayBusinessClearance
                         or DocumentType.OtherTarifaProcessingFee
                ? _amount.Value : 0m,
            Hours: type == DocumentType.BarangayFacilityRental ? _hours.Value : 0m,
            GrossAnnualIncome: type == DocumentType.CommunityTaxCertificate ? _income.Value : 0m,
            Detail: type switch
            {
                DocumentType.BarangayBusinessClearance => _violatedLaw.Text,
                DocumentType.BarangayFacilityRental    => _detail.Text,
                DocumentType.OtherTarifaProcessingFee  => _detail.Text,
                _ => string.Empty
            },
            ApplyJobseekerWaiver: type == DocumentType.BarangayClearance && _jobseekerWaiver.Checked);
    }

    /// <summary>
    /// I re-run the fee rules whenever any choice changes, and I disable the
    /// button outright when the law forbids the document altogether.
    /// </summary>
    private void Recalculate()
    {
        if (_resident is null || _fees is null) return;

        var input = CurrentInput();
        var assessment = _fees.Assess(_resident, CurrentType(), input);

        if (assessment.IsBlocked)
        {
            _feeLine.Text = "Cannot be issued";
            _feeLine.ForeColor = Danger;
            _blockLine.Text = assessment.BlockReason;
            btnConfirm.Enabled = false;
            return;
        }

        _blockLine.Text = string.Empty;
        btnConfirm.Enabled = true;
        _feeLine.ForeColor = assessment.IsWaived ? Success : Primary;
        _feeLine.Text = assessment.IsWaived
            ? $"FREE  —  {assessment.Basis}"
            : $"{DisplayFormat.Peso(assessment.FinalFee)}  —  {assessment.Basis}";
    }

    private void Confirm(object? sender, EventArgs e)
    {
        var purposeCheck = InputValidator.Purpose(_purpose.Text);
        if (!purposeCheck.Ok)
        {
            Dialog.FieldProblem(this, "Request", purposeCheck.Error);
            return;
        }

        var type = CurrentType();
        var input = CurrentInput();

        // The variable-fee documents have inputs of their own to check. The
        // fee schedule would refuse them anyway (that is its job), but a
        // named message at the field beats a generic refusal at the bottom.
        if (type == DocumentType.OtherTarifaProcessingFee)
        {
            var detailCheck = InputValidator.RequiredText(input.Detail, "Barangay Taripa item");
            if (!detailCheck.Ok) { Dialog.FieldProblem(this, "Request", detailCheck.Error); return; }

            var amountCheck = InputValidator.AssessedAmount(input.Amount, "The assessed amount");
            if (!amountCheck.Ok) { Dialog.FieldProblem(this, "Request", amountCheck.Error); return; }
        }

        if (type == DocumentType.BarangayFacilityRental)
        {
            var hoursCheck = InputValidator.Hours(input.Hours);
            if (!hoursCheck.Ok) { Dialog.FieldProblem(this, "Request", hoursCheck.Error); return; }
        }

        SelectedType = type;
        Purpose = _purpose.Text.Trim();
        Input = input;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Cancel(object? sender, EventArgs e) => Close();
}
