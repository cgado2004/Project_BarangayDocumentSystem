namespace BarangayDocumentSystem.Domain.Entities;

/// <summary>Type of document a resident may request from the barangay.</summary>
public enum DocumentType
{
    BarangayClearance,
    CertificateOfResidency,
    CertificateOfIndigency,
    BarangayBusinessClearance,
    BarangayID,
    FirstTimeJobseekerCertificate,
    CertificateOfGoodMoralCharacter
}

/// <summary>
/// Where a request currently sits. Requests move forward through this
/// sequence; Released and Rejected are terminal.
/// </summary>
public enum RequestStatus
{
    Pending,
    Processing,
    ReadyForRelease,
    Released,
    Rejected
}

/// <summary>
/// Special classifications that affect fees under national law or local
/// ordinance. A resident may hold more than one, so this is a [Flags] enum.
/// </summary>
[Flags]
public enum ResidentClassification
{
    None          = 0,
    SeniorCitizen = 1 << 0,   // RA 9994
    PWD           = 1 << 1,   // RA 10754
    Indigent      = 1 << 2,   // certified low-income
    Student       = 1 << 3,
    SoloParent    = 1 << 4    // RA 8972 / RA 11861
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
