// ---------------------------------------------------------------------------
//  InputValidator.cs - the checks that keep bad data out of the registry.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// The rules a value has to pass before it is saved.
    ///
    /// Why not check in the form's KeyPress event? Because that only stops
    /// typing - a paste walks straight past it - and because two forms that
    /// need the same rule would end up with two slightly different versions of
    /// it. So: the form keeps its key filter for convenience, and every save
    /// button runs through here. One rule, one place, and the same rule is what
    /// my tests check.
    ///
    /// Every method hands back a list of sentences a clerk can read, instead of
    /// a single "invalid input" that tells nobody anything.
    /// </summary>
    public static class InputValidator
    {
        private static readonly Regex NamePattern = new Regex(@"^[A-Za-zÑñ\s'\-\.]+$", RegexOptions.Compiled);
        private static readonly Regex ContactPattern = new Regex(@"^[0-9\s\+\-\(\)]+$", RegexOptions.Compiled);
        private static readonly Regex UsernamePattern = new Regex(@"^[A-Za-z0-9\._\-]{4,50}$", RegexOptions.Compiled);

        // ==================================================================
        //  Resident
        // ==================================================================

        public static IList<string> ValidateResident(Resident resident)
        {
            List<string> problems = new List<string>();
            if (resident == null)
            {
                problems.Add("There is no resident to save.");
                return problems;
            }

            if (string.IsNullOrWhiteSpace(resident.FirstName))
                problems.Add("The first name is required.");
            else if (!IsName(resident.FirstName))
                problems.Add("The first name should have letters only.");

            if (string.IsNullOrWhiteSpace(resident.LastName))
                problems.Add("The last name is required.");
            else if (!IsName(resident.LastName))
                problems.Add("The last name should have letters only.");

            if (!string.IsNullOrWhiteSpace(resident.MiddleName) && !IsName(resident.MiddleName))
                problems.Add("The middle name should have letters only.");

            if (!string.IsNullOrWhiteSpace(resident.Suffix) && resident.Suffix.Length > 20)
                problems.Add("The suffix is too long. Use something short like Jr. or III.");

            if (resident.DateOfBirth == default(DateTime))
                problems.Add("The date of birth is required.");
            else if (resident.DateOfBirth.Date > DateTime.Today)
                problems.Add("The date of birth cannot be in the future.");
            else if (resident.DateOfBirth.Date < DateTime.Today.AddYears(-130))
                problems.Add("That date of birth is more than 130 years ago. Please check the year.");

            if (resident.DateOfResidency == default(DateTime))
                problems.Add("The date of residency is required.");
            else if (resident.DateOfResidency.Date > DateTime.Today)
                problems.Add("The date of residency cannot be in the future.");
            else if (resident.DateOfBirth != default(DateTime)
                     && resident.DateOfResidency.Date < resident.DateOfBirth.Date)
                problems.Add("The date of residency cannot be earlier than the date of birth.");

            if (string.IsNullOrWhiteSpace(resident.Purok))
                problems.Add("The purok is required - it is how the barangay locates the resident.");
            else if (!PurokList.Contains(resident.Purok))
                problems.Add("That purok is not one of the barangay's puroks. Please pick one from the list.");

            if (string.IsNullOrWhiteSpace(resident.ContactNumber))
                problems.Add("The contact number is required.");
            else if (!ContactPattern.IsMatch(resident.ContactNumber.Trim()))
                problems.Add("The contact number should have digits, spaces, + or - only.");
            else if (DigitsOf(resident.ContactNumber).Length < 7)
                problems.Add("That contact number looks too short. Please check it.");

            if (resident.HasClassification(ResidentClassification.SeniorCitizen) && resident.GetAge() < 60)
                problems.Add("The resident is marked a senior citizen but is only "
                             + resident.GetAge() + " years old. The law counts 60 and above.");

            if (resident.IsBusinessOwner && string.IsNullOrWhiteSpace(resident.Occupation))
                problems.Add("Please say what the business is, so the clearance records it.");

            return problems;
        }

        // ==================================================================
        //  Dependents
        // ==================================================================

        public static IList<string> ValidateDependent(Dependent dependent)
        {
            List<string> problems = new List<string>();
            if (dependent == null)
            {
                problems.Add("There is no dependent to save.");
                return problems;
            }

            if (string.IsNullOrWhiteSpace(dependent.FullName))
                problems.Add("The dependent's name is required.");
            else if (!IsName(dependent.FullName))
                problems.Add("A name should have letters only.");

            if (dependent.DateOfBirth == default(DateTime))
                problems.Add("The dependent's date of birth is required.");
            else if (dependent.DateOfBirth.Date > DateTime.Today)
                problems.Add("A date of birth cannot be in the future.");

            return problems;
        }

        // ==================================================================
        //  Requests
        // ==================================================================

        public static IList<string> ValidateRequest(Resident resident, DocumentRequest request, bool needsValidation)
        {
            List<string> problems = new List<string>();
            if (resident == null) problems.Add("Please choose the resident who is asking for the document.");

            if (request == null)
            {
                problems.Add("There is no request to file.");
                return problems;
            }

            if (string.IsNullOrWhiteSpace(request.Purpose))
                problems.Add("The purpose is required - it is printed on the document itself.");

            if (request.DateRequested == default(DateTime))
                problems.Add("The date the request was filed is required.");

            if (request.DocumentType == DocumentType.BarangayClearance && request.Purpose != null
                && request.Purpose.Trim().Length < 5)
                problems.Add("Please describe the purpose in a few words.");

            if (request.DocumentType == DocumentType.BarangayFacilityRental && request.Hours <= 0m)
                problems.Add("Please say how many hours the facility will be used.");

            if (request.DocumentType == DocumentType.CommunityTaxCertificate && request.GrossAnnualIncome < 0m)
                problems.Add("The declared income cannot be a negative amount.");

            if (request.DocumentType == DocumentType.OtherTarifaProcessingFee
                && (string.IsNullOrWhiteSpace(request.Detail) || request.AssessedAmount <= 0m))
                problems.Add("For a Taripa fee, please state the Taripa line and the amount to collect.");

            if (needsValidation && request.Business == null && request.DocumentType == DocumentType.BarangayBusinessClearance)
                problems.Add("The business details are required for a Barangay Business Clearance.");

            if (request.DocumentType == DocumentType.BarangayBusinessClearance && request.Business != null)
                foreach (string missing in request.Business.GetMissingFields())
                    problems.Add("The Barangay Business Clearance needs the " + missing + ".");

            if (resident != null && request.DocumentType == DocumentType.FirstTimeJobseekerCertificate
                && resident.HasAvailedFirstTimeJobseeker)
                problems.Add("This resident has already used the RA 11261 first-time jobseeker benefit.");

            return problems;
        }

        // ==================================================================
        //  Money
        // ==================================================================

        /// <summary>Checks the collection before it is written down. The two
        /// numbers that matter are the receipt number and its control number,
        /// because without them the collection cannot be traced to a booklet.</summary>
        public static IList<string> ValidatePayment(decimal amount, string orNumber, string controlNumber,
                                                   decimal fee, string seriesCode, ReceiptSeries matchedSeries)
        {
            List<string> problems = new List<string>();

            if (amount <= 0m)
                problems.Add("The amount collected has to be more than zero.");
            else if (amount < fee)
                problems.Add("The amount of P" + amount.ToString("#,##0.00")
                             + " is short of the assessed fee of P" + fee.ToString("#,##0.00") + ".");

            if (!OfficialReceipt.IsWellFormedOrNumber(orNumber))
                problems.Add("Please enter the official receipt number exactly as it is printed (at least 4 characters).");

            if (!OfficialReceipt.IsWellFormedControlNumber(controlNumber))
                problems.Add("Please enter the control number of the receipt booklet.");

            if (string.IsNullOrWhiteSpace(seriesCode))
                problems.Add("Please choose the receipt booklet (the series) the collection was written in.");

            if (matchedSeries == null && !string.IsNullOrWhiteSpace(controlNumber))
                problems.Add("Control number " + controlNumber.Trim()
                             + " is not inside any active receipt booklet. Please check the number, or ask the "
                             + "administrator to record the booklet on the Receipts screen.");

            return problems;
        }

        // ==================================================================
        //  Accounts
        // ==================================================================

        public static IList<string> ValidateUser(string username, string fullName, string roleIsChosen,
                                                 bool isNew, string password, string confirmation)
        {
            List<string> problems = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
                problems.Add("The user name is required.");
            else if (!UsernamePattern.IsMatch(username.Trim()))
                problems.Add("The user name should be 4 to 50 characters, letters, digits, dots, dashes or underscores.");

            if (string.IsNullOrWhiteSpace(fullName))
                problems.Add("The person's full name is required.");

            if (string.IsNullOrWhiteSpace(roleIsChosen))
                problems.Add("Please choose the role.");

            if (!isNew && string.IsNullOrEmpty(password)) return problems;

            IList<string> passwordProblems = ValidatePassword(password, confirmation);
            foreach (string problem in passwordProblems) problems.Add(problem);

            return problems;
        }

        public static IList<string> ValidatePassword(string password, string confirmation)
        {
            List<string> problems = new List<string>();
            int minimum = AppConfig.MinimumPasswordLength;

            if (string.IsNullOrEmpty(password))
            {
                problems.Add("The password is required.");
                return problems;
            }

            if (password.Length < minimum)
                problems.Add("The password needs at least " + minimum + " characters.");

            bool letter = false, digit = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c)) letter = true;
                if (char.IsDigit(c)) digit = true;
            }

            if (!letter || !digit)
                problems.Add("The password should have both letters and numbers.");

            if (!string.IsNullOrEmpty(confirmation) && !string.Equals(password, confirmation, StringComparison.Ordinal))
                problems.Add("The two passwords do not match.");

            return problems;
        }

        // ==================================================================
        //  Small readers the forms use
        // ==================================================================

        public static bool IsName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return NamePattern.IsMatch(text.Trim());
        }

        public static bool IsContact(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return ContactPattern.IsMatch(text.Trim());
        }

        /// <summary>Keeps a numeric field honest while somebody is typing in it:
        /// every character must be a digit or one decimal point.</summary>
        public static bool IsDecimalTyping(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            int points = 0;
            foreach (char c in text)
            {
                if (c == '.') { points++; continue; }
                if (!char.IsDigit(c)) return false;
            }
            return points <= 1;
        }

        /// <summary>"Juan, 34, Purok Orchids" - the one-line description of a
        /// resident used by the confirmation dialog.</summary>
        public static string Describe(Resident resident)
        {
            if (resident == null) return "No resident chosen";
            return resident.GetFullName() + ", " + resident.GetAge() + ", " + resident.Purok;
        }

        /// <summary>
        /// Turns the list of problems into one paragraph with a dash in front of
        /// each sentence.
        ///
        /// Every form asks the validator for its list and then hands it here, so
        /// a refusal reads the same on every screen - and, more importantly,
        /// shows ALL of the problems at once. Nobody wants to fix a form one
        /// message at a time.
        /// </summary>
        public static string Describe(IList<string> messages)
        {
            if (messages == null || messages.Count == 0) return string.Empty;

            StringBuilder text = new StringBuilder();
            foreach (string message in messages) text.AppendLine("- " + message);
            return text.ToString().TrimEnd();
        }

        private static string DigitsOf(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            System.Text.StringBuilder digits = new System.Text.StringBuilder();
            foreach (char c in text) if (char.IsDigit(c)) digits.Append(c);
            return digits.ToString();
        }
    }
}
