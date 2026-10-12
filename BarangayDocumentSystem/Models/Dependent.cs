// ---------------------------------------------------------------------------
//  Dependent.cs - a person registered under a household head.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// Somebody who lives with a resident I already registered - a child, a
    /// spouse, a parent, a relative.
    ///
    /// Dependents are registered from inside the resident's own form, which is
    /// what the barangay asked for. They are not full residents: they do not
    /// get their own fee assessment and they cannot file a document request on
    /// their own, but the census counts them, and the household size shown on
    /// screen is the head plus these people.
    ///
    /// I keep the name in one piece here instead of first/middle/last. The
    /// barangay's own census sheet lists dependents by full name only, and
    /// breaking it up would mean asking a clerk for a middle name she does not
    /// have in front of her.
    /// </summary>
    public class Dependent
    {
        public int DependentId { get; internal set; }

        /// <summary>The resident this person hangs off - the household head.</summary>
        public int HeadResidentId { get; set; }

        public string FullName { get; set; }
        public DependentRelation Relation { get; set; }
        public DateTime DateOfBirth { get; set; }

        /// <summary>True when the dependent studies, so the barangay can count
        /// school-age children for its reports without a second list.</summary>
        public bool IsStudying { get; set; }

        public string Remarks { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }

        public Dependent()
        {
            FullName = string.Empty;
            Remarks = string.Empty;
            DateOfBirth = DateTime.Today;
            CreatedOn = DateTime.Now;
        }

        public int GetAge()
        {
            if (DateOfBirth == default(DateTime)) return 0;

            int age = DateTime.Today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            return age < 0 ? 0 : age;
        }

        /// <summary>The word I print for the relationship, so the census and
        /// the screen use the same vocabulary.</summary>
        public string GetRelationText()
        {
            return EnumText.Spaced(Relation.ToString());
        }

        public override string ToString()
        {
            return FullName + " (" + GetRelationText() + ")";
        }
    }
}
