using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;





















public interface IDocumentTemplate
{

    DocumentType DocumentType { get; }


    string Title { get; }









    string? Subtitle { get; }







    IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile);
}









public record BarangayProfile(
    string BarangayName,
    string CityName,
    string ProvinceName,
    string PunongBarangay)
{

    public static BarangayProfile MagugpoPoblacion => new(
        "BARANGAY MAGUGPO POBLACION",
        "CITY OF TAGUM",
        "PROVINCE OF DAVAO DEL NORTE",
        "HON. [PUNONG BARANGAY NAME]");
}
