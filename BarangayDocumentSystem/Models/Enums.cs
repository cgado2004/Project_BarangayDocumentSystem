using System;
namespace BarangayDocumentSystem.Models;

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

public enum RequestStatus
{
    Pending,
    Processing,
    ReadyForRelease,
    Released,
    Rejected
}

[Flags]
public enum ResidentClassification
{
    None = 0,
    SeniorCitizen = 1 << 0,
    PWD = 1 << 1,
    Indigent = 1 << 2,
    Student = 1 << 3,
    SoloParent = 1 << 4
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
    Female,
    Other
}