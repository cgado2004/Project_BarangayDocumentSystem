// =====================================================================
//  PART:    Models - document types, statuses, scope and the classification flags
//  ORIGIN:  the group's shared design - first modelled in Draft - Jonathan F. Del Rosario,
//           given this place in the tree by Fdraft - Frent Dhieniel Raborar;
//           the code and comments in this file are my v3.1 rewrite (leader_draft - Clint Wood Gado)
//  EDITS:   Clint Wood Gado - the full tarpaulin document list and the v3.1 money documents, header
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace BarangayDocumentSystem.Models;

/// <summary>
/// Every service the barangay issues a paper for.
///
/// I copied the first twenty straight off the tarpaulin posted at the
/// barangay hall, so they match what the staff actually hand out. v3.1
/// appends the four services the Citizen's Charter prices but the v3
/// tarpaulin list did not carry: the cedula, the Katarungang Pambarangay
/// filing fee, barangay facility rental, and the catch-all "other
/// processing fee under the Barangay Taripa".
///
/// A warning to whoever edits this next: do NOT reorder or renumber these
/// values. The database stores the NAME of each one and my SQL ENUM lists
/// them in this exact order. Shuffling them here would silently change
/// what existing rows mean. New values go at the end.
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
    OtherCertification,

    // ---- the v3.1 additions from the Citizen's Charter fee schedule ----
    /// <summary>The community tax certificate. Amount varies: computed
    /// under RA 7160 Sec. 156 from the declarant's sworn gross income.</summary>
    CommunityTaxCertificate,

    /// <summary>Filing a case with the Lupong Tagapamayapa. ₱150 flat.</summary>
    LuponCaseFiling,

    /// <summary>Use of a barangay facility. ₱200 per hour.</summary>
    BarangayFacilityRental,

    /// <summary>Any other processing fee priced by the Barangay Taripa.
    /// The clerk assesses the amount and states the Taripa line.</summary>
    OtherTarifaProcessingFee
}

/// <summary>
/// Where a Barangay Clearance is going to be used.
///
/// I added this because the Citizen's Charter prices ONE document two
/// ways: a Barangay Clearance is ₱100 for local employment but ₱200 if it
/// is for work abroad. Without it I cannot charge what the charter
/// actually says.
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
/// I made this a [Flags] enum because one person can be several of these
/// at once - a senior citizen who is also a PWD is very common. Each value
/// is a separate bit, so I can combine them in a single field.
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

/// <summary>
/// The puroks of Barangay Magugpo Poblacion.
///
/// I took these from the barangay's own "List of Projects Chargeable
/// Against the 20% Development Fund - FY 2025", so they are the real names
/// rather than the "Purok 1, Purok 2, ..." placeholders my earlier
/// versions invented. The v3.1 structure keeps the list in Enums.cs, next
/// to the other value sets the domain speaks in.
/// </summary>
public static class Puroks
{
    public static readonly string[] All =
    {
        "Purok Arellano",
        "Purok Calachuchi",
        "Purok Cristo Rey",
        "Purok Dagohoy",
        "Purok Lapu-Lapu",
        "Purok Marilag 2",
        "Purok Orchids",
        "Purok Paraiso",
        "Purok Sampaguita",
        "Purok Sulgreg",
        "Purok Sunflower",
        "Purok Talisay",
        "Purok Tandang Sora",
        "Purok Tindalo"
    };

    /// <summary>True when the text is one of the real puroks. I use this in
    /// validation so a typo cannot create a purok that does not exist.</summary>
    public static bool Contains(string purok) =>
        All.Contains(purok, StringComparer.OrdinalIgnoreCase);
}
