using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules.DocumentTemplates;

public class BarangayIdTemplate : IDocumentTemplate
{
    public DocumentType DocumentType => DocumentType.BarangayID;
    public string Title => "BARANGAY IDENTIFICATION CARD";
    public string? Subtitle => "(Application Record)";

    public IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile)
    {
        var r = request.Resident;

        yield return $"Name            : {r.GetFullName().ToUpper()}";
        yield return $"Date of Birth   : {r.DateOfBirth:MMMM d, yyyy}";
        yield return $"Age             : {r.GetAge()}";
        yield return $"Gender          : {r.Gender}";
        yield return $"Civil Status    : {r.CivilStatus}";
        yield return $"Address         : {r.AddressLine}, {r.Purok}";
        yield return $"Contact No.     : {r.ContactNumber}";
        yield return $"Resident Since  : {r.DateOfResidency:MMMM d, yyyy}";
        yield return $"Classification  : {r.GetClassificationText()}";
        yield return "In case of emergency, please notify the Barangay Hall.";
    }
}
