// ---------------------------------------------------------------------------
//  Enums.cs - every fixed list of choices the system speaks in.
//  These comments are mine, written the way I would explain it to a teammate.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// Every paper or service the barangay issues.
    ///
    /// The first seven are the documents the system was built for. The rest I
    /// copied off the tarpaulin posted at the barangay hall, so the list
    /// matches what the staff really hand out.
    ///
    /// Careful: the database stores the NAME of a value, never its number, so
    /// reordering this list cannot change what an old row means. Keep new
    /// values at the end anyway - it keeps the history easy to read.
    /// </summary>
    public enum DocumentType
    {
        BarangayClearance = 0,
        CertificateOfResidency = 1,
        CertificateOfIndigency = 2,
        BarangayBusinessClearance = 3,
        BarangayID = 4,
        FirstTimeJobseekerCertificate = 5,
        CertificateOfGoodMoralCharacter = 6,

        CertificateOfLowIncome = 7,
        SoloParentCertification = 8,
        MedicalAssistanceCertification = 9,
        FinancialAssistanceCertification = 10,
        BurialAssistanceCertification = 11,
        IpScholarshipCertification = 12,
        FourPsScholarshipCertification = 13,
        EmploymentCertification = 14,
        AcceptanceCertificate = 15,
        GadRelatedDocumentation = 16,
        BlotterRelatedIncident = 17,
        CsoDocumentation = 18,
        OtherCertification = 19,

        CommunityTaxCertificate = 20,
        LuponCaseFiling = 21,
        BarangayFacilityRental = 22,
        OtherTarifaProcessingFee = 23
    }

    /// <summary>Where a Barangay Clearance will be used. The Citizen's Charter
    /// prices local employment and work abroad differently, so I have to ask.</summary>
    public enum ClearanceScope
    {
        Local = 0,
        Abroad = 1
    }

    /// <summary>
    /// Where a request is in the queue.
    ///
    /// This is the part I am proudest of, because it is the rule the barangay
    /// asked for: a request that needs no validation and is filed between
    /// 8:00 AM and 4:00 PM is cleared on the spot, while anything filed from
    /// 4:01 PM to 7:59 AM waits as Pending until the next working day.
    ///
    /// Pending ......... filed outside the window, or waiting for a validator
    /// Processing ...... needs validation first (this is where business
    ///                   clearances always go)
    /// Cleared ......... validation is done and the request is approved
    /// ReadyForRelease . paid, printed and waiting on the counter
    /// Released ........ handed to the resident (the end of the road)
    /// Rejected ........ refused, with a reason that is always required
    /// </summary>
    public enum RequestStatus
    {
        Pending = 0,
        Processing = 1,
        Cleared = 2,
        ReadyForRelease = 3,
        Released = 4,
        Rejected = 5
    }

    public enum CivilStatus
    {
        Single = 0,
        Married = 1,
        Widowed = 2,
        Separated = 3,
        Divorced = 4
    }

    public enum Gender
    {
        Male = 0,
        Female = 1
    }

    /// <summary>
    /// The tags that can change what a resident pays.
    ///
    /// I made this a [Flags] list because one person is often several of these
    /// at once - a senior citizen who is also a person with disability is very
    /// common, and both waivers have to be honoured.
    ///
    /// "Student" is NOT here any more. It was a classification before, but a
    /// classification is about who a person is; being a student only matters
    /// because of the discount, so it moved to the fee side. You will find it
    /// on the resident record as a fee category instead (see IsStudentFeeCategory).
    /// "4Ps beneficiary" was added here because it is one of the classifications
    /// the barangay actually keeps.
    /// </summary>
    [Flags]
    public enum ResidentClassification
    {
        None = 0,
        SeniorCitizen = 1,
        PWD = 2,
        Indigent = 4,
        SoloParent = 8,
        FourPsBeneficiary = 16
    }

    /// <summary>
    /// How long somebody has been in the barangay - the "Temporary, Permanent,
    /// Newcomer" status the barangay asked for.
    ///
    /// The program suggests a value from the date of residency (a newcomer is
    /// inside the first six months, a permanent resident has been here five
    /// years or more, temporary is everything in between) but the clerk can
    /// always override it, because the barangay knows its own people better
    /// than my rule does.
    /// </summary>
    public enum ResidencyStatus
    {
        Newcomer = 0,
        Temporary = 1,
        Permanent = 2
    }

    /// <summary>
    /// Whether a record is in use, switched off, or filed away.
    ///
    /// Nobody deletes a resident in this system. Deactivating keeps the person
    /// on file but out of the active lists (they moved out, or they died, or
    /// they were encoded twice). Archiving is the same idea for records that
    /// are finished with - they stay in the database because released
    /// documents and official receipts point at them.
    /// </summary>
    public enum RecordState
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>The jobs inside the barangay hall. Each one sees a different
    /// menu, which is how the system keeps residents' data and the money
    /// screens away from people who should not touch them.</summary>
    public enum UserRole
    {
        Administrator = 0,
        Clerk = 1,
        PunongBarangay = 2
    }

    /// <summary>
    /// What I keep for each dependent. Dependents are registered under the
    /// person who heads the household, which is what "Dependents
    /// (Registration)" asked for and what the census needs to count.
    /// </summary>
    public enum DependentRelation
    {
        Spouse = 0,
        Son = 1,
        Daughter = 2,
        Parent = 3,
        Sibling = 4,
        Grandchild = 5,
        Relative = 6,
        Other = 7
    }

    /// <summary>How the money for a request was handed over. Everything is
    /// cash at the barangay hall today, but the receipt record keeps room for
    /// the others so an old row never has to be rewritten.</summary>
    public enum PaymentMethod
    {
        Cash = 0,
        Check = 1,
        Online = 2
    }

    /// <summary>The kinds of thing the activity log records. I group by module
    /// so the admin can filter the log the same way the screens are organised.</summary>
    public enum ActivityModule
    {
        Security = 0,
        Residents = 1,
        Requests = 2,
        Payments = 3,
        Census = 4,
        Reports = 5,
        Users = 6,
        Settings = 7
    }

    /// <summary>
    /// The puroks of Barangay Magugpo Poblacion. I took these names from the
    /// barangay's own list of projects, so they are the real ones rather than
    /// "Purok 1, Purok 2".
    /// </summary>
    public static class PurokList
    {
        public static readonly string[] All =
        {
            "Purok Arellano",
            "Purok Calachuchi",
            "Purok Cristo Rey",
            "Purok Dagohoy",
            "Purok Lapu-Lapu",
            "Purok Marilag 2",
            "Purok Orchids",
            "Purok Paraiso",
            "Purok Sampaguita",
            "Purok Sulgreg",
            "Purok Sunflower",
            "Purok Talisay",
            "Purok Tandang Sora",
            "Purok Tindalo"
        };

        /// <summary>True when the text is one of the real puroks. I use this in
        /// validation so a typing mistake cannot invent a purok.</summary>
        public static bool Contains(string purok)
        {
            if (string.IsNullOrWhiteSpace(purok)) return false;
            return All.Any(p => string.Equals(p, purok.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The purok list plus a blank first entry, for combo boxes
        /// that start empty.</summary>
        public static List<string> ForDropDown()
        {
            List<string> list = new List<string>();
            list.Add(string.Empty);
            list.AddRange(All);
            return list;
        }
    }

    /// <summary>
    /// Small helpers so the screens can print a value the way a person reads
    /// it ("Ready for Release" instead of "ReadyForRelease"). I keep this in
    /// one place because the same wording appears in the grid, the report and
    /// the printed slip.
    /// </summary>
    public static class EnumText
    {
        public static string Of(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Pending: return "Pending";
                case RequestStatus.Processing: return "Processing";
                case RequestStatus.Cleared: return "Cleared";
                case RequestStatus.ReadyForRelease: return "Ready for Release";
                case RequestStatus.Released: return "Released";
                case RequestStatus.Rejected: return "Rejected";
                default: return status.ToString();
            }
        }

        public static string Of(ResidencyStatus status)
        {
            switch (status)
            {
                case ResidencyStatus.Newcomer: return "Newcomer";
                case ResidencyStatus.Temporary: return "Temporary";
                case ResidencyStatus.Permanent: return "Permanent";
                default: return status.ToString();
            }
        }

        public static string Of(RecordState state)
        {
            switch (state)
            {
                case RecordState.Active: return "Active";
                case RecordState.Inactive: return "Inactive";
                case RecordState.Archived: return "Archived";
                default: return state.ToString();
            }
        }

        public static string Of(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator: return "Administrator";
                case UserRole.Clerk: return "Barangay Clerk";
                case UserRole.PunongBarangay: return "Punong Barangay";
                default: return role.ToString();
            }
        }

        /// <summary>Spaces out a PascalCase enum name: CertificateOfResidency
        /// becomes "Certificate of Residency". I use this for the long
        /// document list so I do not have to write 24 sentences by hand.</summary>
        public static string Spaced(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase)) return string.Empty;

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            for (int i = 0; i < pascalCase.Length; i++)
            {
                char c = pascalCase[i];
                bool boundary = i > 0 && char.IsUpper(c) &&
                                (char.IsLower(pascalCase[i - 1]) ||
                                 (i + 1 < pascalCase.Length && char.IsLower(pascalCase[i + 1])));
                if (boundary) text.Append(' ');
                text.Append(c);
            }
            return text.ToString();
        }
    }
}
