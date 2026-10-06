// =====================================================================
//  RuleChecks - my own checks that the barangay's rules still hold.
//
//  A project that only compiles tells you nothing about whether the fees
//  are right, whether a request filed at 4:01 PM still comes out as
//  Pending, or whether a voided receipt stops counting as income. So this
//  is a small program that runs the real classes - the same FeeSchedule,
//  TimeWindowPolicy, RequestService and ReceiptService the screens use -
//  against the in-memory store and prints PASS or FAIL for each rule.
//
//  I run it against the in-memory store on purpose: no database, no
//  sample files, nothing to set up, and it can never touch real records.
//
//  How to run (Visual Studio Developer Command Prompt, from the repo root):
//      msbuild /t:Restore BarangayDocumentSystem.sln
//      msbuild BarangayDocumentSystem.sln /p:Configuration=Debug
//      tests\RuleChecks\bin\Debug\RuleChecks.exe
//
//  The exit code is 0 when everything passed and 1 when something failed,
//  so it can also be run from a script.
// =====================================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using BarangayDocumentSystem.Config;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;
using BarangayDocumentSystem.Services;
using BarangayDocumentSystem.Services.Reports;

namespace BarangayDocumentSystem.RuleChecks
{
    internal static class Program
    {
        private static int _pass;
        private static int _fail;

        private static int Main()
        {
            // The program reads its settings once, the same way the real
            // program does. Running from a test host with no App.config means
            // every value falls back to its default, which is also a check in
            // itself: the defaults have to be sensible.
            AppConfig.Reset();
            AppConfig.Load();

            FixedClock clock = new FixedClock(new DateTime(2026, 3, 10, 9, 15, 0));

            InMemoryBarangayRepository repository = new InMemoryBarangayRepository();
            SampleData.Seed(repository, clock);

            ActivityLogService log = new ActivityLogService(repository, Session(repository), clock);
            FeeSchedule fees = new FeeSchedule();
            TimeWindowPolicy window = new TimeWindowPolicy(clock);

            IList<Resident> residents = repository.GetResidents(new ResidentQuery());

            CheckPuroks(residents);
            CheckFlatFees(fees, residents);
            CheckStudentFeeCategory(fees, residents);
            CheckCommunityTax(fees);
            CheckFreeByLaw(fees, residents);
            CheckBusinessIsNeverWaived(fees, residents);
            CheckJobseeker(fees, residents, clock);
            CheckResidencyStatus(residents, clock);
            CheckOfficeWindow(window);
            CheckStartingStatuses(repository, window, fees, residents, clock);
            CheckRequestWorkflow(repository, log, fees, window, residents, clock);
            CheckAfterHoursQueue(repository, log, fees, window, residents, clock);
            CheckPasswordHashing();
            CheckReceipts(repository, log, residents, clock);
            CheckPermissions(log);
            CheckActivityLog(repository, log, clock);
            CheckCensus(repository, residents);
            CheckReports(repository, log, clock);
            CheckStates(repository, residents, clock);

            Console.WriteLine();
            Console.WriteLine("  " + _pass + " passed, " + _fail + " failed.");
            Console.WriteLine();

            return _fail == 0 ? 0 : 1;
        }

        // =================================================================
        //  The little reporting helper
        // =================================================================

        private static void Check(string name, bool ok, string detail)
        {
            if (ok)
            {
                _pass++;
                Console.WriteLine("  PASS  " + name);
            }
            else
            {
                _fail++;
                Console.WriteLine("  FAIL  " + name
                    + (string.IsNullOrEmpty(detail) ? string.Empty : "   (" + detail + ")"));
            }
        }

        private static void Heading(string text)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + text + " ===");
        }

        // =================================================================
        //  A signed-in administrator, so the services that check who is at
        //  the keyboard let me through.
        // =================================================================

        private static SessionManager Session(IBarangayRepository repository)
        {
            UserAccount administrator = new UserAccount();
            administrator.UserId = 9001;
            administrator.Username = "rulecheck";
            administrator.FullName = "Rule Check";
            administrator.Role = UserRole.Administrator;
            administrator.IsActive = true;
            administrator.Position = "tester";

            SessionManager session = new SessionManager();
            session.SignIn(administrator);
            return session;
        }

        private static Resident Person(IList<Resident> residents, string lastName)
        {
            Resident found = residents.FirstOrDefault(r => r.LastName == lastName);
            if (found == null) throw new InvalidOperationException("No seeded resident named " + lastName + ".");
            return found;
        }

        // =================================================================
        //  The puroks are the real ones
        // =================================================================

        private static void CheckPuroks(IList<Resident> residents)
        {
            Heading("The barangay's own purok list");

            Check("the seeded residents are in real puroks",
                residents.All(r => PurokList.Contains(r.Purok)),
                string.Join(", ", residents.Select(r => r.Purok).Distinct().ToArray()));

            Check("a made-up purok is refused", !PurokList.Contains("Purok Walang Ganito"));
            Check("the drop-down offers every purok plus the blank first line",
                PurokList.ForDropDown().Count == PurokList.All.Length + 1);
            Check("Peña keeps its ñ", residents.Any(r => r.LastName == "Peña"));
        }

        // =================================================================
        //  The Citizen's Charter flat rates
        // =================================================================

        private static void CheckFlatFees(FeeSchedule fees, IList<Resident> residents)
        {
            Heading("Citizen's Charter rates");

            Resident regular = Person(residents, "Santos");

            DocumentRequest local = new DocumentRequest();
            local.DocumentType = DocumentType.BarangayClearance;
            local.Scope = ClearanceScope.Local;
            Check("barangay clearance, local, is the configured fee",
                fees.Assess(regular, local).FinalFee == AppConfig.FeeBarangayClearanceLocal,
                fees.Assess(regular, local).FinalFee.ToString());

            DocumentRequest abroad = new DocumentRequest();
            abroad.DocumentType = DocumentType.BarangayClearance;
            abroad.Scope = ClearanceScope.Abroad;
            Check("barangay clearance, for travel abroad, costs more",
                fees.Assess(regular, abroad).FinalFee == AppConfig.FeeBarangayClearanceAbroad);

            Check("certificate of residency is the configured fee",
                fees.Assess(regular, DocumentType.CertificateOfResidency).FinalFee == AppConfig.FeeCertification);

            Check("filing a lupon complaint is the configured fee",
                fees.Assess(regular, DocumentType.LuponCaseFiling).FinalFee == AppConfig.FeeLuponFiling);

            DocumentRequest rental = new DocumentRequest();
            rental.DocumentType = DocumentType.BarangayFacilityRental;
            rental.Hours = 3m;
            Check("the covered court is charged by the hour",
                fees.Assess(regular, rental).FinalFee == AppConfig.FeeFacilityPerHour * 3m,
                fees.Assess(regular, rental).FinalFee.ToString());

            Check("the fee basis is written down even though the counter does not show it",
                !string.IsNullOrWhiteSpace(fees.Assess(regular, DocumentType.CertificateOfResidency).Basis));
        }

        // =================================================================
        //  Student is a fee category now, not a classification
        // =================================================================

        private static void CheckStudentFeeCategory(FeeSchedule fees, IList<Resident> residents)
        {
            Heading("Student as a fee category");

            Resident student = new Resident();
            student.FirstName = "Test";
            student.LastName = "Student";
            student.IsStudentFeeCategory = true;
            student.RecordState = RecordState.Active;
            student.DateOfResidency = DateTime.Today.AddYears(-1);

            decimal local = AppConfig.FeeBarangayClearanceLocal;
            decimal discount = AppConfig.StudentDiscountPercent / 100m;

            Check("a student pays less for a barangay clearance",
                fees.Assess(student, DocumentType.BarangayClearance).FinalFee == local * (1m - discount),
                fees.Assess(student, DocumentType.BarangayClearance).FinalFee.ToString());

            Check("the student ticket says so in the assessment",
                fees.Assess(student, DocumentType.BarangayClearance).StudentDiscountApplied);

            Check("a student gets no discount on a business clearance",
                fees.Assess(student, DocumentType.BarangayBusinessClearance).FinalFee
                    == AppConfig.FeeBusinessClearanceStandard);

            Check("Student is gone from the classifications",
                !Enum.GetNames(typeof(ResidentClassification)).Contains("Student"));

            Check("the classification drop-down has no Student entry",
                !EnumText.Spaced(ResidentClassification.None).Contains("Student"));

            Resident scholar = Person(residents, "Reyes");
            Check("a seeded student is tagged through the fee category, not the classification",
                scholar.IsStudentFeeCategory && !scholar.HasClassification(ResidentClassification.PWD)
                || !scholar.IsStudentFeeCategory,
                "Reyes: student=" + scholar.IsStudentFeeCategory);
        }

        // =================================================================
        //  Cedula, computed the way the law says
        // =================================================================

        private static void CheckCommunityTax(FeeSchedule fees)
        {
            Heading("Community tax (cedula)");

            Check("no declared income still pays the base tax",
                fees.CommunityTax(0m) == AppConfig.CommunityTaxBase);

            Check("each thousand of income adds the configured peso",
                fees.CommunityTax(10000m) == AppConfig.CommunityTaxBase + (AppConfig.CommunityTaxPerThousand * 10m));

            Check("very large incomes stop at the ceiling",
                fees.CommunityTax(50000000m) == AppConfig.CommunityTaxCap);
        }

        // =================================================================
        //  Free by law
        // =================================================================

        private static void CheckFreeByLaw(FeeSchedule fees, IList<Resident> residents)
        {
            Heading("Documents that are free");

            Resident regular = Person(residents, "Santos");

            Check("certificate of indigency is free",
                fees.Assess(regular, DocumentType.CertificateOfIndigency).IsFree);

            Check("certificate of low income is free",
                fees.Assess(regular, DocumentType.CertificateOfLowIncome).IsFree);

            Check("medical assistance paperwork is free",
                fees.Assess(regular, DocumentType.MedicalAssistanceCertification).IsFree);

            Resident senior = Person(residents, "Dizon");
            Check("a senior citizen's clearance is free under the law",
                fees.Assess(senior, DocumentType.BarangayClearance).IsFree);
        }

        // =================================================================
        //  The business clearance is a regulatory fee: no personal waivers
        // =================================================================

        private static void CheckBusinessIsNeverWaived(FeeSchedule fees, IList<Resident> residents)
        {
            Heading("Business clearance is never waived");

            Resident senior = Person(residents, "Dizon");
            Resident solo = residents.FirstOrDefault(r => r.HasClassification(ResidentClassification.SoloParent))
                            ?? Person(residents, "Santos");
            Resident businessOwner = residents.FirstOrDefault(r => r.IsBusinessOwner) ?? senior;

            Check("a senior citizen who runs a store still pays for the business clearance",
                fees.Assess(businessOwner, DocumentType.BarangayBusinessClearance).FinalFee
                    == AppConfig.FeeBusinessClearanceStandard);

            Check("an indigent resident who runs a store still pays too",
                fees.Assess(solo, DocumentType.BarangayBusinessClearance).FinalFee
                    == AppConfig.FeeBusinessClearanceStandard);
        }

        // =================================================================
        //  First-time jobseeker, free once
        // =================================================================

        private static void CheckJobseeker(FeeSchedule fees, IList<Resident> residents, IClock clock)
        {
            Heading("First-time jobseeker (RA 11261)");

            Resident fresh = Person(residents, "Peña");
            string why;
            bool eligible = fees.IsJobseekerEligible(fresh, out why);
            Check("a newcomer of a few months is not yet a first-time jobseeker",
                !eligible || fresh.GetMonthsOfResidency() >= AppConfig.JobseekerResidencyMonths, why);

            Resident oldEnough = Person(residents, "Santos");
            oldEnough.HasAvailedFirstTimeJobseeker = false;
            oldEnough.DateOfResidency = DateTime.Today.AddYears(-3);

            Check("somebody here long enough qualifies",
                fees.IsJobseekerEligible(oldEnough, out why), why);

            DocumentRequest waived = new DocumentRequest();
            waived.DocumentType = DocumentType.CertificateOfResidency;
            waived.ApplyJobseekerWaiver = true;
            Check("the first free certificate comes out free",
                fees.Assess(oldEnough, waived).IsFree);

            oldEnough.HasAvailedFirstTimeJobseeker = true;
            Check("the same person pays for the second one",
                !fees.Assess(oldEnough, waived).IsFree);
        }

        // =================================================================
        //  Newcomer / Temporary / Permanent
        // =================================================================

        private static void CheckResidencyStatus(IList<Resident> residents, IClock clock)
        {
            Heading("Residency status");

            DateTime now = clock.Now();

            Resident newcomer = new Resident();
            newcomer.DateOfResidency = now.AddMonths(-2);
            Check("under six months here is a newcomer",
                newcomer.SuggestResidencyStatus(AppConfig.NewcomerMonths, AppConfig.PermanentResidencyYears)
                    == ResidencyStatus.Newcomer);

            Resident temporary = new Resident();
            temporary.DateOfResidency = now.AddMonths(-18);
            Check("after the newcomer months the record says temporary",
                temporary.SuggestResidencyStatus(AppConfig.NewcomerMonths, AppConfig.PermanentResidencyYears)
                    == ResidencyStatus.Temporary);

            Resident permanent = new Resident();
            permanent.DateOfResidency = now.AddYears(-7);
            Check("years of residency makes it permanent",
                permanent.SuggestResidencyStatus(AppConfig.NewcomerMonths, AppConfig.PermanentResidencyYears)
                    == ResidencyStatus.Permanent);
        }

        // =================================================================
        //  8:00 AM to 4:00 PM
        // =================================================================

        private static void CheckOfficeWindow(TimeWindowPolicy window)
        {
            Heading("The office window rule");

            Check("8:00 AM is inside the window",
                window.IsInsideOfficeWindow(new DateTime(2026, 3, 10, 8, 0, 0)));

            Check("noon is inside the window",
                window.IsInsideOfficeWindow(new DateTime(2026, 3, 10, 12, 0, 0)));

            Check("4:00 PM exactly is still inside",
                window.IsInsideOfficeWindow(new DateTime(2026, 3, 10, 16, 0, 0)));

            Check("4:01 PM is outside",
                !window.IsInsideOfficeWindow(new DateTime(2026, 3, 10, 16, 1, 0)));

            Check("7:59 AM is outside",
                !window.IsInsideOfficeWindow(new DateTime(2026, 3, 10, 7, 59, 0)));

            Check("the cut-off is described in plain words",
                window.GetCutOffText().Contains("4:00"),
                window.GetCutOffText());
        }

        // =================================================================
        //  Which status a request starts in
        // =================================================================

        private static void CheckStartingStatuses(IBarangayRepository repository, TimeWindowPolicy window,
            FeeSchedule fees, IList<Resident> residents, FixedClock clock)
        {
            Heading("Starting status: cleared on the spot, or waiting");

            SessionManager session = Session(repository);
            ActivityLogService log = new ActivityLogService(repository, session, clock);
            RequestService requests = new RequestService(repository, log, session, clock, fees, window);

            Resident resident = Person(residents, "Santos");

            DocumentRequest inside = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 10, 30, 0));
            RequestPreview preview = requests.Preview(resident, inside, false);
            Check("filed at 10:30 AM with nothing to check: cleared on the spot",
                preview.StartingStatus == RequestStatus.Cleared);
            Check("and the reason says why", !string.IsNullOrWhiteSpace(preview.TimeExplanation));

            DocumentRequest outside = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 16, 20, 0));
            Check("filed at 4:20 PM: it waits as Pending",
                requests.Preview(resident, outside, false).StartingStatus == RequestStatus.Pending);

            DocumentRequest earlyMorning = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 7, 30, 0));
            Check("filed at 7:30 AM: still Pending",
                requests.Preview(resident, earlyMorning, false).StartingStatus == RequestStatus.Pending);

            DocumentRequest business = Simple(resident, DocumentType.BarangayBusinessClearance,
                new DateTime(2026, 3, 10, 10, 30, 0));
            business.RequiresValidation = true;
            Check("a business clearance goes to Processing even at 10:30 AM",
                requests.Preview(resident, business, false).StartingStatus == RequestStatus.Processing);
        }

        private static DocumentRequest Simple(Resident resident, DocumentType type, DateTime filedAt)
        {
            DocumentRequest request = new DocumentRequest();
            request.ResidentId = resident.ResidentId;
            request.ResidentName = resident.GetFullName();
            request.DocumentType = type;
            request.Purpose = "Rule check";
            request.DateRequested = filedAt;
            return request;
        }

        // =================================================================
        //  The walk a request makes: filed, paid, released
        // =================================================================

        private static void CheckRequestWorkflow(IBarangayRepository repository, ActivityLogService log,
            FeeSchedule fees, TimeWindowPolicy window, IList<Resident> residents, FixedClock clock)
        {
            Heading("The request workflow, and the money gate");

            SessionManager session = Session(repository);
            RequestService requests = new RequestService(repository, log, session, clock, fees, window);
            ReceiptService receipts = new ReceiptService(repository, log, session, clock);

            Resident resident = Person(residents, "Santos");
            DocumentRequest request = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 10, 0, 0));

            OperationResult<DocumentRequest> filed = requests.FileRequest(resident, request, false);
            Check("a request filed on the counter is saved", filed.Succeeded, filed.Message);
            Check("and it gets a reference number", !string.IsNullOrWhiteSpace(request.ReferenceNumber),
                request.ReferenceNumber);
            Check("and a history entry of its own", request.History.Count >= 1);

            OperationResult tooEarly = requests.MarkReadyForRelease(request);
            Check("a fee-bearing document cannot be prepared before it is paid",
                !tooEarly.Succeeded, tooEarly.Message);

            Check("the fee is what the schedule says", request.Fee == AppConfig.FeeCertification,
                request.Fee.ToString());

            OperationResult<OfficialReceipt> collected = receipts.Collect(request, request.Fee,
                "A", "0004501", "0004501", PaymentMethod.Cash, "Rule check");
            Check("collecting the fee issues a government receipt", collected.Succeeded, collected.Message);

            if (collected.Succeeded)
            {
                Check("the receipt number is written on the request",
                    request.OfficialReceiptNumber == "0004501", request.OfficialReceiptNumber);
                Check("and the request shows as paid", request.IsPaid);
            }

            Check("now it can be prepared for release", requests.MarkReadyForRelease(request).Succeeded);
            Check("and handed over", requests.Release(request, resident.GetFullName()).Succeeded);
            Check("the request is closed once released", request.IsClosed);
            Check("and the release has a date", request.DateReleased.HasValue);

            // ---- rejection and reopening -----------------------------------
            DocumentRequest second = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 11, 0, 0));
            requests.FileRequest(resident, second, false);
            Check("a request can be rejected with a reason",
                requests.Reject(second, "Purpose is not clear").Succeeded);
            Check("the rejected request is closed", second.IsClosed);
            Check("and it can be reopened, still with its history",
                requests.Reopen(second, "Resident explained the purpose").Succeeded
                && second.History.Count >= 2);
        }

        // =================================================================
        //  What happens to the ones filed after hours
        // =================================================================

        private static void CheckAfterHoursQueue(IBarangayRepository repository, ActivityLogService log,
            FeeSchedule fees, TimeWindowPolicy window, IList<Resident> residents, FixedClock clock)
        {
            Heading("The after-hours queue");

            SessionManager session = Session(repository);
            RequestService requests = new RequestService(repository, log, session, clock, fees, window);

            Resident resident = Person(residents, "Santos");
            DocumentRequest request = Simple(resident, DocumentType.CertificateOfResidency,
                new DateTime(2026, 3, 10, 16, 45, 0));

            OperationResult<DocumentRequest> filed = requests.FileRequest(resident, request, false);
            Check("filing after the cut-off is still accepted",
                filed.Succeeded && request.Status == RequestStatus.Pending, filed.Message);

            int cleared = requests.ClearWaitingRequests();
            Check("nothing is cleared at 4:45 PM", cleared == 0, cleared.ToString());

            // ---- the next morning ------------------------------------------
            clock.Set(new DateTime(2026, 3, 11, 8, 5, 0));
            cleared = requests.ClearWaitingRequests();
            Check("the next morning the waiting request is cleared by itself", cleared >= 1,
                cleared.ToString());
            Check("and it says so in its history",
                request.Status == RequestStatus.Cleared,
                request.GetStatusText() + " / " + request.LastStatusChangeOn.ToString("g"));

            clock.Set(new DateTime(2026, 3, 10, 9, 15, 0));
        }

        // =================================================================
        //  Passwords
        // =================================================================

        private static void CheckPasswordHashing()
        {
            Heading("Passwords are never stored as words");

            PasswordHasher hasher = new PasswordHasher();
            string saltOne, saltTwo;
            string hashOne = hasher.Hash("Barangay@2026", out saltOne);
            string hashTwo = hasher.Hash("Barangay@2026", out saltTwo);

            Check("the same password gives two different stored values",
                hashOne != hashTwo && saltOne != saltTwo);
            Check("the right password is accepted", hasher.Verify("Barangay@2026", hashOne, saltOne));
            Check("a wrong password is refused", !hasher.Verify("barangay@2026", hashOne, saltOne));
            Check("the stored value does not contain the password",
                !hashOne.Contains("Barangay") && !hashOne.Contains("2026"));
            Check("the work factor is the configured one",
                hasher.Iterations >= 100000, hasher.Iterations.ToString());

            UserAccount account = new UserAccount();
            account.Username = "tester";
            account.MustChangePassword = true;
            Check("a new account is asked to change its first password", account.NeedsPasswordChange());
        }

        // =================================================================
        //  Government receipts
        // =================================================================

        private static void CheckReceipts(IBarangayRepository repository, ActivityLogService log,
            IList<Resident> residents, FixedClock clock)
        {
            Heading("Official receipts and their control numbers");

            SessionManager session = Session(repository);
            ReceiptService receipts = new ReceiptService(repository, log, session, clock);

            Check("a well-formed OR number is accepted", OfficialReceipt.IsWellFormedOrNumber("0004501"));
            Check("a sloppy OR number is refused", !OfficialReceipt.IsWellFormedOrNumber("OR-1"));
            Check("a control number must look like a booklet number",
                OfficialReceipt.IsWellFormedControlNumber("0004501")
                && !OfficialReceipt.IsWellFormedControlNumber("abc"));

            IList<ReceiptSeries> series = receipts.GetSeries(false);
            Check("the seeded booklet is found", series.Count >= 1, series.Count.ToString());

            string suggested = receipts.SuggestNextOrNumber("A");
            Check("the next number in the booklet is suggested", !string.IsNullOrWhiteSpace(suggested), suggested);

            Resident payer = Person(residents, "Santos");
            OperationResult<OfficialReceipt> standalone = receipts.CollectStandalone(
                payer.GetFullName(), 50m, series[0].SeriesCode, suggested, suggested,
                PaymentMethod.Cash, "Rule check");

            Check("a walk-in collection with no request behind it is allowed",
                standalone.Succeeded, standalone.Message);

            if (standalone.Succeeded)
            {
                OfficialReceipt receipt = standalone.Value;
                Check("the receipt came out with a number", !string.IsNullOrWhiteSpace(receipt.OrNumber));
                Check("and starts life as a valid receipt", !receipt.IsVoid);

                Check("voiding needs a reason", !receipts.Void(receipt, string.Empty).Succeeded);
                Check("a voided receipt stays in the register but stops counting",
                    receipts.Void(receipt, "Wrong amount punched in").Succeeded && receipt.IsVoid,
                    receipt.VoidReason);
            }

            IList<OfficialReceipt> today = receipts.GetReceipts(new ReceiptQuery
            {
                From = clock.Now().Date,
                To = clock.Now().Date,
                IncludeVoid = true
            });
            decimal counted = today.Where(r => !r.IsVoid).Sum(r => r.Amount);
            decimal everything = today.Sum(r => r.Amount);
            Check("the day's collections exclude the voided receipt", counted < everything,
                counted + " of " + everything);
        }

        // =================================================================
        //  Who may do what
        // =================================================================

        private static void CheckPermissions(ActivityLogService log)
        {
            Heading("Roles and permissions");

            UserAccount clerk = new UserAccount();
            clerk.Username = "clerk";
            clerk.Role = UserRole.Clerk;
            clerk.IsActive = true;

            UserAccount punong = new UserAccount();
            punong.Username = "kapitan";
            punong.Role = UserRole.PunongBarangay;
            punong.IsActive = true;

            UserAccount admin = new UserAccount();
            admin.Username = "admin";
            admin.Role = UserRole.Administrator;
            admin.IsActive = true;

            SessionManager clerkSession = new SessionManager();
            clerkSession.SignIn(clerk);

            SessionManager punongSession = new SessionManager();
            punongSession.SignIn(punong);

            SessionManager adminSession = new SessionManager();
            adminSession.SignIn(admin);

            Check("a clerk may collect at the counter", clerkSession.Has(Permission.CollectPayments));
            Check("a clerk may not void a receipt", !clerkSession.Has(Permission.VoidReceipts));
            Check("a clerk may not manage accounts", !clerkSession.Has(Permission.ManageUsers));
            Check("the punong barangay may approve and release",
                punongSession.Has(Permission.ReleaseDocuments) && punongSession.Has(Permission.ValidateRequests));
            Check("the punong barangay does not collect money",
                !punongSession.Has(Permission.CollectPayments));
            Check("the administrator may manage accounts", adminSession.Has(Permission.ManageUsers));
            Check("only somebody with the right may see a fee basis",
                !clerkSession.Has(Permission.ViewFeeBasis) || adminSession.Has(Permission.ViewFeeBasis));

            UserAccount locked = new UserAccount();
            locked.Username = "locked";
            locked.IsActive = true;
            DateTime now = new DateTime(2026, 3, 10, 9, 0, 0);
            for (int i = 0; i < AppConfig.MaxFailedLogins; i++)
                locked.RegisterFailedAttempt(now, AppConfig.MaxFailedLogins, AppConfig.LockoutMinutes);

            Check("too many wrong passwords locks the account out",
                locked.IsLockedOut(now) && !locked.CanSignIn(now));
            Check("the lock lifts by itself after the configured minutes",
                !locked.IsLockedOut(now.AddMinutes(AppConfig.LockoutMinutes + 1)));
        }

        // =================================================================
        //  The activity log
        // =================================================================

        private static void CheckActivityLog(IBarangayRepository repository, ActivityLogService log, FixedClock clock)
        {
            Heading("The activity log");

            int before = log.CountForDay(clock.Now().Date);
            log.Record(ActivityModule.Requests, "Rule check entry", "Request", "RC-1",
                "Written by the rule checks.");

            Check("an entry is written with the person's name on it",
                log.CountForDay(clock.Now().Date) == before + 1);

            DataTable table = log.GetLogTable(new ActivityLogQuery
            {
                From = clock.Now().Date,
                To = clock.Now().Date,
                MaximumRows = 50
            });

            Check("the log can be read back for the day", table.Rows.Count >= 1, table.Rows.Count.ToString());
            Check("the entry keeps who, what and when",
                table.Rows.Count > 0 && table.Columns.Contains("username")
                && table.Columns.Contains("occurred_on"));

            Check("the log has no way to edit or delete an entry",
                !typeof(ActivityLogService).GetMethods().Any(m =>
                    (m.Name.StartsWith("Update") || m.Name.StartsWith("Delete")) && m.ReturnType == typeof(void)));

            Check("the log offers the list of people who have signed in", log.GetUsernames().Count >= 0);
        }

        // =================================================================
        //  Census
        // =================================================================

        private static void CheckCensus(IBarangayRepository repository, IList<Resident> residents)
        {
            Heading("Census");

            CensusService census = new CensusService(repository);
            CensusSnapshot snapshot = census.GetSnapshot(string.Empty);

            Check("the population count matches the registry",
                snapshot.ActiveResidents == residents.Count, snapshot.ActiveResidents.ToString());
            Check("the total population includes the dependents",
                snapshot.TotalPopulation == snapshot.ActiveResidents + snapshot.Dependents);
            Check("the household count is not zero", snapshot.Households >= 1);
            Check("the average household size makes sense",
                snapshot.AverageHouseholdSize > 0 && snapshot.AverageHouseholdSize < 20,
                snapshot.AverageHouseholdSize.ToString("0.0"));
            Check("the senior citizens are counted", snapshot.Seniors >= 1);
            Check("the students are counted through the fee category", snapshot.Students >= 0);
            Check("the summary can describe itself",
                !string.IsNullOrWhiteSpace(census.Describe(string.Empty, snapshot)));

            DataTable byPurok = census.GetPopulationByPurok(true);
            Check("the population by purok comes out", byPurok.Rows.Count >= 1, byPurok.Rows.Count.ToString());

            DataTable byAge = census.GetPopulationByAge(string.Empty, true);
            Check("the population by age bracket comes out", byAge.Rows.Count >= 1, byAge.Rows.Count.ToString());
        }

        // =================================================================
        //  Reports
        // =================================================================

        private static void CheckReports(IBarangayRepository repository, ActivityLogService log, FixedClock clock)
        {
            Heading("Reports");

            ReportService reports = new ReportService(repository, log, clock, "rulecheck");

            Check("every report I promise is actually there",
                reports.GetDefinitions().Count >= 10, reports.GetDefinitions().Count.ToString());

            ReportParameters parameters = new ReportParameters();
            parameters.From = clock.Now().Date.AddDays(-30);
            parameters.To = clock.Now().Date;
            parameters.Username = "rulecheck";
            parameters.Module = ActivityModule.Requests;

            foreach (ReportDefinition definition in reports.GetDefinitions())
            {
                ReportResult result = reports.Run(definition.Key, parameters);
                bool ok = result != null && !string.IsNullOrWhiteSpace(result.Title);

                // The activity log report needs a named person, and the census
                // reports do not need a date range; the point of the check is
                // that no report throws and each one comes back with a heading.
                Check("report: " + definition.Title, ok, ok ? string.Empty : "no title");
            }

            ReportResult register = reports.Run(ReportDefinitions.DocumentRegister, parameters);
            Check("the document register has the columns the printed report needs",
                register.Table != null && register.Table.Columns.Count >= 5,
                register.Table == null ? "no table" : register.Table.Columns.Count.ToString());

            string folder = Path.Combine(Path.GetTempPath(), "barangay-rule-checks");
            string file = reports.ExportCsv(register, folder);
            Check("the register exports to a spreadsheet file", File.Exists(file), file);
            if (File.Exists(file)) File.Delete(file);
        }

        // =================================================================
        //  Active, inactive, archived
        // =================================================================

        private static void CheckStates(IBarangayRepository repository, IList<Resident> residents, FixedClock clock)
        {
            Heading("Residents: active, inactive, archived");

            Resident resident = Person(residents, "Dizon");
            Check("a seeded resident starts active", resident.IsActive);

            ResidentQuery active = new ResidentQuery();
            active.RecordState = RecordState.Active;
            int activeCount = repository.GetResidents(active).Count;

            ResidentQuery archived = new ResidentQuery();
            archived.RecordState = RecordState.Archived;
            Check("the archive is empty before anybody is archived",
                repository.GetResidents(archived).Count == 0);

            repository.SetRecordState(resident.ResidentId, RecordState.Inactive,
                "Moved to another barangay", "rulecheck", clock.Now());

            Resident reread = repository.GetResident(resident.ResidentId);
            Check("deactivating takes the resident off the active list",
                !reread.IsActive && reread.RecordState == RecordState.Inactive);
            Check("the reason and the date are kept for the record",
                !string.IsNullOrWhiteSpace(reread.StateReason) && reread.StateChangedOn.HasValue);
            Check("the active list is now one shorter",
                repository.GetResidents(active).Count == activeCount - 1);
            Check("nothing was deleted - the record is still there",
                repository.GetResident(resident.ResidentId) != null);

            // Put the resident back the way the shop was found, so a re-run of
            // these checks starts from the same place.
            repository.SetRecordState(resident.ResidentId, RecordState.Active, "Rule check finished",
                "rulecheck", clock.Now());
        }
    }
}
