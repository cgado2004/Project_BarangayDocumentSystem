// ---------------------------------------------------------------------------
//  SampleData.cs - the demo barangay, so the screens are not empty.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Data.Sql;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Database
{
    /// <summary>
    /// Loads a small but realistic set of records: fourteen purok households,
    /// dependents, requests in every state of the queue, official receipts with
    /// control numbers, and an activity log with something in it.
    ///
    /// I wrote this so that whoever opens the program for the first time - my
    /// panel, my group-mates, the barangay clerk during the demo - sees a
    /// working system instead of empty grids. It only runs against a database
    /// with no residents in it, so it can never land on top of real records.
    ///
    /// The names, puroks and businesses are made up. The puroks are the real
    /// ones from the barangay's own list.
    /// </summary>
    public static class SampleData
    {
        public static void Seed(DBContext context, IClock clock)
        {
            if (context == null) throw new ArgumentNullException("context");

            IBarangayRepository repository = new SqlBarangayRepository(context);
            Seed(repository, clock);
        }

        /// <summary>The same seeding for any store, which is what the
        /// in-memory demo and the rule checks use.</summary>
        public static void Seed(IBarangayRepository repository, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (clock == null) clock = new SystemClock();

            DateTime now = clock.Now();

            FeeSchedule fees = new FeeSchedule();
            TimeWindowPolicy window = new TimeWindowPolicy();

            List<Resident> residents = BuildResidents(repository, now);
            SeedSeries(repository, now);
            SeedRequests(repository, residents, fees, window, now);
            SeedActivity(repository, now);
        }

        // ==================================================================
        //  Residents
        // ==================================================================

        private static List<Resident> BuildResidents(IBarangayRepository repository, DateTime now)
        {
            List<Resident> created = new List<Resident>();

            // ---- one household per purok, with the dependents registered ----
            AddResident(repository, created, now, "Marites", "Bautista", "Santos", "", "Purok Orchids",
                new DateTime(1974, 3, 12), Gender.Female, CivilStatus.Married, "0917-220-1144",
                "Sari-sari store owner", new DateTime(2009, 6, 1), true,
                ResidentClassification.None, false, true, true,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Rodolfo", "Aguilar", "Dizon", "Sr.", "Purok Talisay",
                new DateTime(1958, 11, 2), Gender.Male, CivilStatus.Married, "0918-334-7781",
                "Retired government employee", new DateTime(1998, 1, 15), true,
                ResidentClassification.SeniorCitizen, false, false, true,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Liza", "Mendoza", "Reyes", "", "Purok Sampaguita",
                new DateTime(2004, 7, 19), Gender.Female, CivilStatus.Single, "0995-118-2267",
                "College student", new DateTime(2018, 8, 1), false,
                ResidentClassification.PWD, true, false, false,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Joel", "Pascual", "Villanueva", "", "Purok Sunflower",
                new DateTime(1985, 1, 27), Gender.Male, CivilStatus.Married, "0920-771-9093",
                "Tricycle driver", new DateTime(2021, 2, 10), true,
                ResidentClassification.Indigent | ResidentClassification.FourPsBeneficiary, false, false, true,
                ResidencyStatus.Temporary, now);

            AddResident(repository, created, now, "Analyn", "Cruz", "Marquez", "", "Purok Cristo Rey",
                new DateTime(1990, 9, 5), Gender.Female, CivilStatus.Single, "0906-556-3320",
                "Market vendor", new DateTime(2024, 11, 20), true,
                ResidentClassification.SoloParent, false, true, true,
                ResidencyStatus.Newcomer, now);

            AddResident(repository, created, now, "Eduardo", "Lim", "Tan", "Jr.", "Purok Arellano",
                new DateTime(1979, 5, 30), Gender.Male, CivilStatus.Married, "0917-909-4412",
                "Hardware owner", new DateTime(2012, 4, 3), true,
                ResidentClassification.None, false, true, true,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Grace", "Alonzo", "Ferolino", "", "Purok Dagohoy",
                new DateTime(1996, 12, 14), Gender.Female, CivilStatus.Married, "0935-221-7788",
                "Public school teacher", new DateTime(2022, 6, 6), true,
                ResidentClassification.None, false, false, true,
                ResidencyStatus.Temporary, now);

            AddResident(repository, created, now, "Nestor", "Ilagan", "Bucoy", "", "Purok Tindalo",
                new DateTime(1967, 8, 21), Gender.Male, CivilStatus.Widowed, "0921-455-0087",
                "Fisherman", new DateTime(2001, 3, 19), false,
                ResidentClassification.SeniorCitizen | ResidentClassification.PWD, false, false, true,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Kimberly", "Ramos", "Otaza", "", "Purok Lapu-Lapu",
                new DateTime(2007, 2, 9), Gender.Female, CivilStatus.Single, "0998-220-1176",
                "Senior high school student", new DateTime(2015, 5, 25), false,
                ResidentClassification.Indigent, true, false, false,
                ResidencyStatus.Permanent, now);

            AddResident(repository, created, now, "Danilo", "Bacani", "Eusebio", "", "Purok Calachuchi",
                new DateTime(1971, 6, 3), Gender.Male, CivilStatus.Married, "0917-118-6623",
                "Barangay health worker", new DateTime(1995, 7, 1), true,
                ResidentClassification.FourPsBeneficiary, false, false, true,
                ResidencyStatus.Permanent, now);

            return created;
        }

        private static void AddResident(IBarangayRepository repository, List<Resident> created, DateTime now,
                                        string firstName, string middleName, string lastName, string suffix,
                                        string purok, DateTime birth, Gender gender, CivilStatus civilStatus,
                                        string contact, string occupation, DateTime residencyStart,
                                        bool voter, ResidentClassification classification,
                                        bool isStudent, bool isBusinessOwner, bool isHeadOfFamily,
                                        ResidencyStatus residencyStatus, DateTime when)
        {
            Resident resident = new Resident();
            resident.FirstName = firstName;
            resident.MiddleName = middleName;
            resident.LastName = lastName;
            resident.Suffix = suffix;
            resident.DateOfBirth = birth;
            resident.Gender = gender;
            resident.CivilStatus = civilStatus;
            resident.Purok = purok;
            resident.ContactNumber = contact;
            resident.Occupation = occupation;
            resident.DateOfResidency = residencyStart;
            resident.IsRegisteredVoter = voter;
            resident.Classification = classification;
            resident.IsStudentFeeCategory = isStudent;
            resident.IsBusinessOwner = isBusinessOwner;
            resident.IsHeadOfFamily = isHeadOfFamily;
            resident.ResidencyStatus = residencyStatus;
            resident.CreatedOn = when.AddMonths(-10);
            resident.CreatedBy = "sample";

            repository.InsertResident(resident);
            created.Add(resident);

            // The household: two or three dependents under the heads of family.
            if (!isHeadOfFamily) return;

            AddDependent(repository, resident, "Ramon " + lastName, DependentRelation.Spouse, birth.AddYears(1), false, when);
            AddDependent(repository, resident, "Trisha " + lastName, DependentRelation.Daughter, now.AddYears(-9), true, when);
            AddDependent(repository, resident, "Miguel " + lastName, DependentRelation.Son, now.AddYears(-15), true, when);
        }

        private static void AddDependent(IBarangayRepository repository, Resident head, string name,
                                        DependentRelation relation, DateTime birth, bool studying, DateTime when)
        {
            Dependent dependent = new Dependent();
            dependent.HeadResidentId = head.ResidentId;
            dependent.FullName = name;
            dependent.Relation = relation;
            dependent.DateOfBirth = birth;
            dependent.IsStudying = studying;
            dependent.CreatedOn = when.AddMonths(-10);
            dependent.CreatedBy = "sample";

            repository.InsertDependent(dependent);
            head.AddDependent(dependent);
        }

        // ==================================================================
        //  Receipt booklets
        // ==================================================================

        private static void SeedSeries(IBarangayRepository repository, DateTime now)
        {
            if (repository.GetReceiptSeries(false).Count > 0) return;

            ReceiptSeries series = new ReceiptSeries();
            series.SeriesCode = "A";
            series.ControlFrom = "0004501";
            series.ControlTo = "0004700";
            series.IssuedTo = "Barangay Treasurer";
            series.IssuedOn = new DateTime(now.Year, 1, 5);
            series.IsActive = true;
            series.Remarks = "Sample booklet for the demonstration";

            repository.InsertReceiptSeries(series);
        }

        // ==================================================================
        //  Requests, in every state the queue can be in
        // ==================================================================

        private static void SeedRequests(IBarangayRepository repository, List<Resident> residents,
                                         FeeSchedule fees, TimeWindowPolicy window, DateTime now)
        {
            if (residents.Count < 10) return;

            // ---- released, paid, with a receipt ------------------------------
            DocumentRequest clearance = Build(repository, residents[0], DocumentType.BarangayClearance,
                "Local employment requirement", now.AddDays(-9).Date.AddHours(9.5),
                false, window, fees, "sample");

            clearance.RecordPayment(clearance.Fee, "OR-" + now.Year + "-000451", "0004501", "sample", now.AddDays(-9).AddHours(1));
            clearance.Clear("Filed inside office hours, no validation needed", "sample", now.AddDays(-9).AddHours(9.6));
            clearance.MarkReadyForRelease("sample", now.AddDays(-9).AddHours(11));
            clearance.Release(residents[0].GetFullName(), "sample", now.AddDays(-9).AddHours(14));
            repository.UpdateRequest(clearance);

            OfficialReceipt first = BuildReceipt(clearance, "OR-" + now.Year + "-000451", "A", "0004501",
                residents[0].GetFullName(), clearance.Fee, now.AddDays(-9), "sample");
            repository.InsertReceipt(first);

            // ---- free indigency certificate, released ------------------------
            DocumentRequest indigency = Build(repository, residents[3], DocumentType.CertificateOfIndigency,
                "Medical assistance at the provincial hospital", now.AddDays(-6).Date.AddHours(10),
                false, window, fees, "sample");

            indigency.Clear("Filed inside office hours, no validation needed", "sample", now.AddDays(-6).AddHours(10.1));
            indigency.MarkReadyForRelease("sample", now.AddDays(-6).AddHours(11));
            indigency.Release(residents[3].GetFullName(), "sample", now.AddDays(-6).AddHours(13));
            repository.UpdateRequest(indigency);

            // ---- senior clearance, waived, released --------------------------
            DocumentRequest senior = Build(repository, residents[1], DocumentType.BarangayClearance,
                "Senior citizen discount application", now.AddDays(-4).Date.AddHours(8.25),
                false, window, fees, "sample");

            senior.Clear("Filed inside office hours, no validation needed", "sample", now.AddDays(-4).AddHours(8.3));
            senior.MarkReadyForRelease("sample", now.AddDays(-4).AddHours(9));
            senior.Release(residents[1].GetFullName(), "sample", now.AddDays(-4).AddHours(10));
            repository.UpdateRequest(senior);

            // ---- business clearance, waiting for validation ------------------
            DocumentRequest business = Build(repository, residents[0], DocumentType.BarangayBusinessClearance,
                "Renewal of the sari-sari store permit", now.AddDays(-1).Date.AddHours(9),
                true, window, fees, "sample");

            // The business details hang off the request, not off the resident:
            // the same person can open a second store next year and the first
            // clearance must still show the first store's details.
            BusinessDetails store = new BusinessDetails();
            store.BusinessName = "Aling Marites Sari-Sari Store";
            store.NatureOfBusiness = "Retail - food and household items";
            store.Purok = residents[0].Purok;
            store.LocationNote = "Beside the covered court, stall 4";
            store.OwnershipType = "Sole Proprietor";
            store.RegistrationNumber = "DTI-2024-118823";
            store.EmployeeCount = 2;
            store.IsRenewal = true;
            business.Business = store;

            business.SendToProcessing("Business clearances are inspected before the barangay signs", "sample",
                now.AddDays(-1).AddHours(9.1));
            repository.UpdateRequest(business);

            // ---- filed after 4:00 PM, waiting for the next window ------------
            DocumentRequest afterHours = Build(repository, residents[2], DocumentType.CertificateOfResidency,
                "Bank account opening", now.Date.AddHours(16).AddMinutes(20),
                false, window, fees, "sample");

            repository.UpdateRequest(afterHours);

            // ---- rejected, with the reason on the record ---------------------
            DocumentRequest rejected = Build(repository, residents[4], DocumentType.FirstTimeJobseekerCertificate,
                "First job application", now.AddDays(-3).Date.AddHours(11),
                false, window, fees, "sample");
            rejected.Reject("The resident has not yet completed six months in the barangay.", "sample",
                now.AddDays(-3).AddHours(11.2));
            repository.UpdateRequest(rejected);

            // ---- today, still pending ---------------------------------------
            DocumentRequest today = Build(repository, residents[9], DocumentType.CertificateOfGoodMoralCharacter,
                "Scholarship application", now.Date.AddHours(9).AddMinutes(45),
                false, window, fees, "sample");
            repository.UpdateRequest(today);

            // ---- a certificate issued free under the jobseeker law -----------
            DocumentRequest jobseekerResident = residents[8];
            DocumentRequest jobseeker = Build(repository, jobseekerResident, DocumentType.BarangayClearance,
                "First time jobseeker requirement under RA 11261", now.AddDays(-20).Date.AddHours(10),
                false, window, fees, "sample");

            jobseeker.AddJobseekerWaiver("FREE - RA 11261 (First Time Jobseekers Assistance Act)", 100m);
            jobseeker.SetAssessment(0m, "FREE - RA 11261 (First Time Jobseekers Assistance Act)");
            jobseeker.Clear("Verified as a first-time jobseeker", "sample", now.AddDays(-20).AddHours(10.2));
            jobseeker.MarkReadyForRelease("sample", now.AddDays(-20).AddHours(11));
            jobseeker.Release(jobseekerResident.GetFullName(), "sample", now.AddDays(-20).AddHours(13));
            jobseekerResident.HasAvailedFirstTimeJobseeker = true;
            jobseeker.MarkJobseekerBenefitUsed();
            repository.UpdateRequest(jobseeker);
            repository.UpdateResident(jobseekerResident);

            // ---- one yesterday, already paid and waiting on the counter ------
            DocumentRequest ready = Build(repository, residents[5], DocumentType.CertificateOfResidency,
                "Local employment", now.AddDays(-1).Date.AddHours(13),
                false, window, fees, "sample");

            ready.RecordPayment(ready.Fee, "OR-" + now.Year + "-000452", "0004502", "sample", now.AddDays(-1).AddHours(13.5));
            ready.Clear("Filed inside office hours, no validation needed", "sample", now.AddDays(-1).AddHours(13.1));
            ready.MarkReadyForRelease("sample", now.AddDays(-1).AddHours(14));
            repository.UpdateRequest(ready);

            OfficialReceipt second = BuildReceipt(ready, "OR-" + now.Year + "-000452", "A", "0004502",
                residents[5].GetFullName(), ready.Fee, now.AddDays(-1), "sample");
            repository.InsertReceipt(second);
        }

        /// <summary>
        /// Files a request the way the real service does: the fee schedule
        /// decides the price, and the clock decides whether it starts as
        /// Cleared or Pending.
        ///
        /// I deliberately route the sample data through the same rules instead
        /// of typing finished rows. If the 4:00 PM rule is ever changed, the
        /// sample data changes with it, and the demo can never show a state the
        /// real system would not produce.
        /// </summary>
        private static DocumentRequest Build(IBarangayRepository repository, Resident resident,
                                            DocumentType type, string purpose, DateTime filedAt,
                                            bool needsValidation, TimeWindowPolicy window,
                                            FeeSchedule fees, string filedBy)
        {
            DocumentRequest request = new DocumentRequest();
            request.ReferenceNumber = repository.NextReferenceNumber(filedAt);
            request.ResidentId = resident.ResidentId;
            request.ResidentName = resident.GetFullName();
            request.DocumentType = type;
            request.Purpose = purpose;
            request.DateRequested = filedAt;
            request.RequiresValidation = needsValidation;
            request.FiledDuringOfficeWindow = window.IsInsideOfficeWindow(filedAt);
            request.Scope = ClearanceScope.Local;

            FeeAssessment assessment = fees.Assess(resident, request);
            request.SetAssessment(assessment.FinalFee, assessment.Basis);

            RequestStatus status = window.SuggestStartingStatus(request, false);
            request.SetInitialStatus(status, window.ExplainStartingStatus(request, status), filedBy, filedAt);

            repository.InsertRequest(request);
            resident.AddRequest(request);
            return request;
        }

        private static OfficialReceipt BuildReceipt(DocumentRequest request, string orNumber, string series,
                                                    string control, string payer, decimal amount,
                                                    DateTime when, string collector)
        {
            OfficialReceipt receipt = new OfficialReceipt();
            receipt.OrNumber = orNumber;
            receipt.SeriesCode = series;
            receipt.ControlNumber = control;
            receipt.OrDate = when.Date;
            receipt.PayerName = payer;
            receipt.Amount = amount;
            receipt.Method = PaymentMethod.Cash;
            receipt.RequestId = request.RequestId;
            receipt.CollectedBy = collector;
            receipt.CreatedOn = when;
            return receipt;
        }

        private static void SeedActivity(IBarangayRepository repository, DateTime now)
        {
            repository.AppendActivityLog(ActivityLogEntry.Create("admin", UserRole.Administrator,
                ActivityModule.Settings, "Prepared", "System", string.Empty,
                "Loaded the sample barangay data so the screens are not empty", now.AddMinutes(-8), "sample"));
        }
    }
}
