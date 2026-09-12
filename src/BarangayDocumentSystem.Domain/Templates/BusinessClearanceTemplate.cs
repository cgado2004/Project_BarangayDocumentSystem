using BarangayDocumentSystem.Domain.Abstractions;
using BarangayDocumentSystem.Domain.Entities;

namespace BarangayDocumentSystem.Domain.Templates;

public class BusinessClearanceTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.BarangayBusinessClearance;
    public string Title => "BARANGAY BUSINESS CLEARANCE";

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return "TO WHOM IT MAY CONCERN:";
        yield return $"This is to certify that the business establishment described below, owned " +
                     $"and operated by {r.GetFullName().ToUpper()} of {r.Purok}, " +
                     $"{profile.BarangayName}, {profile.CityName}, has been granted clearance to " +
                     $"operate within the territorial jurisdiction of this Barangay.";
        yield return $"Business / Purpose : {request.Purpose}";
        yield return $"Location           : {r.AddressLine}, {r.Purok}";
        yield return "This clearance is issued subject to compliance with existing barangay " +
                     "ordinances and is valid until 31 December of the current year.";
    }
}
