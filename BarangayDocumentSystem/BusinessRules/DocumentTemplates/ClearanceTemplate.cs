using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

/// <summary>Barangay Clearance. One document, one class (SRP).</summary>
public class ClearanceTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.BarangayClearance;
    public string Title => "BARANGAY CLEARANCE";
    public string? Subtitle => null;

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that {r.GetFullName().ToUpper()}, {r.GetAge()} years " +
                     $"old, {r.CivilStatus.ToString().ToLower()}, Filipino citizen, and a bona " +
                     $"fide resident of {r.Purok}, {profile.BarangayName}, {profile.CityName}, " +
                     $"is known to be of good moral character and law-abiding citizen in the " +
                     $"community.";
        yield return "This further certifies that the above-named person has no derogatory " +
                     "record nor pending case filed before this Barangay.";
        yield return $"This certification is issued upon the request of the above-named person " +
                     $"for {request.Purpose.ToUpper()}.";
    }
}
