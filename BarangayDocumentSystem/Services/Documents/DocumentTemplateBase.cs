// ---------------------------------------------------------------------------
//  DocumentTemplateBase.cs - the parts every document wording shares.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services.Documents
{
    /// <summary>
    /// The shared way of writing a document.
    ///
    /// Every paper the barangay issues starts the same way - "TO WHOM IT MAY
    /// CONCERN", then who the person is - and ends the same way. I put those
    /// sentences here so one template cannot accidentally describe a resident
    /// differently from the next one, which is exactly the kind of thing a
    /// barangay clerk notices and a panel does too.
    ///
    /// Notice what is NOT here: the address. The barangay asked for it to be
    /// erased, so a resident is described by name, age, civil status and purok.
    /// The purok alone is the location.
    /// </summary>
    public abstract class DocumentTemplateBase : IDocumentTemplate
    {
        public abstract DocumentType SupportedType { get; }

        public abstract string GetTitle(DocumentContext context);

        public abstract IList<string> BuildBody(DocumentContext context);

        public virtual string GetClosingLine(DocumentContext context)
        {
            return "Respectfully yours,";
        }

        public virtual bool RequiresBusinessDetails
        {
            get { return false; }
        }

        // ==================================================================
        //  Sentences every document needs
        // ==================================================================

        /// <summary>"JUAN P. DELA CRUZ JR." - official papers print a name in
        /// capitals, so I do that here rather than in every template.</summary>
        protected static string NameInCapitals(Resident resident)
        {
            return resident == null ? string.Empty : resident.GetFullName().ToUpperInvariant();
        }

        /// <summary>"of legal age" or "34 years old" - the way a certificate
        /// describes a person in a sentence.</summary>
        protected static string AgePhrase(Resident resident)
        {
            if (resident == null) return "of legal age";
            int age = resident.GetAge();
            if (age <= 0) return "of legal age";
            return age + " years old";
        }

        protected static string CivilStatusPhrase(Resident resident)
        {
            if (resident == null) return string.Empty;
            return resident.CivilStatus.ToString().ToLowerInvariant();
        }

        protected static string Purok(Resident resident)
        {
            if (resident == null || string.IsNullOrWhiteSpace(resident.Purok)) return "the barangay";
            return resident.Purok;
        }

        /// <summary>"Filipino" is what every printed certificate says, and
        /// RA 11261 also assumes it for a registered resident.</summary>
        protected static string Citizenship(Resident resident)
        {
            return "Filipino";
        }

        /// <summary>
        /// "a bona fide resident of Purok Orchids, Barangay Magugpo Poblacion"
        ///
        /// This one sentence is the reason the address field could be erased
        /// without weakening any certificate: the barangay is the address, and
        /// the purok is how the barangay itself locates a household.
        /// </summary>
        protected static string ResidencySentence(Resident resident, DocumentContext context)
        {
            string barangay = context != null && context.Profile != null
                ? context.Profile.BarangayName
                : "Barangay Magugpo Poblacion";

            return "a bona fide resident of " + Purok(resident) + ", " + barangay;
        }

        /// <summary>The household line I print on the certificates that the
        /// barangay uses for its own records: who heads the family and how many
        /// dependents are registered under it.</summary>
        protected static string HouseholdLine(DocumentContext context)
        {
            if (context == null || context.Resident == null) return string.Empty;

            if (!context.Resident.IsHeadOfFamily)
                return "The resident is a member of a household in the barangay.";

            return "The resident heads a household of " + context.HouseholdSize
                 + " member(s), with " + context.Dependents.Count
                 + " dependent(s) registered with the barangay.";
        }

        /// <summary>How long the person has lived here, which the residency
        /// certificates state because the barangay is asked about it daily.</summary>
        protected static string ResidencyLengthLine(Resident resident)
        {
            if (resident == null) return string.Empty;

            int months = resident.GetMonthsOfResidency();
            if (months < 1) return "The resident is a newcomer to the barangay.";

            int years = months / 12;
            int remainder = months % 12;

            string length = years > 0
                ? years + " year(s)" + (remainder > 0 ? " and " + remainder + " month(s)" : string.Empty)
                : months + " month(s)";

            return "The resident has lived in this barangay for " + length + ".";
        }

        /// <summary>"a student" / "a senior citizen" / nothing at all - the
        /// short description I add to the residency sentence when it helps the
        /// office receiving the paper.</summary>
        protected static string ClassificationPhrase(Resident resident)
        {
            if (resident == null) return string.Empty;

            if (resident.HasClassification(ResidentClassification.SeniorCitizen)) return "a senior citizen";
            if (resident.HasClassification(ResidentClassification.PWD)) return "a person with disability";
            if (resident.HasClassification(ResidentClassification.SoloParent)) return "a solo parent";
            if (resident.HasClassification(ResidentClassification.FourPsBeneficiary)) return "a 4Ps beneficiary";
            if (resident.IsStudentFeeCategory) return "a student";

            return string.Empty;
        }

        protected static string PurposeLine(DocumentContext context)
        {
            if (context == null || context.Request == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(context.Request.Purpose)) return string.Empty;

            return "This certification is issued upon the request of the above-named resident for "
                 + context.Request.Purpose.Trim().TrimEnd('.') + ".";
        }

        protected static string IssuedLine(DocumentContext context)
        {
            DateTime when = context != null && context.Request != null
                ? (context.Request.DateReleased ?? context.Request.DateRequested)
                : DateTime.Today;

            return "Issued this " + when.Day + DaySuffix(when.Day) + " day of "
                 + when.ToString("MMMM yyyy", CultureInfo.InvariantCulture) + " at "
                 + (context != null && context.Profile != null ? context.Profile.BarangayName : "the barangay")
                 + ", " + (context != null && context.Profile != null ? context.Profile.CityName : "Tagum City")
                 + ".";
        }

        private static string DaySuffix(int day)
        {
            if (day % 100 >= 11 && day % 100 <= 13) return "th";
            switch (day % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        /// <summary>A blank line in the body. The renderer keeps the spacing,
        /// so a template that needs air does not have to draw anything.</summary>
        protected static string Blank
        {
            get { return string.Empty; }
        }
    }
}
