// ---------------------------------------------------------------------------
//  DocumentTemplateRegistry.cs - which wording belongs to which document.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// The list of document wordings.
    ///
    /// Adding a new kind of paper to this system is two steps and neither of
    /// them touches the printer: write a class that inherits
    /// DocumentTemplateBase, and add one line to the list below. That is the
    /// whole reason the renderer and the wordings are separate objects.
    ///
    /// I also use this list to fill the "document type" drop-down on the
    /// request screen and the "document type" filter on the requests screen, so
    /// the screens, the printer and the database can never disagree about what
    /// documents exist.
    /// </summary>
    public class DocumentTemplateRegistry
    {
        private readonly Dictionary<DocumentType, IDocumentTemplate> _templates =
            new Dictionary<DocumentType, IDocumentTemplate>();

        private readonly List<DocumentType> _order = new List<DocumentType>();

        public DocumentTemplateRegistry()
        {
            Register(new BarangayClearanceTemplate());
            Register(new ResidencyCertificateTemplate());
            Register(new IndigencyTemplate());
            Register(new LowIncomeTemplate());
            Register(new GoodMoralTemplate());
            Register(new FirstTimeJobseekerTemplate());
            Register(new BusinessClearanceTemplate());
            Register(new BarangayIdTemplate());
            Register(new EmploymentCertificationTemplate());
            Register(new BlotterRecordTemplate());
            Register(new CommunityTaxTemplate());
            Register(new LuponFilingTemplate());
            Register(new FacilityRentalTemplate());
            Register(new TaripaFeeTemplate());

            foreach (IDocumentTemplate template in AssistanceCertificationTemplate.All())
                Register(template);
        }

        private static DocumentTemplateRegistry _default;

        /// <summary>
        /// The one list of wordings, for the screens that only need to ask a
        /// quick question - "does this document exist?", "does it need the
        /// business details?". The screens use this instead of building their
        /// own list every time a form opens, and they always get the same
        /// answer as the printer does.
        /// </summary>
        public static DocumentTemplateRegistry Default
        {
            get
            {
                if (_default == null) _default = new DocumentTemplateRegistry();
                return _default;
            }
        }

        public static bool HasTemplate(DocumentType type)
        {
            return Default.Has(type);
        }

        public static bool RequiresBusinessDetails(DocumentType type)
        {
            return Default.RequiresBusinessDetails(type);
        }

        public void Register(IDocumentTemplate template)
        {
            if (template == null) throw new ArgumentNullException("template");

            _templates[template.SupportedType] = template;
            if (!_order.Contains(template.SupportedType)) _order.Add(template.SupportedType);
        }

        /// <summary>The wording for a document, or null when there is none. The
        /// renderer writes a clear note on the page instead of crashing, so a
        /// missing template shows up as a blank paper with a sentence on it.</summary>
        public IDocumentTemplate Find(DocumentType type)
        {
            IDocumentTemplate template;
            return _templates.TryGetValue(type, out template) ? template : null;
        }

        public bool Has(DocumentType type)
        {
            return _templates.ContainsKey(type);
        }

        /// <summary>Every document the system can issue, in the order I want
        /// them listed - the seven core documents first, because those are what
        /// the barangay asked for, then the rest of the tarpaulin list.</summary>
        public IList<DocumentType> GetDocumentTypes()
        {
            return new List<DocumentType>(_order);
        }

        /// <summary>The names for a drop-down.</summary>
        public IList<string> GetDocumentNames()
        {
            List<string> names = new List<string>();
            foreach (DocumentType type in _order) names.Add(EnumText.Spaced(type.ToString()));
            return names;
        }

        /// <summary>The template that says it needs the business details, so
        /// the request form knows when to show that section.</summary>
        public bool RequiresBusinessDetails(DocumentType type)
        {
            IDocumentTemplate template = Find(type);
            return template != null && template.RequiresBusinessDetails;
        }

        /// <summary>True when every document type has wording. The rule checks
        /// run this, so that adding a document type to the enum without writing
        /// its sentences is caught before it reaches a resident.</summary>
        public bool CoversEveryDocumentType(out IList<string> missing)
        {
            missing = new List<string>();

            foreach (DocumentType type in (DocumentType[])Enum.GetValues(typeof(DocumentType)))
                if (!_templates.ContainsKey(type)) missing.Add(EnumText.Spaced(type.ToString()));

            return missing.Count == 0;
        }
    }
}
