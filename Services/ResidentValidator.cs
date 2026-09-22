using System;
using System.Linq;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public static class ResidentValidator
    {
        public static void Validate(Resident resident)
        {
            if (resident == null) throw new ArgumentException("Enter the resident's details.");
            Required(resident.FirstName, "First name", 80);
            Required(resident.LastName, "Last name", 80);
            Required(resident.Purok, "Purok", 60);
            Required(resident.Address, "Address", 250);
            Required(resident.ContactNumber, "Contact number", 15);
            Optional(resident.MiddleName, "Middle name", 80);
            Optional(resident.Suffix, "Suffix", 20);
            Optional(resident.Occupation, "Occupation", 100);

            DateTime today = DateTime.Today;
            if (resident.DateOfBirth.Date > today || resident.DateOfBirth.Date < today.AddYears(-130))
                throw new ArgumentException("Enter a birth date within the last 130 years.");
            if (resident.DateOfResidency.Date > today)
                throw new ArgumentException("Date of residency cannot be in the future.");
            if (resident.DateOfResidency.Date < resident.DateOfBirth.Date)
                throw new ArgumentException("Date of residency cannot be before the resident's birth date.");
            if (!Enum.IsDefined(typeof(Gender), resident.Gender) || !Enum.IsDefined(typeof(CivilStatus), resident.CivilStatus))
                throw new ArgumentException("Select a valid gender and civil status.");
            if (resident.IsSeniorCitizen && resident.GetAge(today) < 60)
                throw new ArgumentException("A resident must be at least 60 years old to use the senior citizen classification.");
            string contact = resident.ContactNumber.Trim();
            if (contact.Length < 7 || !contact.All(character => character >= '0' && character <= '9'))
                throw new ArgumentException("Contact number must contain 7 to 15 digits, without spaces or symbols.");
        }

        public static string Required(string value, string fieldName, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(fieldName + " is required.");
            string result = value.Trim();
            Optional(result, fieldName, maximumLength);
            return result;
        }

        public static string Optional(string value, string fieldName, int maximumLength)
        {
            string result = (value ?? "").Trim();
            if (result.Length > maximumLength)
                throw new ArgumentException(fieldName + " must be " + maximumLength + " characters or fewer.");
            if (result.Any(char.IsControl))
                throw new ArgumentException(fieldName + " must not contain tabs or line breaks.");
            return result;
        }
    }
}
