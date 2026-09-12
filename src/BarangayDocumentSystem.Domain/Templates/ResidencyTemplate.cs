using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Entities;

namespace BarangayDocumentSystem.Domain.Templates;

public class ResidencyTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.CertificateOfResidency;
    public string Title => "CERTIFICATE OF RESIDENCY";

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that {r.GetFullName().ToUpper()}, {r.GetAge()} years " +
                     $"old, is a bona fide resident of {r.AddressLine}, {r.Purok}, " +
                     $"{profile.BarangayName}, {profile.CityName}, Davao del Norte.";
        yield return $"Records of this office show that the above-named person has been residing " +
                     $"in this barangay since {r.DateOfResidency:MMMM d, yyyy} " +
                     $"({r.GetMonthsOfResidency()} months).";
        yield return $"Issued this day upon the request of the above-named person for " +
                     $"{request.Purpose.ToUpper()}.";
    }
}
