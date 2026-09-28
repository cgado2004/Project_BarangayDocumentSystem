using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

public class GoodMoralTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.CertificateOfGoodMoralCharacter;
    public string Title => "CERTIFICATE OF GOOD MORAL CHARACTER";
    public string? Subtitle => null;

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that {r.GetFullName().ToUpper()}, {r.GetAge()} years " +
                     $"old, a bona fide resident of {r.Purok}, {profile.BarangayName}, " +
                     $"{profile.CityName}, is a person of GOOD MORAL CHARACTER and has not been " +
                     $"involved in any anomalous or illegal activity within this barangay.";
        yield return $"Issued upon request for {request.Purpose.ToUpper()}.";
    }
}
