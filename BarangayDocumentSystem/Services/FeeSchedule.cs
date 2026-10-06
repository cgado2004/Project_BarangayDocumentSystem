// ---------------------------------------------------------------------------
//  FeeSchedule.cs - every rule about money, in one place.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    /// <summary>What the fee schedule decided, and why.</summary>
    public class FeeAssessment
    {
        /// <summary>What the document costs before any waiver or discount.</summary>
        public decimal BaseFee { get; set; }

        /// <summary>What the resident actually pays. Zero means free.</summary>
        public decimal FinalFee { get; set; }

        /// <summary>
        /// The reason behind the amount, in words.
        ///
        /// This is INTERNAL. The clerk's screen shows the amount only, which is
        /// what the barangay asked for, but the sentence is still written on
        /// the record and it is what the admin sees in the activity log and the
        /// collection report when somebody asks "why was this one free?".
        /// </summary>
        public string Basis { get; set; }

        /// <summary>True when the resident was entitled to a waiver rather
        /// than simply not being charged.</summary>
        public bool IsExempt { get; set; }

        /// <summary>True when the student fee category lowered the price.</summary>
        public bool StudentDiscountApplied { get; set; }

        /// <summary>Short label for the fee column: "Regular", "Senior
        /// citizen", "Student", "Business", "Free by law".</summary>
        public string Category { get; set; }

        public FeeAssessment()
        {
            Basis = string.Empty;
            Category = "Regular";
        }

        public bool IsFree { get { return FinalFee <= 0m; } }

        public decimal Discount
        {
            get { return BaseFee > FinalFee ? BaseFee - FinalFee : 0m; }
        }
    }

    /// <summary>
    /// All the money rules of the barangay, and nothing else.
    ///
    /// I kept this class free of any screen code on purpose. No form ever
    /// works out a price - a form asks this class and shows what comes back.
    /// That is why the same rules hold whether the fee is computed from the
    /// request dialog, from the sample data, or from a rule check running with
    /// no user interface at all.
    ///
    /// The order of the checks below matters and I will explain why:
    ///
    ///   1. The business clearance is handled FIRST, because it is a
    ///      regulatory fee on an enterprise and never waived by the owner's
    ///      personal status. A senior citizen renewing a sari-sari store
    ///      permit still pays. If I checked the senior waiver first, she
    ///      would not, and the barangay would lose revenue it is entitled to.
    ///
    ///   2. Then the documents that are free by law for everybody
    ///      (certificate of indigency, certificate of low income).
    ///
    ///   3. Then the personal waivers: RA 9994 senior citizens, RA 10754
    ///      persons with disability, DILG MC 2019-177 for indigent residents,
    ///      solo parents, and 4Ps beneficiaries.
    ///
    ///   4. Then the once-in-a-lifetime RA 11261 jobseeker waiver, which is
    ///      checked against eligibility before it is applied.
    ///
    ///   5. Then the student fee category, which is a discount and not a
    ///      waiver, and only on the documents the ordinance lists.
    ///
    /// All amounts come from App.config, so a new revenue ordinance is a
    /// settings change rather than a rebuild.
    /// </summary>
    public class FeeSchedule
    {
        public FeeSchedule()
        {
        }

        // ==================================================================
        //  The one method the rest of the program calls
        // ==================================================================

        public FeeAssessment Assess(Resident resident, DocumentRequest request)
        {
            if (resident == null) throw new ArgumentNullException("resident");
            if (request == null) throw new ArgumentNullException("request");

            FeeAssessment assessment = new FeeAssessment();

            // ---- 1. Business clearance ------------------------------------
            if (request.DocumentType == DocumentType.BarangayBusinessClearance)
            {
                assessment.BaseFee = AppConfig.FeeBusinessClearance;
                assessment.FinalFee = AppConfig.FeeBusinessClearance;
                assessment.Category = "Business";
                assessment.Basis = "Barangay Business Clearance at the standard rate of P"
                    + AppConfig.FeeBusinessClearance.ToString("#,##0.00")
                    + ". This is a regulatory fee on the business, so the owner's personal "
                    + "exemptions (senior citizen, PWD, indigent, solo parent, 4Ps, student) "
                    + "do not apply to it. Citizen's Charter; RA 7160, Sec. 152.";
                return assessment;
            }

            // ---- what this document costs in the first place --------------
            decimal baseFee = StandardFeeFor(request);
            assessment.BaseFee = baseFee;
            assessment.FinalFee = baseFee;

            // ---- 2. Free by law for everybody -----------------------------
            if (request.DocumentType == DocumentType.CertificateOfIndigency)
            {
                assessment.FinalFee = 0m;
                assessment.IsExempt = true;
                assessment.Category = "Free by law";
                assessment.Basis = "FREE - Certificate of Indigency is issued without charge. "
                                 + "DILG Memorandum Circular 2019-177.";
                return assessment;
            }

            if (request.DocumentType == DocumentType.CertificateOfLowIncome)
            {
                assessment.FinalFee = 0m;
                assessment.IsExempt = true;
                assessment.Category = "Free by law";
                assessment.Basis = "FREE - Certificate of Low Income is issued without charge, "
                                 + "as with the certificate of indigency. DILG MC 2019-177.";
                return assessment;
            }

            // ---- 3. Personal waivers --------------------------------------
            string waiver = PersonalWaiverReason(resident, request.DocumentType);
            if (waiver != null)
            {
                assessment.FinalFee = 0m;
                assessment.IsExempt = true;
                assessment.Category = "Free by law";
                assessment.Basis = "FREE - " + waiver;
                return assessment;
            }

            // ---- 4. The once-only jobseeker waiver ------------------------
            string jobseekerReason;
            if (request.ApplyJobseekerWaiver
                && IsJobseekerEligible(resident, out jobseekerReason)
                && IsCoveredByJobseekerAct(request.DocumentType))
            {
                assessment.FinalFee = 0m;
                assessment.IsExempt = true;
                assessment.Category = "Free by law";
                assessment.Basis = "FREE - RA 11261 (First Time Jobseekers Assistance Act), "
                                 + "first availment and at least "
                                 + AppConfig.JobseekerResidencyMonths + " months of residency.";
                return assessment;
            }

            // ---- 5. The student fee category ------------------------------
            if (resident.IsStudentFeeCategory
                && AppConfig.StudentDiscountEnabled
                && IsStudentDocument(request.DocumentType)
                && baseFee > 0m)
            {
                int percent = Clamp(AppConfig.StudentDiscountPercent, 0, 100);
                decimal discounted = Round(baseFee - (baseFee * percent / 100m));

                assessment.FinalFee = discounted;
                assessment.StudentDiscountApplied = true;
                assessment.Category = "Student";
                assessment.Basis = "Student fee category - " + percent + "% off the "
                                 + Display(StandardRateName(request)) + " of P"
                                 + baseFee.ToString("#,##0.00") + " (barangay ordinance). "
                                 + "Being a student is a fee category in this system, not a "
                                 + "classification, so it only ever changes the price.";
                return assessment;
            }

            // ---- nothing special: the standard rate ------------------------
            assessment.Category = "Regular";
            assessment.Basis = Display(StandardRateName(request)) + " of P"
                             + baseFee.ToString("#,##0.00") + " under the barangay's Citizen's Charter.";
            return assessment;
        }

        /// <summary>The assessment for a request filed with no particular
        /// options, used by the sample data and the rule checks.</summary>
        public FeeAssessment Assess(Resident resident, DocumentType type)
        {
            DocumentRequest request = new DocumentRequest();
            request.DocumentType = type;
            request.Scope = ClearanceScope.Local;
            return Assess(resident, request);
        }

        // ==================================================================
        //  Standard rates
        // ==================================================================

        private decimal StandardFeeFor(DocumentRequest request)
        {
            switch (request.DocumentType)
            {
                case DocumentType.BarangayClearance:
                    return request.Scope == ClearanceScope.Abroad
                        ? AppConfig.FeeBarangayClearanceAbroad
                        : AppConfig.FeeBarangayClearanceLocal;

                case DocumentType.CertificateOfResidency:
                case DocumentType.CertificateOfGoodMoralCharacter:
                case DocumentType.EmploymentCertification:
                case DocumentType.AcceptanceCertificate:
                case DocumentType.SoloParentCertification:
                case DocumentType.MedicalAssistanceCertification:
                case DocumentType.FinancialAssistanceCertification:
                case DocumentType.BurialAssistanceCertification:
                case DocumentType.IpScholarshipCertification:
                case DocumentType.FourPsScholarshipCertification:
                case DocumentType.GadRelatedDocumentation:
                case DocumentType.CsoDocumentation:
                case DocumentType.OtherCertification:
                    return AppConfig.FeeCertification;

                // A barangay ID and the documents that only record an incident
                // are issued without charge.
                case DocumentType.BarangayID:
                case DocumentType.BlotterRelatedIncident:
                    return 0m;

                case DocumentType.FirstTimeJobseekerCertificate:
                    // Free by law for an eligible first-time jobseeker; if the
                    // resident is not eligible the request is refused earlier,
                    // so the rate here is the ordinary certification rate.
                    return AppConfig.FeeCertification;

                case DocumentType.LuponCaseFiling:
                    return AppConfig.FeeLuponFiling;

                case DocumentType.BarangayFacilityRental:
                    return request.Hours > 0m
                        ? Round(request.Hours * AppConfig.FeeFacilityPerHour)
                        : AppConfig.FeeFacilityPerHour;

                case DocumentType.CommunityTaxCertificate:
                    return CommunityTax(request.GrossAnnualIncome);

                case DocumentType.OtherTarifaProcessingFee:
                    return request.AssessedAmount > 0m
                        ? Round(request.AssessedAmount)
                        : AppConfig.FeeCertification;

                default:
                    return AppConfig.FeeCertification;
            }
        }

        /// <summary>
        /// The community tax for an individual, the way RA 7160 Sec. 156
        /// writes it: a basic tax plus one peso for every thousand pesos of
        /// declared gross annual income, with the additional part capped.
        /// </summary>
        public decimal CommunityTax(decimal grossAnnualIncome)
        {
            if (grossAnnualIncome <= 0m) return AppConfig.CommunityTaxBase;

            decimal additional = Round((grossAnnualIncome / 1000m) * AppConfig.CommunityTaxPerThousand);
            if (AppConfig.CommunityTaxCap > 0m && additional > AppConfig.CommunityTaxCap)
                additional = AppConfig.CommunityTaxCap;

            return AppConfig.CommunityTaxBase + additional;
        }

        private static string StandardRateName(DocumentRequest request)
        {
            switch (request.DocumentType)
            {
                case DocumentType.BarangayClearance:
                    return request.Scope == ClearanceScope.Abroad
                        ? "Barangay Clearance (for work abroad)"
                        : "Barangay Clearance (local)";

                case DocumentType.CertificateOfResidency: return "Certificate of Residency";
                case DocumentType.CertificateOfGoodMoralCharacter: return "Certificate of Good Moral Character";
                case DocumentType.EmploymentCertification: return "Employment Certification";
                case DocumentType.CommunityTaxCertificate: return "Community Tax Certificate (cedula)";
                case DocumentType.LuponCaseFiling: return "Katarungang Pambarangay filing fee";
                case DocumentType.BarangayFacilityRental: return "Barangay facility rental";
                case DocumentType.OtherTarifaProcessingFee: return "Barangay Taripa processing fee";
                default: return EnumText.Spaced(request.DocumentType.ToString());
            }
        }

        // ==================================================================
        //  Waivers and discounts
        // ==================================================================

        /// <summary>
        /// The personal waivers, in the order the law gives them.
        ///
        /// Only the documents a waiver can cover are listed here. A cedula, a
        /// filing fee and a facility rental are not covered by a senior
        /// citizen's discount - they are taxes and fees for a specific act,
        /// not a charge for a document - so they are excluded on purpose.
        /// </summary>
        private static string PersonalWaiverReason(Resident resident, DocumentType type)
        {
            if (!IsDocumentWaivable(type)) return null;

            if (resident.HasClassification(ResidentClassification.SeniorCitizen))
                return "RA 9994 (Expanded Senior Citizens Act) - all barangay document fees "
                     + "are waived for a senior citizen.";

            if (resident.HasClassification(ResidentClassification.PWD))
                return "RA 10754 (Expanding the Benefits of Persons with Disability) - "
                     + "document fees are waived for a person with disability.";

            if (resident.HasClassification(ResidentClassification.Indigent))
                return "Indigent resident - document fees are waived (DILG MC 2019-177).";

            if (resident.HasClassification(ResidentClassification.SoloParent))
                return "Solo parent - document fees are waived for the certifications a solo "
                     + "parent needs (RA 8972, as expanded).";

            if (resident.HasClassification(ResidentClassification.FourPsBeneficiary))
                return "4Ps beneficiary household - document fees are waived (DILG guidance "
                     + "for Pantawid Pamilyang Pilipino Program beneficiaries).";

            return null;
        }

        private static bool IsDocumentWaivable(DocumentType type)
        {
            switch (type)
            {
                case DocumentType.CommunityTaxCertificate:
                case DocumentType.LuponCaseFiling:
                case DocumentType.BarangayFacilityRental:
                case DocumentType.OtherTarifaProcessingFee:
                case DocumentType.BarangayBusinessClearance:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>The documents the student fee category covers. It comes
        /// from App.config, so the barangay can extend it without me.</summary>
        public bool IsStudentDocument(DocumentType type)
        {
            string list = AppConfig.StudentDiscountDocuments;
            if (string.IsNullOrWhiteSpace(list)) return false;

            foreach (string piece in list.Split(','))
                if (string.Equals(piece.Trim(), type.ToString(), StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>The documents RA 11261 can cover. The law is about the
        /// clearances and certifications a first-time jobseeker needs to get
        /// hired, so the business clearance and the money documents are out.</summary>
        public static bool IsCoveredByJobseekerAct(DocumentType type)
        {
            switch (type)
            {
                case DocumentType.BarangayClearance:
                case DocumentType.CertificateOfResidency:
                case DocumentType.CertificateOfGoodMoralCharacter:
                case DocumentType.FirstTimeJobseekerCertificate:
                case DocumentType.EmploymentCertification:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether this resident can still use RA 11261, and if not, why not.
        ///
        /// Two conditions: at least six months in the barangay, and the benefit
        /// never used before. Both come from App.config and from the resident's
        /// own record. I return the reason as text because the dialog shows it
        /// to the clerk instead of just greying out a button.
        /// </summary>
        public bool IsJobseekerEligible(Resident resident, out string reason)
        {
            if (resident == null)
            {
                reason = "There is no resident on the request.";
                return false;
            }

            if (resident.HasAvailedFirstTimeJobseeker)
            {
                reason = "This resident has already used the RA 11261 first-time jobseeker benefit. "
                       + "It may be availed once only.";
                return false;
            }

            int months = resident.GetMonthsOfResidency();
            int required = AppConfig.JobseekerResidencyMonths;
            if (months < required)
            {
                reason = "The resident has been here " + months + " month(s). RA 11261 needs at least "
                       + required + " months of residency in the barangay.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        // ==================================================================
        //  Reading a fee back to a person
        // ==================================================================

        /// <summary>What the clerk's screen is allowed to show: the amount, and
        /// one short label. No sentence of law - that is internal.</summary>
        public static string DescribeForClerk(FeeAssessment assessment)
        {
            if (assessment == null) return string.Empty;
            if (assessment.IsFree) return "Free";
            return "P" + assessment.FinalFee.ToString("#,##0.00");
        }

        /// <summary>What the admin sees in the log and the reports.</summary>
        public static string DescribeForAudit(FeeAssessment assessment)
        {
            if (assessment == null) return string.Empty;
            if (assessment.IsFree) return "Free - " + assessment.Basis;
            return "P" + assessment.FinalFee.ToString("#,##0.00") + " - " + assessment.Basis;
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }

        /// <summary>The whole schedule on one screen, for the Reports screen's
        /// "fee schedule" view. The barangay posts its Citizen's Charter, so
        /// the system can print the same list.</summary>
        public IList<string[]> GetScheduleTable()
        {
            List<string[]> rows = new List<string[]>();

            rows.Add(new string[] { "Barangay Clearance (local)", Money(AppConfig.FeeBarangayClearanceLocal), "Citizen's Charter" });
            rows.Add(new string[] { "Barangay Clearance (work abroad)", Money(AppConfig.FeeBarangayClearanceAbroad), "Citizen's Charter" });
            rows.Add(new string[] { "Certificate of Residency / Good Moral / others", Money(AppConfig.FeeCertification), "Citizen's Charter" });
            rows.Add(new string[] { "Certificate of Indigency", "Free", "DILG MC 2019-177" });
            rows.Add(new string[] { "Certificate of Low Income", "Free", "DILG MC 2019-177" });
            rows.Add(new string[] { "Barangay Business Clearance", Money(AppConfig.FeeBusinessClearance), "Citizen's Charter; RA 7160 Sec. 152" });
            rows.Add(new string[] { "Community Tax Certificate (cedula)", "Computed", "RA 7160 Sec. 156" });
            rows.Add(new string[] { "Katarungang Pambarangay filing", Money(AppConfig.FeeLuponFiling), "Citizen's Charter" });
            rows.Add(new string[] { "Barangay facility rental (per hour)", Money(AppConfig.FeeFacilityPerHour), "Citizen's Charter" });
            rows.Add(new string[] { "Student fee category", AppConfig.StudentDiscountPercent + "% off", "Barangay ordinance" });

            return rows;
        }

        private static string Money(decimal value)
        {
            return "P" + value.ToString("#,##0.00");
        }
    }
}
