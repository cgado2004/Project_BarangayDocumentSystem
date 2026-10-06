// ---------------------------------------------------------------------------
//  BusinessAndMoneyTemplates.cs - the business clearance and the papers that
//  are about a service rather than about a person.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// The Barangay Business Clearance.
    ///
    /// This is the only document the system treats differently, and the
    /// difference is the point: it is a REGULATORY fee on a business, so the
    /// owner being a senior citizen or a person with disability does not waive
    /// it. A clearance for a sari-sari store also cannot be signed from a desk
    /// - somebody has to look at where the business is - so this request always
    /// passes through Processing before it is cleared.
    ///
    /// The wording names the business, what it does and which purok it is in,
    /// which is what the inspecting officer checks on the ground.
    /// </summary>
    public class BusinessClearanceTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.BarangayBusinessClearance; } }

        public override bool RequiresBusinessDetails { get { return true; } }

        public override string GetTitle(DocumentContext context)
        {
            return "BARANGAY BUSINESS CLEARANCE";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;
            BusinessDetails business = context.Request == null ? null : context.Request.Business;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This is to certify that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context) + ", is authorized to operate a "
                   + "business in this barangay, as follows:");
            body.Add(Blank);

            if (business != null)
            {
                body.Add("Business name      : " + business.BusinessName.ToUpperInvariant());
                body.Add("Nature of business : " + business.NatureOfBusiness);
                body.Add("Location           : " + business.Purok
                       + (string.IsNullOrWhiteSpace(business.LocationNote) ? string.Empty : " - " + business.LocationNote));
                body.Add("Ownership          : " + business.OwnershipType);
                body.Add("Employees          : " + business.EmployeeCount);

                if (!string.IsNullOrWhiteSpace(business.RegistrationNumber))
                    body.Add("Registration       : " + business.RegistrationNumber);

                if (business.IsRenewal && !string.IsNullOrWhiteSpace(business.PreviousPermitNumber))
                    body.Add("Previous permit    : " + business.PreviousPermitNumber);
            }

            body.Add(Blank);
            body.Add("The business complies with the requirements of this barangay and may be issued the "
                   + "clearance it has applied for. This clearance is issued on the condition that the "
                   + "business keeps peace and order and follows the barangay's ordinances.");
            body.Add(Blank);
            body.Add("This clearance is a regulatory fee of the barangay and is not waived by the "
                   + "personal status of the owner.");
            body.Add(PurposeLine(context));
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        public override string GetClosingLine(DocumentContext context)
        {
            return "Approved by:";
        }
    }

    /// <summary>
    /// The Community Tax Certificate (cedula).
    ///
    /// The amount is not a fixed price: RA 7160 Sec. 156 computes it from the
    /// income the person declares, which is why the request asks for the gross
    /// annual income. The paper states the computation so the resident can see
    /// how the figure was reached.
    /// </summary>
    public class CommunityTaxTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.CommunityTaxCertificate; } }

        public override string GetTitle(DocumentContext context)
        {
            return "COMMUNITY TAX CERTIFICATE (CEDULA)";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;
            decimal income = context.Request == null ? 0m : context.Request.GrossAnnualIncome;
            decimal tax = context.Request == null ? 0m : context.Request.Fee;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This certifies that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context)
                   + ", has paid the community tax for the current year in the amount of "
                   + Peso(tax) + ".");
            body.Add(Blank);
            body.Add("Declared gross annual income : " + Peso(income));
            body.Add("Basic community tax          : " + Peso(Config.AppConfig.CommunityTaxBase));
            body.Add("Additional tax               : "
                   + Peso(tax - Config.AppConfig.CommunityTaxBase)
                   + " (one peso for every thousand pesos of declared income, as capped)");
            body.Add("Total paid                   : " + Peso(tax));
            body.Add(Blank);
            body.Add("Computed under Republic Act No. 7160, Section 156, from the income declared by "
                   + "the taxpayer.");
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        private static string Peso(decimal value)
        {
            return "P" + (value < 0m ? 0m : value).ToString("#,##0.00");
        }
    }

    /// <summary>The Katarungang Pambarangay filing fee receipt-clerk's paper:
    /// it records that a complaint was filed with the Lupon and the fee for it.</summary>
    public class LuponFilingTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.LuponCaseFiling; } }

        public override string GetTitle(DocumentContext context)
        {
            return "KATARUNGANG PAMBARANGAY FILING";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This records that " + NameInCapitals(resident) + ", " + AgePhrase(resident)
                   + ", " + ResidencySentence(resident, context)
                   + ", has filed a complaint with the Lupong Tagapamayapa of this barangay.");
            body.Add(Blank);
            body.Add("Filing fee paid : " + Peso(context.Request == null ? 0m : context.Request.Fee));
            body.Add("Subject         : " + (context.Request == null || string.IsNullOrWhiteSpace(context.Request.Detail)
                   ? "as stated in the complaint" : context.Request.Detail));
            body.Add(Blank);
            body.Add("The parties will be called to mediation or conciliation on the date the Lupon "
                   + "sets, as required by Republic Act No. 7160, Sections 399 to 422.");
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        private static string Peso(decimal value)
        {
            return "P" + value.ToString("#,##0.00");
        }
    }

    /// <summary>The barangay facility rental agreement - the covered court or
    /// the multi-purpose hall, billed by the hour.</summary>
    public class FacilityRentalTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.BarangayFacilityRental; } }

        public override string GetTitle(DocumentContext context)
        {
            return "BARANGAY FACILITY USE AGREEMENT";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;
            DocumentRequest request = context.Request;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This agreement is between the Barangay and " + NameInCapitals(resident) + ", "
                   + AgePhrase(resident) + ", " + ResidencySentence(resident, context)
                   + ", for the use of a barangay facility.");
            body.Add(Blank);
            body.Add("Facility   : " + (request == null || string.IsNullOrWhiteSpace(request.Detail)
                   ? "Barangay facility" : request.Detail));
            body.Add("Hours used : " + (request == null ? 0m : request.Hours).ToString("#,##0.##"));
            body.Add("Rate       : " + Peso(Config.AppConfig.FeeFacilityPerHour) + " per hour");
            body.Add("Total      : " + Peso(request == null ? 0m : request.Fee));
            body.Add(Blank);
            body.Add("The user agrees to keep the facility clean, to follow the barangay's rules on "
                   + "hours and noise, and to answer for any damage caused during the use.");
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        public override string GetClosingLine(DocumentContext context)
        {
            return "Agreed:";
        }

        private static string Peso(decimal value)
        {
            return "P" + value.ToString("#,##0.00");
        }
    }

    /// <summary>Any other fee the barangay's Taripa prices. The clerk states
    /// the Taripa line and the amount, and the paper records both, which is
    /// what makes the collection explainable later.</summary>
    public class TaripaFeeTemplate : DocumentTemplateBase
    {
        public override DocumentType SupportedType { get { return DocumentType.OtherTarifaProcessingFee; } }

        public override string GetTitle(DocumentContext context)
        {
            return "BARANGAY PROCESSING FEE (TARIPA)";
        }

        public override IList<string> BuildBody(DocumentContext context)
        {
            List<string> body = new List<string>();
            Resident resident = context.Resident;
            DocumentRequest request = context.Request;

            body.Add("TO WHOM IT MAY CONCERN:");
            body.Add(Blank);
            body.Add("This records a barangay processing fee collected from " + NameInCapitals(resident)
                   + ", " + AgePhrase(resident) + ", " + ResidencySentence(resident, context) + ".");
            body.Add(Blank);
            body.Add("Taripa line : " + (request == null || string.IsNullOrWhiteSpace(request.Detail)
                   ? "as assessed by the clerk" : request.Detail));
            body.Add("Amount      : " + Peso(request == null ? 0m : request.Fee));
            body.Add(Blank);
            body.Add("Collected under the Barangay Taripa, as authorized by Republic Act No. 7160, "
                   + "Section 152, and posted in the barangay's Citizen's Charter.");
            body.Add(Blank);
            body.Add(IssuedLine(context));

            return body;
        }

        private static string Peso(decimal value)
        {
            return "P" + value.ToString("#,##0.00");
        }
    }
}
