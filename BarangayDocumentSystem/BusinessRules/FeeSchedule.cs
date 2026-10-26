using System;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.BusinessRules;


public class FeeAssessment
{
    public decimal BaseFee { get; init; }
    public decimal FinalFee { get; init; }
    public string Basis { get; init; } = string.Empty;
    public bool IsWaived => FinalFee == 0;
    public bool IsExempt { get; init; }
}
































public class FeeSchedule
{

    public const decimal ClearanceFee        = 50m;
    public const decimal ResidencyFee        = 100m;
    public const decimal IndigencyFee        = 0m;
    public const decimal BusinessClearanceFee = 200m;
    public const decimal BarangayIdFee       = 100m;
    public const decimal GoodMoralFee        = 100m;
    public const decimal JobseekerFee        = 0m;






    public const decimal StudentDiscountRate = 0.50m;


    public const int RequiredMonthsForJobseeker = 6;





    public FeeAssessment Assess(Resident resident, DocumentType documentType, bool isStudent = false)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        decimal baseFee = GetBaseFee(documentType);


        if (documentType == DocumentType.FirstTimeJobseekerCertificate)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — RA 11261 (First Time Jobseekers Assistance Act)",
                IsExempt = true
            };
        }


        if (documentType == DocumentType.CertificateOfIndigency)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Certificate of Indigency (DILG MC 2019-177)",
                IsExempt = true
            };
        }



        if (documentType == DocumentType.BarangayBusinessClearance)
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = baseFee,
                Basis    = "Business clearance — personal exemptions do not apply",
                IsExempt = false
            };
        }


        if (resident.HasClassification(ResidentClassification.Indigent))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — indigent resident",
                IsExempt = true
            };
        }


        if (resident.HasClassification(ResidentClassification.SeniorCitizen))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Senior Citizen (RA 9994)",
                IsExempt = true
            };
        }


        if (resident.HasClassification(ResidentClassification.PWD))
        {
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = 0m,
                Basis    = "FREE — Person With Disability (RA 10754)",
                IsExempt = true
            };
        }




        if (isStudent && baseFee > 0)
        {
            decimal discounted = Math.Round(baseFee * (1m - StudentDiscountRate), 2);
            return new FeeAssessment
            {
                BaseFee  = baseFee,
                FinalFee = discounted,
                Basis    = $"Student discount ({StudentDiscountRate:P0} off) — student eligibility confirmed",
                IsExempt = true
            };
        }


        return new FeeAssessment
        {
            BaseFee  = baseFee,
            FinalFee = baseFee,
            Basis    = "Standard rate per barangay revenue ordinance",
            IsExempt = false
        };
    }

    public decimal GetBaseFee(DocumentType type) => type switch
    {
        DocumentType.BarangayClearance             => ClearanceFee,
        DocumentType.CertificateOfResidency        => ResidencyFee,
        DocumentType.CertificateOfIndigency        => IndigencyFee,
        DocumentType.BarangayBusinessClearance     => BusinessClearanceFee,
        DocumentType.BarangayID                    => BarangayIdFee,
        DocumentType.FirstTimeJobseekerCertificate => JobseekerFee,
        DocumentType.CertificateOfGoodMoralCharacter => GoodMoralFee,
        _                                          => 0m
    };





    public bool CanIssueJobseekerCertificate(Resident resident, out string reason)
    {
        if (resident is null) throw new ArgumentNullException(nameof(resident));

        if (resident.HasAvailedFirstTimeJobseeker)
        {
            reason = "This resident has already availed of the RA 11261 benefit. " +
                     "The law allows it only once.";
            return false;
        }

        int months = resident.GetMonthsOfResidency();
        if (months < RequiredMonthsForJobseeker)
        {
            reason = $"RA 11261 requires at least {RequiredMonthsForJobseeker} months' " +
                     $"residency in the barangay. This resident has {months} month(s).";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
