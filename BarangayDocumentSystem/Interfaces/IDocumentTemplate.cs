using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// Produces the body text of one kind of barangay document.
///
/// ── OPEN/CLOSED PRINCIPLE ───────────────────────────────────────────────
/// Before: DocumentPrinter was a 310-line class with a switch over every
/// DocumentType and one private Append* method per document. Adding a new
/// certificate meant EDITING that class — reopening working, tested code and
/// risking the seven documents that already worked.
///
/// Now each document is its own small class implementing this interface, and
/// the printer discovers them from a registry. Adding a certificate means
/// ADDING a file and one registration line. The printer itself never changes.
///
/// Open for extension, closed for modification.
///
/// ── SINGLE RESPONSIBILITY ───────────────────────────────────────────────
/// Each template knows the wording of exactly one document. When the Punong
/// Barangay wants the indigency wording changed, there is precisely one file
/// to open, and nothing else can break.
/// </summary>
public interface IDocumentTemplate
{
    /// <summary>Which document this template renders.</summary>
    DocumentType DocumentType { get; }

    /// <summary>Heading printed under the letterhead, e.g. "BARANGAY CLEARANCE".</summary>
    string Title { get; }

    /// <summary>
    /// Optional second heading line, e.g. "(First Time Jobseeker)".
    ///
    /// Not a default-implemented property: .NET Framework's CLR (unlike
    /// .NET Core 3.0+) cannot execute default interface members, so every
    /// template must implement this itself. Five of the seven just return
    /// null — see any of them for the one-line pattern.
    /// </summary>
    string? Subtitle { get; }

    /// <summary>
    /// The body paragraphs. Returned as separate strings so the renderer owns
    /// wrapping and centring — the template only decides WHAT is said, never
    /// HOW it is laid out. That separation is why changing the page width does
    /// not touch any template.
    /// </summary>
    IEnumerable<string> BuildBody(DocumentRequest request, BarangayProfile profile);
}

/// <summary>
/// Identity of the issuing barangay.
///
/// ── DRY ─────────────────────────────────────────────────────────────────
/// Before, "BARANGAY MAGUGPO POBLACION" and "CITY OF TAGUM" were repeated as
/// literals across seven Append* methods. Changing the Punong Barangay's name
/// meant hunting through 310 lines. One object now, injected once.
/// </summary>
public record BarangayProfile(
    string BarangayName,
    string CityName,
    string ProvinceName,
    string PunongBarangay)
{
    /// <summary>Defaults for this deployment.</summary>
    public static BarangayProfile MagugpoPoblacion => new(
        "BARANGAY MAGUGPO POBLACION",
        "CITY OF TAGUM",
        "PROVINCE OF DAVAO DEL NORTE",
        "HON. [PUNONG BARANGAY NAME]");
}
