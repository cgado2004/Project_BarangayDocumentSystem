using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services;

/// <summary>Outcome of assessing a fee, including WHY it came out that way.</summary>
public class FeeAssessment
{
    public decimal BaseFee { get; init; }
    public decimal FinalFee { get; init; }
    public string Basis { get; init; } = string.Empty;
    public bool IsWaived => FinalFee == 0;
    public bool IsExempt { get; init; }
}

/// <summary>
/// Fee rules for Barangay Magugpo Poblacion.
///
/// ─────────────────────────────────────────────────────────────────────────
///  ⚠️ THE AMOUNTS BELOW ARE PLACEHOLDERS
/// ─────────────────────────────────────────────────────────────────────────
/// Under the Local Government Code (RA 7160, secs. 152–186) a barangay may
/// only collect a fee that is fixed by a duly enacted barangay revenue
/// ordinance. Collecting without an ordinance is illegal exaction.
///
/// The figures here are typical Philippine ranges used so the program runs.
/// REPLACE THEM with the actual ordinance rates for Magugpo Poblacion before
/// this is used for anything real — they are all in one place for exactly
/// that reason.
///
/// ─────────────────────────────────────────────────────────────────────────
///  STATUTORY EXEMPTIONS ENCODED HERE
/// ─────────────────────────────────────────────────────────────────────────
/// • RA 11261 (First Time Jobseekers Assistance Act) — barangay clearance and
///   certification are FREE for a qualified first-time jobseeker. Conditions:
///   Filipino citizen, at least 6 months' residency in the issuing barangay,
///   and the benefit may be availed ONCE only.
///
/// • Certificate of Indigency — DILG has opined that charging more than ₱50
///   "appears excessive", and ARTA treats refusal-to-issue-without-payment as
///   a grave offence under RA 11032. Issued free here.
///
/// • RA 9994 (senior citizens) and RA 10754 (PWD) grant broad privileges;
///   many barangays waive document fees for both. Implemented as a waiver,
///   but CONFIRM against the local ordinance.
/// </summary>
public class FeeSchedule
{
    // --- Base rates (placeholders — see class remarks) ---
    public const decimal ClearanceFee        = 50m;
    public const decimal ResidencyFee        = 50m;
    public const decimal IndigencyFee        = 0m;     // free by policy
    public const decimal BusinessClearanceFee = 200m;
    public const decimal BarangayIdFee       = 100m;
    public const decimal GoodMoralFee        = 50m;
    public const decimal JobseekerFee        = 0m;     // free by RA 11261

    /// <summary>Minimum residency for the RA 11261 certificate.</summary>
    public const int RequiredMonthsForJobseeker = 6;

    /// <summary>
    /// Works out what a resident should pay for a document, and records the
    /// legal or policy basis for the answer.
    /// </summary>
    public FeeAssessment Assess(Resident resident, DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(resident);

        decimal baseFee = GetBaseFee(documentType);

        // --- Exemption 1: RA 11261, first-time jobseeker ---
        if (documentType == DocumentType.FirstTimeJobseekerCertificate)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — RA 11261 (First Time Jobseekers Assistance Act)",
                IsExempt = true
            };
        }

        // --- Exemption 2: certificate of indigency ---
        if (documentType == DocumentType.CertificateOfIndigency)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Certificate of Indigency (DILG MC 2019-177)",
                IsExempt = true
            };
        }

        // Business clearance is a regulatory fee on an enterprise, not a
        // personal document, so personal exemptions do NOT apply to it.
        if (documentType == DocumentType.BarangayBusinessClearance)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = baseFee,
                Basis    = "Business clearance — personal exemptions do not apply",
                IsExempt = false
            };
        }

        // --- Exemption 3: indigent resident ---
        if (resident.HasClassification(ResidentClassification.Indigent))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — indigent resident",
                IsExempt = true
            };
        }

        // --- Exemption 4: senior citizen (RA 9994) ---
        if (resident.HasClassification(ResidentClassification.SeniorCitizen))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Senior Citizen (RA 9994)",
                IsExempt = true
            };
        }

        // --- Exemption 5: PWD (RA 10754) ---
        if (resident.HasClassification(ResidentClassification.PWD))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Person With Disability (RA 10754)",
                IsExempt = true
            };
        }

        // --- No exemption ---
        return new FeeAssessment
        {
            BaseFee  = baseFee,
            FinalFee = baseFee,
            Basis    = "Standard rate per barangay revenue ordinance",
            IsExempt = false
        };
    }

    public decimal GetBaseFee(DocumentType type) => type switch
    {
        DocumentType.BarangayClearance             => ClearanceFee,
        DocumentType.CertificateOfResidency        => ResidencyFee,
        DocumentType.CertificateOfIndigency        => IndigencyFee,
        DocumentType.BarangayBusinessClearance     => BusinessClearanceFee,
        DocumentType.BarangayID                    => BarangayIdFee,
        DocumentType.FirstTimeJobseekerCertificate => JobseekerFee,
        DocumentType.CertificateOfGoodMoralCharacter => GoodMoralFee,
        _                                          => 0m
    };

    /// <summary>
    /// Checks the RA 11261 preconditions. Returns false plus a reason when the
    /// resident does not qualify, so the clerk can explain why.
    /// </summary>
    public bool CanIssueJobseekerCertificate(Resident resident, out string reason)
    {
        ArgumentNullException.ThrowIfNull(resident);

        if (resident.HasAvailedFirstTimeJobseeker)
        {
            reason = "This resident has already availed of the RA 11261 benefit. " +
                     "The law allows it only once.";
            return false;
        }

        int months = resident.GetMonthsOfResidency();
        if (months < RequiredMonthsForJobseeker)
        {
            reason = $"RA 11261 requires at least {RequiredMonthsForJobseeker} months' " +
                     $"residency in the barangay. This resident has {months} month(s).";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
