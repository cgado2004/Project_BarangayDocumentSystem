using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;

/// <summary>
/// The layout contract for one kind of barangay document.
///
/// v3.1 splits the document text from the engine that draws it. A template
/// knows WHAT a Certificate of Indigency says; the DocumentRenderer knows
/// HOW a page is laid out - letterhead, margins, wrapping, the signature
/// block, the print job. That way a new document type is one small class,
/// and a change to the barangay letterhead is one edit for every document
/// at once.
///
/// The contract is deliberately text-shaped: templates return logical
/// lines (an empty string is a paragraph break) and stay free of any
/// GDI+ dependency. The renderer measures and wraps them, on paper and in
/// the plain-text rendering used by the rule checks.
/// </summary>
public interface IDocumentTemplate
{
    /// <summary>The document this template is for. One template instance per
    /// value; the registry in DocumentRenderer maps them.</summary>
    DocumentType Type { get; }

    /// <summary>The title printed under the letterhead, for example
    /// "CERTIFICATE OF INDIGENCY".</summary>
    string Title { get; }

    /// <summary>An optional line under the title, for example "For Local
    /// Employment" on a clearance. It is per-request because two requests
    /// for the same document type can differ in what the paper must say.
    /// Null when there is none.</summary>
    string? SubtitleFor(DocumentRequest request);

    /// <summary>The body of the document as logical lines. An empty string
    /// marks a paragraph break; the renderer does the wrapping.</summary>
    IEnumerable<string> BodyLines(DocumentRequest request, BarangayProfile barangay);

    /// <summary>True when the document carries an oath the requester must
    /// sign, as RA 11261 requires of the first-time jobseeker.</summary>
    bool RequiresOath { get; }

    /// <summary>The lines of that oath, when RequiresOath is true.</summary>
    IEnumerable<string> OathLines(DocumentRequest request);
}
