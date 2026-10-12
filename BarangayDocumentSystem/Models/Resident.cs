// ---------------------------------------------------------------------------
//  Resident.cs - one person on the barangay's registry.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// A resident of Barangay Magugpo Poblacion.
    ///
    /// Two decisions I want to explain, because they show up everywhere else:
    ///
    /// 1. There is no "Address" field. The barangay asked me to erase it. A
    ///    person's place in the barangay is their purok, which is also the
    ///    only thing the census and the reports group by, so the purok is what
    ///    I keep. Nothing is lost: the barangay is the whole address.
    ///
    /// 2. Nobody gets deleted. A resident who moved out, or who was encoded
    ///    twice, is deactivated or archived - see SetRecordState below. That
    ///    matters because certificates already issued and official receipts
    ///    already collected point back at this record, and history must not
    ///    develop holes.
    /// </summary>
    public class Resident
    {
        private readonly List<Dependent> _dependents = new List<Dependent>();
        private readonly List<DocumentRequest> _requests = new List<DocumentRequest>();

        public int ResidentId { get; internal set; }

        // ---- name ----
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Suffix { get; set; }

        // ---- who the person is ----
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public CivilStatus CivilStatus { get; set; }
        public string ContactNumber { get; set; }
        public string Occupation { get; set; }

        /// <summary>The purok. This is the only location detail the system
        /// keeps for a person, and it has to be one of the real puroks.</summary>
        public string Purok { get; set; }

        /// <summary>When the person started living in the barangay. I use it
        /// for the six-month jobseeker test, and to suggest the newcomer /
        /// temporary / permanent status.</summary>
        public DateTime DateOfResidency { get; set; }

        public bool IsRegisteredVoter { get; set; }

        /// <summary>Extra facts about the person. Nobody here decides a fee on
        /// their own - that is the job of the fee schedule.</summary>
        public ResidentClassification Classification { get; set; }

        /// <summary>
        /// A FEE CATEGORY, not a classification.
        ///
        /// Being a student only changes one thing in this system: the discount
        /// on the documents the barangay ordinance lists. So I keep it as a
        /// simple yes/no on the record instead of pretending it is a personal
        /// status like senior citizen or PWD.
        /// </summary>
        public bool IsStudentFeeCategory { get; set; }

        /// <summary>True when the barangay knows this person runs a business
        /// here. It only matters for the Barangay Business Clearance, which is
        /// the one document where the business details are asked for.</summary>
        public bool IsBusinessOwner { get; set; }

        /// <summary>The household head. The census counts households by
        /// looking for this flag and then counting the dependents under it.</summary>
        public bool IsHeadOfFamily { get; set; }

        // ---- status ----
        public ResidencyStatus ResidencyStatus { get; set; }
        public RecordState RecordState { get; set; }

        /// <summary>Why the record was switched off or filed away, and when.
        /// A clerk reading the record two years from now should not have to
        /// guess.</summary>
        public string StateReason { get; set; }
        public DateTime? StateChangedOn { get; set; }
        public string StateChangedBy { get; set; }

        /// <summary>RA 11261 may be used once in a lifetime, so I remember
        /// whether this person already used it.</summary>
        public bool HasAvailedFirstTimeJobseeker { get; set; }

        // ---- bookkeeping ----
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }

        public IReadOnlyList<Dependent> Dependents { get { return _dependents.AsReadOnly(); } }
        public IReadOnlyList<DocumentRequest> Requests { get { return _requests.AsReadOnly(); } }

        public Resident()
        {
            DateOfResidency = DateTime.Today;
            RecordState = RecordState.Active;
            ResidencyStatus = ResidencyStatus.Newcomer;
            Purok = string.Empty;
            FirstName = string.Empty;
            MiddleName = string.Empty;
            LastName = string.Empty;
            Suffix = string.Empty;
            ContactNumber = string.Empty;
            Occupation = string.Empty;
            CreatedOn = DateTime.Now;
        }

        // ==================================================================
        //  Names
        // ==================================================================

        /// <summary>
        /// "Juan P. Dela Cruz Jr."
        ///
        /// I look for the first real letter in the middle name before turning
        /// it into an initial, because a middle name of "." once printed as
        /// "Juan .. Dela Cruz" on a sample certificate. Punctuation does not
        /// belong on an official paper.
        /// </summary>
        public string GetFullName()
        {
            string suffix = string.IsNullOrWhiteSpace(Suffix) ? string.Empty : " " + Suffix.Trim();
            return (FirstName + MiddleInitial() + " " + LastName + suffix).Trim();
        }

        /// <summary>"Dela Cruz, Juan P." - what I sort the master list by.</summary>
        public string GetSortableName()
        {
            return (LastName + ", " + FirstName + MiddleInitial()).Trim();
        }

        private string MiddleInitial()
        {
            if (string.IsNullOrWhiteSpace(MiddleName)) return string.Empty;

            foreach (char c in MiddleName)
                if (char.IsLetter(c)) return " " + char.ToUpperInvariant(c) + ".";

            return string.Empty;
        }

        // ==================================================================
        //  Ages and residency
        // ==================================================================

        public int GetAge()
        {
            if (DateOfBirth == default(DateTime)) return 0;

            int age = DateTime.Today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
            return age < 0 ? 0 : age;
        }

        /// <summary>Whole months of continuous stay. The RA 11261 jobseeker
        /// benefit needs at least six of these.</summary>
        public int GetMonthsOfResidency()
        {
            DateTime today = DateTime.Today;
            int months = ((today.Year - DateOfResidency.Year) * 12) + today.Month - DateOfResidency.Month;
            if (today.Day < DateOfResidency.Day) months--;
            return Math.Max(0, months);
        }

        /// <summary>
        /// What I would write in the status box if nobody has touched it:
        /// a newcomer is inside the first months, a permanent resident has
        /// been here the long stretch, and everyone else is temporary.
        ///
        /// I return the suggestion instead of setting the field myself so the
        /// clerk stays in charge - the barangay knows its own people.
        /// </summary>
        public ResidencyStatus SuggestResidencyStatus(int newcomerMonths, int permanentYears)
        {
            int months = GetMonthsOfResidency();
            if (months < newcomerMonths) return ResidencyStatus.Newcomer;
            if (months >= permanentYears * 12) return ResidencyStatus.Permanent;
            return ResidencyStatus.Temporary;
        }

        // ==================================================================
        //  Classifications and the fee category
        // ==================================================================

        public bool HasClassification(ResidentClassification flag)
        {
            if (flag == ResidentClassification.None) return false;
            return (Classification & flag) == flag;
        }

        /// <summary>"Senior Citizen, PWD" - the readable version I show on
        /// screen and print on the census.</summary>
        public string GetClassificationText()
        {
            if (Classification == ResidentClassification.None) return "None";

            List<string> parts = new List<string>();
            if (HasClassification(ResidentClassification.SeniorCitizen)) parts.Add("Senior Citizen");
            if (HasClassification(ResidentClassification.PWD)) parts.Add("PWD");
            if (HasClassification(ResidentClassification.Indigent)) parts.Add("Indigent");
            if (HasClassification(ResidentClassification.SoloParent)) parts.Add("Solo Parent");
            if (HasClassification(ResidentClassification.FourPsBeneficiary)) parts.Add("4Ps Beneficiary");

            return parts.Count == 0 ? "None" : string.Join(", ", parts);
        }

        /// <summary>"Student", "Business owner", "Student, Business owner" or
        /// "Regular". This is the fee side of the record, and it is what the
        /// fee schedule actually reads.</summary>
        public string GetFeeCategoryText()
        {
            List<string> parts = new List<string>();
            if (IsStudentFeeCategory) parts.Add("Student");
            if (IsBusinessOwner) parts.Add("Business owner");
            return parts.Count == 0 ? "Regular" : string.Join(", ", parts);
        }

        /// <summary>True when the resident is entitled to a personal fee
        /// waiver. I ask this of the resident, never of the request, so the
        /// rule stays in one place.</summary>
        public bool HasPersonalFeeExemption()
        {
            return HasClassification(ResidentClassification.SeniorCitizen)
                || HasClassification(ResidentClassification.PWD)
                || HasClassification(ResidentClassification.Indigent)
                || HasClassification(ResidentClassification.SoloParent)
                || HasClassification(ResidentClassification.FourPsBeneficiary);
        }

        // ==================================================================
        //  Active / inactive / archived
        // ==================================================================

        public bool IsActive { get { return RecordState == RecordState.Active; } }

        /// <summary>
        /// Switch the record off, or file it away, always with a reason.
        ///
        /// I guard the transitions here rather than in the screen so that a
        /// mistake in a form cannot produce a record with no reason, and so
        /// the activity log always has something readable to write.
        /// </summary>
        public void SetRecordState(RecordState state, string reason, string changedBy, DateTime when)
        {
            if (state == RecordState && StateReason == reason) return;

            if (string.IsNullOrWhiteSpace(reason) && state != RecordState.Active)
                throw new InvalidOperationException(
                    "I need a reason before I can deactivate or archive a resident.");

            RecordState = state;
            StateReason = reason == null ? string.Empty : reason.Trim();
            StateChangedOn = when;
            StateChangedBy = changedBy ?? string.Empty;
        }

        public void Deactivate(string reason, string changedBy, DateTime when)
        {
            SetRecordState(RecordState.Inactive, reason, changedBy, when);
        }

        public void Archive(string reason, string changedBy, DateTime when)
        {
            SetRecordState(RecordState.Archived, reason, changedBy, when);
        }

        public void Reactivate(string changedBy, DateTime when)
        {
            SetRecordState(RecordState.Active, "Reactivated", changedBy, when);
        }

        // ==================================================================
        //  Household
        // ==================================================================

        /// <summary>How many people I should count for this household: the
        /// head, plus the dependents registered under the head.</summary>
        public int GetHouseholdSize()
        {
            return 1 + _dependents.Count;
        }

        internal void AddDependent(Dependent dependent)
        {
            if (dependent == null) throw new ArgumentNullException("dependent");
            if (!_dependents.Contains(dependent)) _dependents.Add(dependent);
        }

        internal void ClearDependents()
        {
            _dependents.Clear();
        }

        internal void AddRequest(DocumentRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (!_requests.Contains(request)) _requests.Add(request);
        }

        /// <summary>Age brackets the census counts people in. I keep the
        /// breaks here so the screen and the printed report can never
        /// disagree about what "working age" means.</summary>
        public static readonly int[] CensusAgeBreaks = { 0, 5, 13, 18, 31, 60 };

        public static readonly string[] CensusAgeLabels =
        {
            "Under 1", "1 - 5", "6 - 13", "14 - 18", "19 - 30", "31 - 59", "60 and above"
        };

        public static int CensusAgeBucket(int age)
        {
            if (age < CensusAgeBreaks[0]) return 0;
            for (int i = CensusAgeBreaks.Length - 1; i >= 0; i--)
                if (age >= CensusAgeBreaks[i]) return i;

            return 0;
        }

        public override string ToString()
        {
            return GetSortableName();
        }
    }
}
