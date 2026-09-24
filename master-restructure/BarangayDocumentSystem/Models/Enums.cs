namespace BarangayDocumentSystem.Domain.Entities;

/// document type the residents can choose from
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


/// Status of the request
public enum RequestStatus
{
    Pending,
    Processing,
    ReadyForRelease,
    Released,
    Rejected
}


/// Residents classifications 
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
