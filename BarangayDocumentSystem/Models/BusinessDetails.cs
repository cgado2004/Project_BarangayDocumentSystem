// ---------------------------------------------------------------------------
//  BusinessDetails.cs - what the barangay needs to know about a business
//  before it signs a business clearance.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// The business part of a Barangay Business Clearance.
    ///
    /// I asked for these details because the barangay cannot sign a clearance
    /// for a business it cannot name or locate. Two of them also decide the
    /// money: the nature of the business tells me whether the standard rate
    /// applies, and the ownership tells the barangay who is answerable if the
    /// business causes a complaint later.
    ///
    /// Where is the address? It is the purok, the same as for a person - the
    /// barangay asked me to stop keeping street addresses, and a stall inside
    /// a purok is located by the purok.
    /// </summary>
    public class BusinessDetails
    {
        public string BusinessName { get; set; }
        public string NatureOfBusiness { get; set; }
        public string Purok { get; set; }

        /// <summary>Where inside the purok, so the inspector can find it:
        /// "beside the covered court", "stall 14, public market".</summary>
        public string LocationNote { get; set; }

        public string OwnershipType { get; set; }

        /// <summary>DTI or SEC registration, if the owner has one. Small
        /// sari-sari stores often do not, so this stays optional.</summary>
        public string RegistrationNumber { get; set; }

        /// <summary>Mayor's permit number of the previous year, for renewals.</summary>
        public string PreviousPermitNumber { get; set; }

        public int EmployeeCount { get; set; }
        public bool IsRenewal { get; set; }

        public BusinessDetails()
        {
            BusinessName = string.Empty;
            NatureOfBusiness = string.Empty;
            Purok = string.Empty;
            LocationNote = string.Empty;
            OwnershipType = "Sole Proprietor";
            RegistrationNumber = string.Empty;
            PreviousPermitNumber = string.Empty;
        }

        /// <summary>The ownership choices I offer. A free-text box here would
        /// produce five spellings of "sole proprietor" in one week.</summary>
        public static readonly string[] OwnershipChoices =
        {
            "Sole Proprietor",
            "Partnership",
            "Corporation",
            "Cooperative",
            "Association"
        };

        /// <summary>
        /// What is still missing before the request can be validated.
        ///
        /// The screen uses this list to tell the clerk exactly which box to
        /// fill in, instead of showing one vague "incomplete" message.
        /// </summary>
        public IList<string> GetMissingFields()
        {
            List<string> missing = new List<string>();

            if (string.IsNullOrWhiteSpace(BusinessName)) missing.Add("business name");
            if (string.IsNullOrWhiteSpace(NatureOfBusiness)) missing.Add("nature of business");
            if (string.IsNullOrWhiteSpace(Purok)) missing.Add("purok where the business is");
            if (string.IsNullOrWhiteSpace(OwnershipType)) missing.Add("ownership type");

            return missing;
        }

        public bool IsComplete()
        {
            return GetMissingFields().Count == 0;
        }

        /// <summary>"Aling Nena's Sari-Sari Store (retail) - Purok Orchids".</summary>
        public string Summary()
        {
            string text = BusinessName;
            if (!string.IsNullOrWhiteSpace(NatureOfBusiness))
                text += " (" + NatureOfBusiness + ")";
            if (!string.IsNullOrWhiteSpace(Purok))
                text += " - " + Purok;
            return text;
        }
    }
}
