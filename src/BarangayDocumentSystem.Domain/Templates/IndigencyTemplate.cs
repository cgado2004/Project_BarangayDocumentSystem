using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Entities;

namespace BarangayDocumentSystem.Domain.Templates;

public class IndigencyTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.CertificateOfIndigency;
    public string Title => "CERTIFICATE OF INDIGENCY";

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that {r.GetFullName().ToUpper()}, {r.GetAge()} years " +
                     $"old, {r.CivilStatus.ToString().ToLower()}, and a bona fide resident of " +
                     $"{r.Purok}, {profile.BarangayName}, {profile.CityName}, belongs to an " +
                     $"INDIGENT FAMILY in this barangay.";
        yield return "The above-named person has no sufficient means of livelihood to support " +
                     "the basic necessities of the family.";
        yield return $"This certification is issued FREE OF CHARGE upon the request of the " +
                     $"above-named person for {request.Purpose.ToUpper()}.";
    }
}
