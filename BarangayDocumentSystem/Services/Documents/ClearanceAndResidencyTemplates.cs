// ---------------------------------------------------------------------------
//  ClearanceAndResidencyTemplates.cs - the papers the counter issues most.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// The Barangay Clearance - the paper the barangay issues more than any
    /// other. Local employment is one price and work abroad is another, which
    /// is why the request asks which one it is for.
    /// </summary>
    public class BarangayClearanceTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.BarangayClearance; } }

        public override string GetTitle(DocumentContext context)
        {
            return "BARANGAY CLEARANCE";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + CivilStatusPhrase(resident) + ", " + Citizenship(resident) + ", is "
                   + ResidencySentence(resident, context) + ".");
            body.Add(Blank);
            body.Add("This further certifies that the above-named person is known to be of good moral "
                   + "character and reputation, and has no derogatory record filed before this Barangay "
                   + "as of this date.");
            body.Add(Blank);

            if (context.Request != null && context.Request.Scope == ClearanceScope.Abroad)
                body.Add("This clearance is issued for employment abroad.");
            else
                body.Add("This clearance is issued for local employment and other legal purposes.");

            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>The Certificate of Residency. This one carries the length of
    /// stay and the purok, because that is what the offices receiving it ask
    /// the barangay to confirm.</summary>
    public class ResidencyCertificateTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.CertificateOfResidency; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATE OF RESIDENCY";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + CivilStatusPhrase(resident) + ", is " + ResidencySentence(resident, context) + ".");
            body.Add(Blank);
            body.Add(ResidencyLengthLine(resident));
            body.Add("The resident is recorded with the barangay as a "
                   + EnumText.Of(resident.ResidencyStatus).ToLowerInvariant() + " resident.");
            body.Add(Blank);
            body.Add(HouseholdLine(context));
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>The Certificate of Good Moral Character, which is what a
    /// student or a job applicant is usually asked for.</summary>
    public class GoodMoralTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.CertificateOfGoodMoralCharacter; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATE OF GOOD MORAL CHARACTER";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + CivilStatusPhrase(resident) + ", " + ResidencySentence(resident, context) + ", "
                   + "is a person of good moral character and standing in this community.");
            body.Add(Blank);
            body.Add("The barangay has no record of any complaint, blotter entry or derogatory "
                   + "information against the above-named person as of this date.");
            body.Add(Blank);

            string classification = ClassificationPhrase(resident);
            if (!string.IsNullOrEmpty(classification))
                body.Add("The resident is recorded with the barangay as " + classification + ".");

            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>
    /// The Certificate of Indigency.
    ///
    /// This one is always free - it is the certificate that proves the need for
    /// help, so charging for it would defeat the point. The fee schedule makes
    /// it zero on purpose and this template says so in its wording.
    /// </summary>
    public class IndigencyTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.CertificateOfIndigency; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATE OF INDIGENCY";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that the family of " + NameInCapitals(resident) + ", "
                   + AgePhrase(resident) + ", " + ResidencySentence(resident, context)
                   + ", is known to this barangay as an indigent family.");
            body.Add(Blank);
            body.Add("The family has " + context.HouseholdSize + " member(s) and has been listed by the "
                   + "barangay among the households in need of assistance.");
            body.Add(Blank);
            body.Add("This certification is issued free of charge, as required for indigent residents.");
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }

    /// <summary>The Certificate of Low Income, the sibling of the certificate
    /// of indigency and free for the same reason.</summary>
    public class LowIncomeTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.CertificateOfLowIncome; } }

        public override string GetTitle(DocumentContext context)
        {
            return "CERTIFICATE OF LOW INCOME";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context)
                   + ", belongs to a household whose income is below the level the barangay records as low income.");
            body.Add(Blank);
            body.Add("This certification is issued free of charge for a resident of low income.");
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }
    }
}
