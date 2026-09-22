using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class DocumentRenderer
    {
        private readonly BarangayProfile profile;
        private readonly List<IDocumentTemplate> templates;
        public IReadOnlyList<IDocumentTemplate> Templates { get { return templates.AsReadOnly(); } }

        public DocumentRenderer(BarangayProfile profile, IEnumerable<IDocumentTemplate> templates)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            this.templates = templates.ToList();
            if (this.templates.GroupBy(template => template.DocumentType).Any(group => group.Count() > 1))
                throw new ArgumentException("Each document type must have only one registered template.");
        }

        public IDocumentTemplate GetTemplate(DocumentType type)
        {
            var template = templates.FirstOrDefault(item => item.DocumentType == type);
            if (template == null) throw new ArgumentException("No template is registered for this document type.");
            return template;
        }

        public string Render(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!string.IsNullOrEmpty(request.ReleasedDocumentText)) return request.ReleasedDocumentText;
            var template = GetTemplate(request.DocumentType);
            var text = new StringBuilder();
            text.AppendLine("REPUBLIC OF THE PHILIPPINES");
            text.AppendLine(profile.ProvinceName.ToUpperInvariant());
            text.AppendLine(profile.CityName.ToUpperInvariant());
            text.AppendLine(profile.BarangayName.ToUpperInvariant());
            text.AppendLine("OFFICE OF THE PUNONG BARANGAY");
            text.AppendLine();
            text.AppendLine(template.Title.ToUpperInvariant());
            text.AppendLine();
            if (request.Status != RequestStatus.Released)
            {
                text.AppendLine("DRAFT - NOT VALID FOR RELEASE");
                text.AppendLine();
            }
            string body = template.BuildBody(request).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            text.AppendLine(body);
            text.AppendLine();
            text.AppendLine("Purpose: " + request.Purpose);
            text.AppendLine("Issued on: " + (request.DateReleased ?? DateTime.Today).ToString("MMMM d, yyyy"));
            text.AppendLine();
            text.AppendLine(profile.PunongBarangay);
            text.AppendLine("Punong Barangay");
            text.AppendLine();
            text.AppendLine("Reference: " + request.ReferenceNumber);
            text.AppendLine("Fee: PHP " + request.Fee.ToString("N2"));
            text.AppendLine("Basis: " + request.FeeBasis);
            text.AppendLine("Official receipt: " + (request.IsPaid ? request.OfficialReceiptNumber : "Not applicable / unpaid"));
            text.AppendLine();
            text.AppendLine("Classroom sample. Requires authorized review before official use.");
            return text.ToString();
        }
    }
}
