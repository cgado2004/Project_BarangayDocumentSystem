// ---------------------------------------------------------------------------
//  EmploymentAndAssistanceTemplates.cs - the jobseeker paper, the assistance
//  certifications and the barangay ID.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// The First-Time Jobseeker Certificate.
    ///
    /// This is the one document with an extra piece attached: the Oath of
    /// Undertaking. NBI, PSA and BIR look for that oath on this certificate, so
    /// leaving it out would make the paper useless to the person holding it.
    /// The law behind it is RA 11261, which may be availed ONCE only and needs
    /// at least six months of residence in the barangay - both of which this
    /// program enforces before the certificate can even be filed.
    /// </summary>
    public class FirstTimeJobseekerTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.FirstTimeJobseekerCertificate; } }

        public override string GetTitle(DocumentContext context)
        {
            return "FIRST-TIME JOBSEEKER CERTIFICATE";
        }

        public override string GetClosingLine(DocumentContext context)
        {
            return "Respectfully yours,";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + CivilStatusPhrase(resident) + ", " + ResidencySentence(resident, context)
                   + ", is a FIRST-TIME JOBSEEKER and is qualified under Republic Act No. 11261, "
                   + "otherwise known as the First Time Jobseekers Assistance Act.");
            body.Add(Blank);
            body.Add(ResidencyLengthLine(resident));
            body.Add("The resident has not previously availed of the benefits under the said Act.");
            body.Add(Blank);
            body.Add("In view thereof, the above-named is exempt from paying the fees for the "
                   + "documents required for first-time employment, including this barangay "
                   + "clearance and certification.");
            body.Add(Blank);
            body.Add("------------------------------------------------------------");
            body.Add("OATH OF UNDERTAKING");
            body.Add(Blank);
            body.Add("I, " + NameInCapitals(resident) + ", of legal age, a resident of " + Purok(resident)
                   + ", " + (context.Profile != null ? context.Profile.BarangayName : string.Empty)
                   + ", do hereby declare that I am a first-time jobseeker and that I have not "
                   + "previously availed of any benefit under RA 11261.");
            body.Add(Blank);
            body.Add("I hereby undertake that the documents issued to me under this Act shall be used "
                   + "solely for the purpose of seeking first-time employment, and that I shall be "
                   + "liable for any consequences should this declaration be found false.");
            body.Add(Blank);
            body.Add("JURAT: Sworn before me this " + IssuedLine(context).Replace("Issued this ", string.Empty));
            body.Add(Blank);
            body.Add("Purpose of request: " + (context.Request == null ? string.Empty : context.Request.Purpose));

            return body;
        }
    }

    /// <summary>The Employment Certification - a short paper confirming that
    /// somebody lives here and is known to the barangay, for a job application
    /// or for the Department of Labor.</summary>
    public class EmploymentCertificationTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.EmploymentCertification; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATION OF EMPLOYMENT SUPPORT";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context) + ", is known to this barangay.");
            body.Add(Blank);

            if (!string.IsNullOrWhiteSpace(resident.Occupation))
                body.Add("The resident is engaged in " + resident.Occupation.Trim() + " as a means of livelihood.");

            body.Add(ResidencyLengthLine(resident));
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>The certifications the barangay issues for the assistance
    /// programmes: medical, financial, burial, the scholarship lists and the
    /// solo parent certification. They share one wording with a different
    /// heading, which is exactly how the barangay writes them.</summary>
    public class AssistanceCertificationTemplate : DocumentTemplateBase
    {
        private readonly DocumentType _type;
        private readonly string _title;
        private readonly string _sentence;

        public AssistanceCertificationTemplate(DocumentType type, string title, string sentence)
        {
            _type = type;
            _title = title;
            _sentence = sentence;
        }

        public override DocumentType SupportedType { get { return _type; } }

        public override string GetTitle(DocumentContext context)
        {
            return _title;
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context) + ", " + _sentence);
            body.Add(Blank);
            body.Add(HouseholdLine(context));
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        /// <summary>The set of certifications that share this wording, so
        /// adding one is a single line rather than another class.</summary>
        public static IEnumerable<IDocumentTemplate> All()
        {
            List<IDocumentTemplate> templates = new List<IDocumentTemplate>();

            templates.Add(new AssistanceCertificationTemplate(DocumentType.SoloParentCertification,
                "SOLO PARENT CERTIFICATION",
                "is a solo parent and is registered as such with this barangay."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.MedicalAssistanceCertification,
                "CERTIFICATION FOR MEDICAL ASSISTANCE",
                "is known to this barangay as a resident in need of medical assistance."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.FinancialAssistanceCertification,
                "CERTIFICATION FOR FINANCIAL ASSISTANCE",
                "is known to this barangay as a resident in need of financial assistance."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.BurialAssistanceCertification,
                "CERTIFICATION FOR BURIAL ASSISTANCE",
                "is known to this barangay as a resident qualified for burial assistance for a "
                + "member of the immediate family."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.IpScholarshipCertification,
                "CERTIFICATION FOR INDIGENOUS PEOPLES SCHOLARSHIP",
                "belongs to an indigenous people's household in this barangay and is recommended "
                + "for the scholarship programme."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.FourPsScholarshipCertification,
                "CERTIFICATION FOR 4Ps SCHOLARSHIP",
                "is a member of a household listed under the Pantawid Pamilyang Pilipino Program "
                + "with this barangay."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.AcceptanceCertificate,
                "CERTIFICATE OF ACCEPTANCE",
                "is accepted and acknowledged as a resident of this barangay."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.GadRelatedDocumentation,
                "GENDER AND DEVELOPMENT CERTIFICATION",
                "is recorded with this barangay for gender and development documentation."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.CsoDocumentation,
                "CIVIL SOCIETY ORGANISATION CERTIFICATION",
                "is recorded with this barangay in connection with a civil society organisation."));

            templates.Add(new AssistanceCertificationTemplate(DocumentType.OtherCertification,
                "CERTIFICATION",
                "is known to this barangay, and this certification is issued for the purpose "
                + "stated by the resident."));

            return templates;
        }
    }

    /// <summary>The Barangay ID. It carries the purok, the household position
    /// and the residency status, which is what the offices that accept it look
    /// for.</summary>
    public class BarangayIdTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.BarangayID; } }

        public override string GetTitle(DocumentContext context)
        {
            return "BARANGAY IDENTIFICATION CERTIFICATION";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + CivilStatusPhrase(resident) + ", " + ResidencySentence(resident, context)
                   + ", is a registered resident of this barangay and is entitled to a barangay "
                   + "identification card.");
            body.Add(Blank);
            body.Add("Purok: " + Purok(resident));
            body.Add("Residency status: " + EnumText.Of(resident.ResidencyStatus));
            body.Add("Household size: " + context.HouseholdSize);
            body.Add("Registered voter: " + (resident.IsRegisteredVoter ? "Yes" : "No"));
            body.Add(Blank);
            body.Add("This certification is issued without charge.");
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>The blotter-related document. It records that a resident
    /// reported an incident; the barangay does not settle the case here.</summary>
    public class BlotterRecordTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.BlotterRelatedIncident; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATION OF RECORDED INCIDENT";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context)
                   + ", is covered by a record kept in the barangay blotter.");
            body.Add(Blank);
            body.Add("This certification only states that a record exists. It is not a finding of "
                   + "guilt or innocence, and the matter is not resolved by this barangay.");
            body.Add(Blank);
            body.Add("This certification is issued without charge.");
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }
}
