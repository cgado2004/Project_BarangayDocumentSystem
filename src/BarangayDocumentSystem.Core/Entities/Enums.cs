namespace BarangayDocumentSystem.Core.Entities;

/// <summary>
/// Every service the barangay issues a paper for.
///
/// I copied this list straight off the tarpaulin posted at the barangay hall,
/// so it matches what the staff actually hand out rather than what I imagined
/// they hand out. The first seven are the core documents our specification
/// originally listed; the rest are the other certifications on the same board.
///
/// A warning to whoever edits this next: do NOT reorder these values. The
/// database stores the NAME of each one, and my SQL ENUM lists them in this
/// exact order. Shuffling them here would silently change what existing rows
/// mean.
/// </summary>
public enum DocumentType
{
    // ---- the seven core documents ----
    BarangayClearance,
    CertificateOfResidency,
    CertificateOfIndigency,
    BarangayBusinessClearance,
    BarangayID,
    FirstTimeJobseekerCertificate,
    CertificateOfGoodMoralCharacter,

    // ---- the rest of the tarpaulin ----
    CertificateOfLowIncome,
    SoloParentCertification,
    MedicalAssistanceCertification,
    FinancialAssistanceCertification,
    BurialAssistanceCertification,
    IpScholarshipCertification,
    FourPsScholarshipCertification,
    EmploymentCertification,
    AcceptanceCertificate,
    GadRelatedDocumentation,
    BlotterRelatedIncident,
    CsoDocumentation,
    OtherCertification
}

/// <summary>
/// Where the clearance is going to be used.
///
/// I added this because the Citizen's Charter prices ONE document two ways: a
/// Barangay Clearance is ₱100 for local employment but ₱200 if it is for work
/// abroad. Without it I cannot charge what the charter actually says.
/// </summary>
public enum ClearanceScope
{
    Local,
    Abroad
}

public enum RequestStatus
{
    Pending,
    Processing,
    ReadyForRelease,
    Released,
    Rejected
}

public enum CivilStatus
{
    Single,
    Married,
    Widowed,
    Separated,
    Divorced
}

public enum Gender
{
    Male,
    Female
}

/// <summary>
/// The tags that decide whether a resident pays.
///
/// I made this a [Flags] enum because one person can be several of these at
/// once - a senior citizen who is also a PWD is very common. Each value is a
/// separate bit, so I can combine them in a single field.
/// </summary>
[Flags]
public enum ResidentClassification
{
    None          = 0,
    SeniorCitizen = 1,
    PWD           = 2,
    Indigent      = 4,
    Student       = 8,
    SoloParent    = 16
}
