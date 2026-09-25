using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules;

/// <summary>
/// What I get back when I assess a fee: the amount, and just as importantly
/// the reason for it. I return the reason because a resident can and will ask
/// why they are being charged, and "the computer said so" is not an answer I
/// am willing to give.
/// </summary>
public sealed record FeeAssessment
{
    public decimal BaseFee { get; init; }
    public decimal FinalFee { get; init; }
    public string Basis { get; init; } = string.Empty;
    public bool IsWaived => FinalFee == 0m;
    public bool IsExempt { get; init; }

    /// <summary>I set this when the law forbids issuing the document at all,
    /// for example a second RA 11261 certificate, or a cedula for a minor.
    /// When I set it, the screen must refuse to file the request rather than
    /// just pricing it.</summary>
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;

    /// <summary>True when releasing this request consumes the resident's
    /// once-only RA 11261 benefit.</summary>
    public bool MarksJobseekerAvailment { get; init; }
}

/// <summary>
/// Every peso I charge, and the legal reason behind it.
///
/// WHY I PUT IT ALL IN ONE CLASS
/// A barangay may only collect what its revenue ordinance allows (RA 7160,
/// Sec. 152), so when the ordinance changes I want exactly one file to edit,
/// not twenty forms each carrying their own hard-coded number. This is also
/// NFR-04 in the documentation I wrote.
///
/// WHERE I GOT THESE AMOUNTS
/// From the Barangay Citizen's Charter posted at the hall of Barangay Magugpo
/// Poblacion. They are the real rates, not figures I invented:
///
///   Barangay Clearance (local employment) ..... ₱100
///   Barangay Clearance (work abroad) .......... ₱200
///   Certification (residency, good moral…) .... ₱100
///   Certificate of Indigency .................. FREE
///   Certificate of Low Income ................. FREE
///   Business Clearance ........................ VARIES with the law violated
///   Cedula (community tax) .................... VARIES, RA 7160 Sec. 156
///   Filing a case (Katarungang Pambarangay) ... ₱150
///   Barangay facilities ....................... ₱200 per hour
///   Other processing fees ..................... Barangay Taripa, assessed
///
/// Anything the charter does not price separately I treat as a ₱100
/// certification, which is the charter's own catch-all rate.
///
/// THE WAIVERS, IN ONE PLACE
///   1. RA 9994  - senior citizens .......... personal certificates free
///   2. RA 10754 - persons with disability .. personal certificates free
///   3. Indigent status (RA 11291) .......... personal certificates free
///   4. RA 11261 - first-time jobseekers .... once only, 6 months' residency,
///                 covers the certificate AND the barangay clearance
///
/// None of them touch the business clearance, the cedula, the lupon filing
/// fee, facility rental or the Taripa items: those are regulatory or
/// tax amounts, not personal certificate fees, and an auditor would find
/// the difference.
/// </summary>
public class FeeSchedule
{
    // The charter's real numbers. I default them here and let App.config
    // override them, so my group-mates can correct a rate without rebuilding.
    private readonly decimal _clearanceLocal;
    private readonly decimal _clearanceAbroad;
    private readonly decimal _certification;
    private readonly decimal _businessStandard;
    private readonly decimal _luponFiling;
    private readonly decimal _facilityHourly;
    private readonly decimal _communityTaxBase;
    private readonly decimal _communityTaxPerThousand;
    private readonly decimal _communityTaxCap;

    /// <summary>The months of residency RA 11261 requires before I may issue
    /// a first-time jobseeker certificate.</summary>
    public int JobseekerResidencyMonths { get; }

    // ---- the posted rates, readable by the document templates so the ----
    // ---- computation printed on a certificate matches the fee charged ----

    /// <summary>The basic community tax an individual pays (₱5.00 under
    /// RA 7160 Sec. 156).</summary>
    public decimal CommunityTaxBase => _communityTaxBase;

    /// <summary>The community tax rate per ₱1,000 of sworn gross annual
    /// income (₱1.00).</summary>
    public decimal CommunityTaxPerThousand => _communityTaxPerThousand;

    /// <summary>The most an individual's additional community tax can be
    /// (₱5,000.00).</summary>
    public decimal CommunityTaxCap => _communityTaxCap;

    /// <summary>The charter rate for one hour of barangay facility use
    /// (₱200.00).</summary>
    public decimal FacilityHourly => _facilityHourly;

    /// <summary>The standard business clearance rate (₱200.00), used when no
    /// violation is assessed.</summary>
    public decimal BusinessClearanceStandard => _businessStandard;

    /// <summary>The flat Katarungang Pambarangay filing fee (₱150.00).</summary>
    public decimal LuponFilingFee => _luponFiling;

    /// <summary>RA 11032 prescribes this many working days for a simple
    /// frontline transaction; the request queue flags anything older.</summary>
    public int RA11032SimpleWorkingDays { get; }

    /// <summary>
    /// I build the schedule with the posted Citizen's Charter rates.
    ///
    /// This is the constructor the in-memory demo runs on when nothing
    /// overrides the figures.
    /// </summary>
    public FeeSchedule() : this(
        clearanceLocal: 100m, clearanceAbroad: 200m, certification: 100m,
        businessStandard: 200m, luponFiling: 150m, facilityHourly: 200m,
        communityTaxBase: 5m, communityTaxPerThousand: 1m, communityTaxCap: 5000m,
        jobseekerResidencyMonths: 6, ra11032SimpleWorkingDays: 3) { }

    /// <summary>
    /// I build the schedule from values supplied by App.config.
    ///
    /// I kept this separate from the default constructor so this class never
    /// has to know that a config file exists. Program.cs reads the file and
    /// passes me plain numbers.
    /// </summary>
    public FeeSchedule(
        decimal clearanceLocal, decimal clearanceAbroad, decimal certification,
        decimal businessStandard, decimal luponFiling, decimal facilityHourly,
        decimal communityTaxBase, decimal communityTaxPerThousand, decimal communityTaxCap,
        int jobseekerResidencyMonths, int ra11032SimpleWorkingDays)
    {
        _clearanceLocal  = clearanceLocal;
        _clearanceAbroad = clearanceAbroad;
        _certification   = certification;
        _businessStandard = businessStandard;
        _luponFiling     = luponFiling;
        _facilityHourly  = facilityHourly;
        _communityTaxBase = communityTaxBase;
        _communityTaxPerThousand = communityTaxPerThousand;
        _communityTaxCap = communityTaxCap;
        JobseekerResidencyMonths = jobseekerResidencyMonths;
        RA11032SimpleWorkingDays = ra11032SimpleWorkingDays;
    }

    /// <summary>
    /// I work out what this resident pays for this document, and why.
    ///
    /// The order of the checks below matters a great deal, so I have numbered
    /// them. Getting them out of order would give away money the barangay is
    /// entitled to, or charge someone the law says is exempt.
    /// </summary>
    public FeeAssessment Assess(Resident resident, DocumentType type, RequestInput? input = null)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));
        input ??= RequestInput.Default;

        // ---- 1. Documents the charter gives away to everybody -------------
        // These are free no matter who is asking, so I settle them before I
        // look at any personal exemption.
        if (type == DocumentType.CertificateOfIndigency)
            return Free("FREE — Certificate of Indigency (Citizen's Charter; RA 11291, Magna Carta of the Poor)");

        if (type == DocumentType.CertificateOfLowIncome)
            return Free("FREE — Certificate of Low Income (Citizen's Charter)");

        if (IsAssistanceOrSocialService(type))
            return Free("FREE — assistance and social-service documentation (Citizen's Charter)");

        // ---- 2. RA 11261, first-time jobseekers ---------------------------
        // I check this early because it can BLOCK the request outright rather
        // than merely change the price. The law covers the barangay
        // CERTIFICATE and the barangay CLEARANCE a first-time jobseeker needs
        // for employment, so a clearance can claim the waiver too - once.
        if (type == DocumentType.FirstTimeJobseekerCertificate)
            return AssessJobseeker(resident);

        if (type == DocumentType.BarangayClearance && input.ApplyJobseekerWaiver)
            return AssessJobseeker(resident);

        // ---- 3. Documents whose price is not a fixed number ----------------
        // The charter prices these by circumstance: the business clearance by
        // the law violated, the cedula by sworn income, facilities by the
        // hour, Taripa items as assessed. I put this block BEFORE the personal
        // exemptions on purpose. These are regulatory fees and taxes, not
        // personal certificate fees, so being a senior citizen does not make
        // your shop's permit, your cedula or your case filing free. If I had
        // written this check after the exemptions I would be giving away
        // money an auditor would find.
        switch (type)
        {
            case DocumentType.BarangayBusinessClearance:
                return AssessBusiness(input);

            case DocumentType.CommunityTaxCertificate:
                return AssessCommunityTax(resident, input);

            case DocumentType.LuponCaseFiling:
                return AssessLuponFiling();

            case DocumentType.BarangayFacilityRental:
                return AssessFacility(input);

            case DocumentType.OtherTarifaProcessingFee:
                return AssessTarifa(input);
        }

        // ---- 4. Personal statutory exemptions ------------------------------
        // Only now, after every regulatory amount is out of the way, do I let
        // a personal exemption waive the fee.
        decimal baseFee = BaseFeeFor(type, input.Scope);

        if (resident.HasClassification(ResidentClassification.SeniorCitizen))
            return Exempt(baseFee, "FREE — Senior Citizen (RA 9994, Expanded Senior Citizens Act)");

        if (resident.HasClassification(ResidentClassification.PWD))
            return Exempt(baseFee, "FREE — Person With Disability (RA 10754, Magna Carta for PWDs)");

        if (resident.HasClassification(ResidentClassification.Indigent))
            return Exempt(baseFee, "FREE — Indigent resident (RA 11291; Citizen's Charter)");

        // ---- 5. Ordinary rate ----------------------------------------------
        // Nothing exempted this resident, so I charge the charter rate.
        return new FeeAssessment
        {
            BaseFee  = baseFee,
            FinalFee = baseFee,
            Basis    = BasisFor(type, input.Scope)
        };
    }

    /// <summary>
    /// True when this document is a personal certificate the statutory
    /// exemptions can waive - as opposed to the regulatory and tax amounts
    /// the exemptions must never touch.
    /// </summary>
    public static bool IsPersonalCertificate(DocumentType type) => type
        is not DocumentType.BarangayBusinessClearance
        and not DocumentType.CommunityTaxCertificate
        and not DocumentType.LuponCaseFiling
        and not DocumentType.BarangayFacilityRental
        and not DocumentType.OtherTarifaProcessingFee;

    /// <summary>
    /// True when this document's amount is decided by circumstance rather
    /// than by the posted flat rate - the four the clerk assesses, plus the
    /// clearance that can be priced two ways.
    /// </summary>
    public static bool HasVariableFee(DocumentType type) => type
        is DocumentType.BarangayBusinessClearance
        or DocumentType.CommunityTaxCertificate
        or DocumentType.BarangayFacilityRental
        or DocumentType.OtherTarifaProcessingFee;

    /// <summary>
    /// RA 11261 gives a first-time jobseeker their barangay documents free,
    /// but only under conditions, and only once in their life. So here I
    /// either clear the request at zero or refuse it with a reason I can
    /// show the resident.
    /// </summary>
    private FeeAssessment AssessJobseeker(Resident resident)
    {
        if (resident.HasAvailedFirstTimeJobseeker)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason =
                    "RA 11261 may be availed only ONCE. This resident has already " +
                    "been issued a document under the First Time Jobseekers Act.",
                Basis = "Blocked — RA 11261 already availed"
            };

        int months = resident.GetMonthsOfResidency();
        if (months < JobseekerResidencyMonths)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason =
                    $"RA 11261 requires at least {JobseekerResidencyMonths} months of " +
                    $"residency in the barangay. This resident has {months}.",
                Basis = "Blocked — insufficient residency"
            };

        return new FeeAssessment
        {
            Basis = "FREE — RA 11261 (First Time Jobseekers Assistance Act), availment 1 of 1",
            MarksJobseekerAvailment = true
        };
    }

    /// <summary>
    /// The business clearance. The charter posts it as "amount varies
    /// depending on the law violated", so the clerk assesses the amount and
    /// names the ordinance or law it is assessed under; when nothing was
    /// violated I charge the standard rate. Either way, personal exemptions
    /// do not apply.
    /// </summary>
    private FeeAssessment AssessBusiness(RequestInput input)
    {
        decimal fee = input.Amount > 0m ? input.Amount : _businessStandard;
        string detail = input.Detail.Trim();

        string basis = detail.Length > 0
            ? $"Business clearance — {DisplayFormat.Peso(fee)} assessed for violation of {detail} (Citizen's Charter; RA 7160, Sec. 152). Personal exemptions do not apply."
            : $"Business clearance — standard rate {DisplayFormat.Peso(fee)} (Citizen's Charter; RA 7160, Sec. 152). Personal exemptions do not apply.";

        return new FeeAssessment { BaseFee = _businessStandard, FinalFee = fee, Basis = basis };
    }

    /// <summary>
    /// The cedula. RA 7160, Sec. 156: every inhabitant eighteen or over who
    /// is regularly employed or engaged in business pays a basic community
    /// tax of ₱5.00 plus ₱1.00 for every ₱1,000 of gross income from the
    /// preceding year, the additional portion never to exceed ₱5,000. The
    /// computation runs off the declarant's own sworn statement, which is
    /// why the income arrives on the request input.
    /// </summary>
    private FeeAssessment AssessCommunityTax(Resident resident, RequestInput input)
    {
        if (resident.GetAge() < 18)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason =
                    "The community tax may be paid only by a person at least " +
                    "eighteen (18) years of age (RA 7160, Sec. 156).",
                Basis = "Blocked — under eighteen"
            };

        decimal income = Math.Max(0m, input.GrossAnnualIncome);
        decimal thousands = income <= 0m ? 0m : Math.Ceiling(income / 1000m);
        decimal additional = Math.Min(_communityTaxCap, thousands * _communityTaxPerThousand);
        decimal fee = _communityTaxBase + additional;

        string basis = additional > 0m
            ? $"Community tax — {DisplayFormat.Peso(_communityTaxBase)} basic plus {DisplayFormat.Peso(_communityTaxPerThousand)} for every ₱1,000 of sworn gross annual income (RA 7160, Sec. 156); additional portion capped at {DisplayFormat.Peso(_communityTaxCap)}."
            : $"Community tax — {DisplayFormat.Peso(_communityTaxBase)} basic, no declared taxable income (RA 7160, Sec. 156).";

        return new FeeAssessment { BaseFee = _communityTaxBase, FinalFee = fee, Basis = basis };
    }

    /// <summary>
    /// Filing a case with the Lupong Tagapamayapa: a flat ₱150 under the
    /// charter. The Katarungang Pambarangay itself is RA 7160, Secs. 399 to
    /// 422; the barangay's own ordinance prices the filing.
    /// </summary>
    private FeeAssessment AssessLuponFiling() =>
        new()
        {
            BaseFee  = _luponFiling,
            FinalFee = _luponFiling,
            Basis    = "Katarungang Pambarangay filing fee (Citizen's Charter; RA 7160, Secs. 399–422)"
        };

    /// <summary>
    /// Barangay facilities at ₱200 per hour, charged by the hour or any part
    /// of an hour - a two-and-a-half-hour program is three hours.
    /// </summary>
    private FeeAssessment AssessFacility(RequestInput input)
    {
        if (input.Hours <= 0m)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason = "State the hours of use before filing — barangay facilities are charged at " +
                              DisplayFormat.Peso(_facilityHourly) + " per hour.",
                Basis = "Blocked — hours of use not stated"
            };

        decimal billable = Math.Ceiling(input.Hours);
        decimal fee = billable * _facilityHourly;

        return new FeeAssessment
        {
            BaseFee  = fee,
            FinalFee = fee,
            Basis    = $"Barangay facility use — {DisplayFormat.Hours(input.Hours)} hour(s) billed as {DisplayFormat.Hours(billable)} × " +
                       $"{DisplayFormat.Peso(_facilityHourly)} per hour (Citizen's Charter; RA 7160, Sec. 152)"
        };
    }

    /// <summary>
    /// The catch-all: any other processing fee the Barangay Taripa prices.
    /// The clerk assesses the amount and states the Taripa line, and both
    /// travel onto the receipt.
    /// </summary>
    private FeeAssessment AssessTarifa(RequestInput input)
    {
        if (input.Amount <= 0m)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason = "Enter the amount charged under the Barangay Taripa before filing.",
                Basis = "Blocked — Taripa amount not entered"
            };

        string detail = input.Detail.Trim();
        if (detail.Length == 0)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason = "State which Barangay Taripa item this fee is assessed under.",
                Basis = "Blocked — Taripa item not stated"
            };

        return new FeeAssessment
        {
            BaseFee  = input.Amount,
            FinalFee = input.Amount,
            Basis    = $"{detail} — {DisplayFormat.Peso(input.Amount)} (Barangay Taripa; Citizen's Charter; RA 7160, Sec. 152)"
        };
    }

    /// <summary>
    /// The charter rate for a document, before I apply any exemption.
    /// </summary>
    private decimal BaseFeeFor(DocumentType type, ClearanceScope scope) => type switch
    {
        // This is the one document the charter prices two ways.
        DocumentType.BarangayClearance =>
            scope == ClearanceScope.Abroad ? _clearanceAbroad : _clearanceLocal,

        // Everything else I treat as a standard certification.
        _ => _certification
    };

    private static string BasisFor(DocumentType type, ClearanceScope scope)
    {
        if (type == DocumentType.BarangayClearance)
            return scope == ClearanceScope.Abroad
                ? "Barangay Clearance — for employment abroad (Citizen's Charter)"
                : "Barangay Clearance — for local employment (Citizen's Charter)";

        return "Standard certification rate (Citizen's Charter)";
    }

    /// <summary>
    /// The assistance and social-service papers from the tarpaulin.
    ///
    /// I treat all of these as free. They exist so a resident can claim help,
    /// so charging for them would defeat the whole point of the service.
    /// </summary>
    private static bool IsAssistanceOrSocialService(DocumentType type) => type
        is DocumentType.MedicalAssistanceCertification
        or DocumentType.FinancialAssistanceCertification
        or DocumentType.BurialAssistanceCertification
        or DocumentType.IpScholarshipCertification
        or DocumentType.FourPsScholarshipCertification
        or DocumentType.SoloParentCertification
        or DocumentType.GadRelatedDocumentation
        or DocumentType.CsoDocumentation
        or DocumentType.BlotterRelatedIncident;

    private static FeeAssessment Free(string basis) =>
        new() { BaseFee = 0m, FinalFee = 0m, Basis = basis };

    private static FeeAssessment Exempt(decimal baseFee, string basis) =>
        new() { BaseFee = baseFee, FinalFee = 0m, Basis = basis, IsExempt = true };

    /// <summary>The readable name I show on screen and print on the page. I
    /// keep it here so one document is never called two different things in
    /// two different places.</summary>
    public static string NameOf(DocumentType type) => type switch
    {
        DocumentType.BarangayClearance               => "Barangay Clearance",
        DocumentType.CertificateOfResidency          => "Certificate of Residency",
        DocumentType.CertificateOfIndigency          => "Certificate of Indigency",
        DocumentType.BarangayBusinessClearance       => "Barangay Business Clearance",
        DocumentType.BarangayID                      => "Barangay ID",
        DocumentType.FirstTimeJobseekerCertificate   => "First-Time Jobseeker Certificate",
        DocumentType.CertificateOfGoodMoralCharacter => "Certificate of Good Moral Character",
        DocumentType.CertificateOfLowIncome          => "Certificate of Low Income",
        DocumentType.SoloParentCertification         => "Solo Parent Certification",
        DocumentType.MedicalAssistanceCertification  => "Medical Assistance Certification",
        DocumentType.FinancialAssistanceCertification=> "Financial Assistance Certification",
        DocumentType.BurialAssistanceCertification   => "Burial Assistance Certification",
        DocumentType.IpScholarshipCertification      => "IP Scholarship / Certification",
        DocumentType.FourPsScholarshipCertification  => "4Ps Scholarship / Certification",
        DocumentType.EmploymentCertification         => "Employment Certification",
        DocumentType.AcceptanceCertificate           => "Acceptance Certificate",
        DocumentType.GadRelatedDocumentation         => "GAD Related Documentation",
        DocumentType.BlotterRelatedIncident          => "Blotter Related Incident",
        DocumentType.CsoDocumentation                => "Civil Society Organization (CSO) Documentation",
        DocumentType.OtherCertification              => "Other Certification",
        DocumentType.CommunityTaxCertificate         => "Community Tax Certificate (Cedula)",
        DocumentType.LuponCaseFiling                 => "Katarungang Pambarangay Case Filing",
        DocumentType.BarangayFacilityRental          => "Barangay Facility Use / Rental",
        DocumentType.OtherTarifaProcessingFee        => "Other Processing Fee (Barangay Taripa)",
        _                                            => type.ToString()
    };
}
