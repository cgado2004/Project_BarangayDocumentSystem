using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Service.Templates;

/// <summary>
/// The shared shape of every document template.
///
/// Each template answers ONE question - what does this document SAY - and
/// the renderer handles everything about how a page looks. The base class
/// builds the standard certification paragraph around whatever the derived
/// template declares, so most documents are a handful of lines.
///
/// BodyLines returns logical lines: an empty string is a paragraph break,
/// and the renderer word-wraps everything. Templates never measure text
/// and never touch GDI+.
/// </summary>
public abstract class DocumentTemplateBase : IDocumentTemplate
{
    protected DocumentTemplateBase(DocumentType type) => Type = type;

    public DocumentType Type { get; }

    public virtual string Title => FeeSchedule.NameOf(Type);

    public virtual string? SubtitleFor(DocumentRequest request) => null;

    public virtual bool RequiresOath => false;

    public virtual IEnumerable<string> OathLines(DocumentRequest request) =>
        Array.Empty<string>();

    public virtual IEnumerable<string> BodyLines(DocumentRequest request, BarangayProfile barangay)
    {
        yield return "TO WHOM IT MAY CONCERN:";
        yield return "";

        foreach (string line in Describe(request, barangay))
            yield return line;

        yield return "";
        yield return $"    This {Noun} is issued upon the request of the above-named person " +
                     $"for {request.Purpose}.";
    }

    /// <summary>The word the closing sentence uses: "certification",
    /// "clearance", "certificate".</summary>
    protected virtual string Noun => "certification";

    /// <summary>
    /// The sentences between "TO WHOM IT MAY CONCERN" and the closing
    /// paragraph. One sentence per line; an empty string for a break.
    /// </summary>
    protected abstract IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay);

    // -----------------------------------------------------------------
    //  Phrase helpers every template leans on. One place for the words
    //  means every certificate agrees about who the resident is.
    // -----------------------------------------------------------------

    /// <summary>"JUAN P. DELA CRUZ" - the way a name is printed on an
    /// official document.</summary>
    protected static string FormalName(Resident r) => r.GetFullName().ToUpperInvariant();

    /// <summary>"…, 41 years of age, Filipino, and a bona fide resident of
    /// Purok Tandang Sora, Barangay Magugpo Poblacion…". The opening every
    /// certification begins with.</summary>
    protected static string ResidentIntroduction(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;
        return $"{FormalName(r)}, {r.GetAge()} years of age, is a bona fide resident " +
               $"of {r.Purok}, {barangay.BarangayName}, {barangay.CityName}.";
    }
}

/// <summary>
/// The template of last resort: a plain certification of residency and good
/// standing. I register it for every document type that has no wording of
/// its own - Barangay ID, GAD documentation, CSO documentation, the other
/// certifications - so no document type can ever render as a blank page.
/// </summary>
public sealed class GenericCertificationTemplate : DocumentTemplateBase
{
    public GenericCertificationTemplate(DocumentType type) : base(type) { }

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    As such, {request.Resident.GetFullName()} is known to this office " +
                     "to be of good standing and has no derogatory record on file herein.";
    }
}
