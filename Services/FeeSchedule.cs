using System;
using MySql.Data.MySqlClient;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class FeeSchedule
    {
        // Rates come from the database's fee_schedule table (seeded from
        // docs/07-fee-schedule-and-legal-basis.md - the Citizen's Charter) when
        // the app connects; the constructor below carries the same figures as
        // compiled defaults so the tests run without a server. The clearance is
        // PHP 100 for local employment and PHP 200 for work abroad - the reason
        // ClearanceScope exists.
        public readonly decimal ClearanceFee;
        public readonly decimal ClearanceAbroadFee;
        public readonly decimal ResidencyFee;
        public readonly decimal IndigencyFee;
        public readonly decimal BusinessClearanceFee;
        public readonly decimal BarangayIdFee;
        public readonly decimal JobseekerFee;
        public readonly decimal GoodMoralFee;


        /// <summary>Compiled defaults: the Citizen's Charter figures from
        /// docs/07, used by the tests and as fallback for missing table rows.</summary>
        public FeeSchedule()
        {
            ClearanceFee = 100m;
            ClearanceAbroadFee = 200m;
            ResidencyFee = 100m;
            IndigencyFee = 0m;
            BusinessClearanceFee = 200m;
            BarangayIdFee = 100m;
            JobseekerFee = 0m;
            GoodMoralFee = 100m;
        }

        /// <summary>Loads the live rates from the fee_schedule table that
        /// schema.sql seeds from the Citizen's Charter. A missing row keeps
        /// its compiled default; connection failures surface to the caller
        /// (the app cannot price documents without its database anyway).</summary>
        public FeeSchedule(string connectionString)
            : this()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(
                    "SELECT fee_code, amount FROM fee_schedule", connection))
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        decimal amount = reader.GetDecimal(1);
                        switch (reader.GetString(0))
                        {
                            case "clearance_local": ClearanceFee = amount; break;
                            case "clearance_abroad": ClearanceAbroadFee = amount; break;
                            case "certification": ResidencyFee = amount; GoodMoralFee = amount; break;
                            case "indigency": IndigencyFee = amount; break;
                            case "business_clearance": BusinessClearanceFee = amount; break;
                            case "barangay_id": BarangayIdFee = amount; break;
                            case "first_time_jobseeker": JobseekerFee = amount; break;
                        }
                    }
            }
        }
        public FeeAssessment Assess(Resident resident, DocumentType documentType)
        {
            return Assess(resident, documentType, ClearanceScope.Local);
        }

        public FeeAssessment Assess(Resident resident, DocumentType documentType, ClearanceScope scope)
        {
            if (resident == null) throw new ArgumentException("Select a resident first.");
            decimal fee = GetBaseFee(documentType);
            if (documentType == DocumentType.BarangayClearance && scope == ClearanceScope.Abroad)
                fee = ClearanceAbroadFee;
            if (documentType == DocumentType.BarangayBusinessClearance)
                return new FeeAssessment(fee, "Project schedule: business clearance; personal exemptions do not apply.");
            if (documentType == DocumentType.FirstTimeJobseekerCertificate)
            {
                ValidateJobseeker(resident);
                return new FeeAssessment(JobseekerFee, "First-time jobseeker certification - RA 11261.");
            }
            if (documentType == DocumentType.CertificateOfIndigency)
                return new FeeAssessment(IndigencyFee, "Project policy: free certificate of indigency.");
            if (resident.IsIndigent)
                return new FeeAssessment(0m, "Project policy: indigent resident exemption.");
            if (resident.IsSeniorCitizen)
                return new FeeAssessment(0m, "Project policy: senior citizen exemption (reference: RA 9994).");
            if (resident.IsPersonWithDisability)
                return new FeeAssessment(0m, "Project policy: PWD exemption (reference: RA 10754).");
            if (documentType == DocumentType.CertificateOfResidency ||
                documentType == DocumentType.CertificateOfGoodMoralCharacter)
                return new FeeAssessment(fee, "Citizen's Charter: residency and good moral certification.");
            if (documentType == DocumentType.BarangayClearance)
                return new FeeAssessment(fee, scope == ClearanceScope.Abroad
                    ? "Citizen's Charter: barangay clearance, work abroad."
                    : "Citizen's Charter: barangay clearance, local employment.");
            return new FeeAssessment(fee, "Standard classroom rate; local ordinance rates are not configured.");
        }

        public decimal GetBaseFee(DocumentType type)
        {
            switch (type)
            {
                case DocumentType.BarangayClearance: return ClearanceFee;
                case DocumentType.CertificateOfResidency: return ResidencyFee;
                case DocumentType.CertificateOfIndigency: return IndigencyFee;
                case DocumentType.BarangayBusinessClearance: return BusinessClearanceFee;
                case DocumentType.BarangayId: return BarangayIdFee;
                case DocumentType.FirstTimeJobseekerCertificate: return JobseekerFee;
                case DocumentType.CertificateOfGoodMoralCharacter: return GoodMoralFee;
                default: throw new ArgumentException("Select a valid document type.");
            }
        }

        public void ValidateJobseeker(Resident resident)
        {
            if (resident.HasUsedJobseekerBenefit)
                throw new InvalidOperationException("This resident has already used the first-time jobseeker benefit.");
            if (resident.DateOfResidency.Date.AddMonths(6) > DateTime.Today)
                throw new InvalidOperationException("First-time jobseeker certification requires at least six months of residency.");
        }
    }
}
