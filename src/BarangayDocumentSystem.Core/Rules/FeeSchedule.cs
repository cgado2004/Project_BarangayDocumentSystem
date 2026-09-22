using BarangayDocumentSystem.Core.Entities;

namespace BarangayDocumentSystem.Core.Rules;

/// <summary>
/// What I get back when I assess a fee: the amount, and just as importantly
/// the reason for it. I return the reason because a resident can and will ask
/// why they are being charged, and "the computer said so" is not an answer I
/// am willing to give.
/// </summary>
public class FeeAssessment
{
    public decimal BaseFee { get; init; }
    public decimal FinalFee { get; init; }
    public string Basis { get; init; } = string.Empty;
    public bool IsWaived => FinalFee == 0;
    public bool IsExempt { get; init; }

    /// <summary>I set this when the law forbids issuing the document at all,
    /// for example a second RA 11261 certificate. When I set it, the screen
    /// must refuse to file the request rather than just pricing it.</summary>
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;
}

/// <summary>
/// Every peso I charge, and the legal reason behind it.
///
/// WHY I PUT IT ALL IN ONE CLASS
/// A barangay may only collect what its revenue ordinance allows. So when the
/// ordinance changes I want exactly one file to edit, not twenty forms each
/// carrying their own hard-coded number. This is also NFR-04 in the
/// documentation I wrote.
///
/// WHERE I GOT THESE AMOUNTS
/// I took them from the Barangay Citizen's Charter posted at the hall of
/// Barangay Magugpo Poblacion. They are the real rates, not figures I
/// invented. My earlier versions used ₱50 placeholders, and those were
/// simply wrong.
///
///   Barangay Clearance ....... ₱100 local employment / ₱200 abroad
///   Barangay Certification ... ₱100  (residency, good moral, other purpose)
///   Certificate of Indigency . FREE
///   Certificate of Low Income  FREE
///   Business Clearance ....... varies; ₱200 used as the standard rate
///   Lupon / filing a case .... ₱150
///
/// Anything the charter does not price separately I treat as a ₱100
/// certification, which is the charter's own catch-all "Other Purpose" rate.
/// </summary>
public class FeeSchedule
{
    // The charter's real numbers. I default them here and let App.config
    // override them, so my group-mates can correct a rate without rebuilding.
    private readonly decimal _clearanceLocal;
    private readonly decimal _clearanceAbroad;
    private readonly decimal _certification;
    private readonly decimal _businessRate;

    /// <summary>The months of residency RA 11261 requires before I may issue
    /// a first-time jobseeker certificate.</summary>
    public int JobseekerResidencyMonths { get; }

    /// <summary>
    /// I build the schedule with the posted Citizen's Charter rates.
    ///
    /// This is the constructor I use when nothing overrides the figures - it
    /// is what the in-memory demo runs on.
    /// </summary>
    public FeeSchedule() : this(100m, 200m, 100m, 200m, 6) { }

    /// <summary>
    /// I build the schedule from values supplied by App.config.
    ///
    /// I kept this separate from the default constructor so the Core project
    /// never has to know that a config file exists. The App layer reads the
    /// file and passes me plain numbers, which keeps Core free of any
    /// dependency on System.Configuration.
    /// </summary>
    public FeeSchedule(decimal clearanceLocal, decimal clearanceAbroad,
                       decimal certification, decimal businessRate,
                       int jobseekerResidencyMonths)
    {
        _clearanceLocal  = clearanceLocal;
        _clearanceAbroad = clearanceAbroad;
        _certification   = certification;
        _businessRate    = businessRate;
        JobseekerResidencyMonths = jobseekerResidencyMonths;
    }

    /// <summary>
    /// I work out what this resident pays for this document, and why.
    ///
    /// The order of the checks below matters a great deal, so I have numbered
    /// them. Getting them out of order would give away money the barangay is
    /// entitled to, or charge someone the law says is exempt.
    /// </summary>
    public FeeAssessment Assess(
        Resident resident,
        DocumentType type,
        ClearanceScope scope = ClearanceScope.Local)
    {
        ArgumentNullException.ThrowIfNull(resident);

        // ---- 1. Documents the charter gives away to everybody -------------
        // These are free no matter who is asking, so I settle them before I
        // look at any personal exemption.
        if (type == DocumentType.CertificateOfIndigency)
            return Free("FREE - Certificate of Indigency (DILG MC 2019-177)");

        if (type == DocumentType.CertificateOfLowIncome)
            return Free("FREE - Certificate of Low Income (Citizen's Charter)");

        if (IsAssistanceOrSocialService(type))
            return Free("FREE - social service documentation");

        // ---- 2. RA 11261, first-time jobseekers ---------------------------
        // I check this early because it can BLOCK the request outright rather
        // than merely change the price.
        if (type == DocumentType.FirstTimeJobseekerCertificate)
            return AssessJobseeker(resident);

        // ---- 3. Business clearance ----------------------------------------
        // I deliberately put this BEFORE the personal exemptions. A business
        // clearance is a regulatory fee on an enterprise, not on a person, so
        // being a senior citizen does not make your shop's permit free. If I
        // had written this check after the exemptions I would be giving away
        // business permits, which is the kind of mistake an auditor finds.
        if (type == DocumentType.BarangayBusinessClearance)
            return new FeeAssessment
            {
                BaseFee  = _businessRate,
                FinalFee = _businessRate,
                Basis    = "Business clearance - personal exemptions do not apply"
            };

        // ---- 4. Personal statutory exemptions ------------------------------
        // Only now, after the business clearance is out of the way, do I let a
        // personal exemption waive the fee.
        decimal baseFee = BaseFeeFor(type, scope);

        if (resident.HasClassification(ResidentClassification.SeniorCitizen))
            return Exempt(baseFee, "FREE - Senior Citizen (RA 9994)");

        if (resident.HasClassification(ResidentClassification.PWD))
            return Exempt(baseFee, "FREE - Person With Disability (RA 10754)");

        if (resident.HasClassification(ResidentClassification.Indigent))
            return Exempt(baseFee, "FREE - Indigent resident");

        // ---- 5. Ordinary rate ----------------------------------------------
        // Nothing exempted this resident, so I charge the charter rate.
        return new FeeAssessment
        {
            BaseFee  = baseFee,
            FinalFee = baseFee,
            Basis    = BasisFor(type, scope)
        };
    }

    /// <summary>
    /// RA 11261 gives a first-time jobseeker their documents free, but only
    /// under conditions, and only once in their life. So here I either clear
    /// the request at zero or refuse it with a reason I can show the resident.
    /// </summary>
    private FeeAssessment AssessJobseeker(Resident resident)
    {
        if (resident.HasAvailedFirstTimeJobseeker)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason =
                    "RA 11261 may be availed only ONCE. This resident has already "
                  + "been issued a First-Time Jobseeker Certificate.",
                Basis = "Blocked - RA 11261 already availed"
            };

        int months = resident.GetMonthsOfResidency();
        if (months < JobseekerResidencyMonths)
            return new FeeAssessment
            {
                IsBlocked   = true,
                BlockReason =
                    $"RA 11261 requires at least {JobseekerResidencyMonths} months of "
                  + $"residency in the barangay. This resident has {months}.",
                Basis = "Blocked - insufficient residency"
            };

        return Free("FREE - RA 11261 (First Time Jobseekers Assistance Act)");
    }

    /// <summary>
    /// The charter rate for a document, before I apply any exemption.
    /// </summary>
    private decimal BaseFeeFor(DocumentType type, ClearanceScope scope) => type switch
    {
        // This is the one document I have to price two ways.
        DocumentType.BarangayClearance =>
            scope == ClearanceScope.Abroad ? _clearanceAbroad : _clearanceLocal,

        DocumentType.BarangayBusinessClearance => _businessRate,

        // Everything else I treat as a standard certification.
        _ => _certification
    };

    private static string BasisFor(DocumentType type, ClearanceScope scope)
    {
        if (type == DocumentType.BarangayClearance)
            return scope == ClearanceScope.Abroad
                ? "Barangay Clearance - for employment abroad (Citizen's Charter)"
                : "Barangay Clearance - for local employment (Citizen's Charter)";

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
        _                                            => type.ToString()
    };
}
