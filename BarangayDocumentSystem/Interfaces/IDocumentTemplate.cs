// ---------------------------------------------------------------------------
//  IDocumentTemplate.cs - the wording of one document.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces
{
    /// <summary>
    /// Everything a template needs to write a document: the person, the
    /// request, and the barangay's own details. I pass one object rather than
    /// three arguments so that when I add something later (a signatory, say) I
    /// only change this class and not every template.
    /// </summary>
    public class DocumentContext
    {
        public Resident Resident { get; set; }
        public DocumentRequest Request { get; set; }
        public BarangayProfile Profile { get; set; }
        public IList<Dependent> Dependents { get; set; }

        public DocumentContext()
        {
            Dependents = new List<Dependent>();
        }

        /// <summary>How many people are in this resident's household, printed
        /// on the census-type certificates.</summary>
        public int HouseholdSize
        {
            get { return 1 + (Dependents == null ? 0 : Dependents.Count); }
        }
    }

    /// <summary>
    /// One document's wording.
    ///
    /// The renderer draws the letterhead, the margins, the signature block and
    /// the footer, once, for every document. A template only writes the
    /// sentences that belong to its own document. That split is why adding a
    /// new document type is one small class and one line in the registry,
    /// instead of another 400-line printer.
    /// </summary>
    public interface IDocumentTemplate
    {
        /// <summary>The document type this template writes.</summary>
        DocumentType SupportedType { get; }

        /// <summary>The heading in the middle of the page, e.g.
        /// "BARANGAY CLEARANCE".</summary>
        string GetTitle(DocumentContext context);

        /// <summary>The body, one line at a time. Blank strings are line
        /// breaks - the renderer keeps the spacing.</summary>
        IList<string> BuildBody(DocumentContext context);

        /// <summary>What goes above the signature: usually "Respectfully
        /// yours" or a short note about the purpose of the paper.</summary>
        string GetClosingLine(DocumentContext context);

        /// <summary>True when this document is only issued to a business, so
        /// the request form knows to ask for the business details.</summary>
        bool RequiresBusinessDetails { get; }
    }
}
