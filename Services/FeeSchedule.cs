using System;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class FeeSchedule
    {
        // Residency and good moral base fees follow the supplied Citizen's Charter photo.
        // Other rates and personal exemptions still use the classroom policy.
        // v2: the clearance joined them - the Charter prices it PHP 100 for local
        // employment and PHP 200 for work abroad (the reason ClearanceScope exists).
        public const decimal ClearanceFee = 100m;
        public const decimal ClearanceAbroadFee = 200m;
        public const decimal ResidencyFee = 100m;
        public const decimal IndigencyFee = 0m;
        public const decimal BusinessClearanceFee = 200m;
        public const decimal BarangayIdFee = 100m;
        public const decimal JobseekerFee = 0m;
        public const decimal GoodMoralFee = 100m;

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
