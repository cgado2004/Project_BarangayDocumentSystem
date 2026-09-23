using System;

namespace BarangayDocumentSystem.Models
{
    public class Resident
    {
        public int ResidentId { get; set; }
        // Stops old edits from replacing newer ones.
        public int Version { get; internal set; }
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Suffix { get; set; } = "";
        public DateTime DateOfBirth { get; set; } = new DateTime(2000, 1, 1);
        public Gender Gender { get; set; }
        public CivilStatus CivilStatus { get; set; }
        public string Purok { get; set; } = "";
        public string Address { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public string Occupation { get; set; } = "";
        public DateTime DateOfResidency { get; set; } = DateTime.Today;
        public bool IsRegisteredVoter { get; set; }
        public bool IsSeniorCitizen { get; set; }
        public bool IsPersonWithDisability { get; set; }
        public bool IsIndigent { get; set; }
        public bool IsStudent { get; set; }
        public bool IsSoloParent { get; set; }
        public bool HasUsedJobseekerBenefit { get; set; }

        public string FullName
        {
            get { return string.Join(" ", new[] { FirstName, MiddleName, LastName, Suffix }).Replace("  ", " ").Trim(); }
        }

        public string FullAddress { get { return Address + ", " + Purok; } }

        public int GetAge(DateTime onDate)
        {
            int age = onDate.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > onDate.Date.AddYears(-age)) age--;
            return age;
        }

        public Resident Copy()
        {
            return (Resident)MemberwiseClone();
        }
    }
}
