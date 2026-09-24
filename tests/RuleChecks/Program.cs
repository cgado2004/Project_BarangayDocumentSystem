using BarangayDocumentSystem.DBContext;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Service;
using MySql.Data.MySqlClient;

// =====================================================================
//  My rule checks, v3.1.1.
//
//  I wrote this because compiling only proves my code is grammatical - it
//  says nothing about whether the fees are right. This runs the actual
//  business rules and compares them against the Citizen's Charter and the
//  laws behind it.
//
//  v3.1 grew the list for the new money documents: the cedula computed
//  under RA 7160 Sec. 156, the ₱150 lupon filing, facilities at ₱200 an
//  hour, the Taripa items, and the business clearance whose amount now
//  VARIES with the law violated.
//
//  v3.1.1 adds the rejection rules, the payment guards and the
//  receipt-uniqueness rule, ported from Jonathan F. Del Rosario's
//  Draft-branch test suite and adapted to this codebase's model.
//
//  To run it:  dotnet run --project tests/RuleChecks
// =====================================================================

int pass = 0, fail = 0;

void Check(string name, bool ok, string detail = "")
{
    if (ok) { pass++; Console.WriteLine($"  PASS  {name}"); }
    else    { fail++; Console.WriteLine($"  FAIL  {name}  {detail}"); }
}

var fees = new FeeSchedule();
var repo = new InMemoryBarangayRepository(fees);

Console.WriteLine("=== Seed data and the real puroks ===");
Check("7 residents seeded", repo.Residents.Count == 7, repo.Residents.Count.ToString());
foreach (var r in repo.Residents)
    Console.WriteLine($"     {r.GetSortableName(),-26} {r.Purok}");
Check("every purok is a real one",
    repo.Residents.All(r => Puroks.Contains(r.Purok)));
Check("Peña keeps its ñ", repo.Residents.Any(r => r.LastName == "Peña"));

var juan  = repo.Residents.First(r => r.LastName == "Dela Cruz");
var maria = repo.Residents.First(r => r.FirstName == "Maria");
var ana   = repo.Residents.First(r => r.LastName == "Villanueva");
var liza  = repo.Residents.First(r => r.LastName == "Santos-Reyes");
var carlo = repo.Residents.First(r => r.LastName == "Peña");
var jose  = repo.Residents.First(r => r.LastName == "Bautista");

Console.WriteLine("\n=== The Citizen's Charter flat rates ===");
Check("clearance LOCAL  = 100", fees.Assess(juan, DocumentType.BarangayClearance, new RequestInput(Scope: ClearanceScope.Local)).FinalFee == 100m);
Check("clearance ABROAD = 200", fees.Assess(juan, DocumentType.BarangayClearance, new RequestInput(Scope: ClearanceScope.Abroad)).FinalFee == 200m);
Check("residency        = 100", fees.Assess(juan, DocumentType.CertificateOfResidency).FinalFee == 100m);
Check("indigency        FREE",  fees.Assess(juan, DocumentType.CertificateOfIndigency).FinalFee == 0m);
Check("low income       FREE",  fees.Assess(juan, DocumentType.CertificateOfLowIncome).FinalFee == 0m);
Check("assistance papers FREE", fees.Assess(juan, DocumentType.MedicalAssistanceCertification).FinalFee == 0m);

Console.WriteLine("\n=== The v3.1 variable-fee documents ===");
var business = fees.Assess(ana, DocumentType.BarangayBusinessClearance,
    new RequestInput(Amount: 500m, Detail: "Barangay Ordinance No. 12-2024"));
Check("business VARIES - assessed 500 stands", business.FinalFee == 500m, business.FinalFee.ToString());
Check("business basis names the violated law",
    business.Basis.Contains("Ordinance No. 12-2024"));
Check("business standard rate when nothing violated",
    fees.Assess(ana, DocumentType.BarangayBusinessClearance).FinalFee == 200m);

var cedula120k = fees.Assess(juan, DocumentType.CommunityTaxCertificate,
    new RequestInput(GrossAnnualIncome: 120_000m));
Check("cedula on ₱120,000 income = 125", cedula120k.FinalFee == 125m, cedula120k.FinalFee.ToString());
Check("cedula with no income = 5",
    fees.Assess(juan, DocumentType.CommunityTaxCertificate).FinalFee == 5m);
Check("cedula additional caps at 5,000",
    fees.Assess(juan, DocumentType.CommunityTaxCertificate,
        new RequestInput(GrossAnnualIncome: 10_000_000m)).FinalFee == 5_005m);
Check("cedula for a minor is BLOCKED",
    fees.Assess(repo.Residents.First(r => r.GetAge() < 18),
        DocumentType.CommunityTaxCertificate).IsBlocked);

Check("lupon filing = 150",
    fees.Assess(juan, DocumentType.LuponCaseFiling).FinalFee == 150m);
Check("facility 2.5 hours bills as 3 = 600",
    fees.Assess(juan, DocumentType.BarangayFacilityRental, new RequestInput(Hours: 2.5m)).FinalFee == 600m);
Check("facility without hours is BLOCKED",
    fees.Assess(juan, DocumentType.BarangayFacilityRental).IsBlocked);

Check("Taripa 50 with its item stands",
    fees.Assess(juan, DocumentType.OtherTarifaProcessingFee,
        new RequestInput(Amount: 50m, Detail: "Certified true copies")).FinalFee == 50m);
Check("Taripa without an item is BLOCKED",
    fees.Assess(juan, DocumentType.OtherTarifaProcessingFee,
        new RequestInput(Amount: 50m)).IsBlocked);

Console.WriteLine("\n=== Statutory exemptions, and where they stop ===");
Check("senior waived (RA 9994)",  fees.Assess(maria, DocumentType.BarangayClearance).FinalFee == 0m);
Check("PWD waived (RA 10754)",    fees.Assess(liza, DocumentType.CertificateOfGoodMoralCharacter).FinalFee == 0m);
Check("senior STILL pays the business fee",
    fees.Assess(maria, DocumentType.BarangayBusinessClearance).FinalFee == 200m);
Check("senior STILL pays the cedula",
    fees.Assess(maria, DocumentType.CommunityTaxCertificate,
        new RequestInput(GrossAnnualIncome: 100_000m)).FinalFee == 105m);
Check("indigent still pays the lupon filing fee",
    fees.Assess(repo.Residents.First(r => r.FirstName == "Pedro"),
        DocumentType.LuponCaseFiling).FinalFee == 150m);
Check("senior waived on the abroad rate too",
    fees.Assess(maria, DocumentType.BarangayClearance, new RequestInput(Scope: ClearanceScope.Abroad)).FinalFee == 0m);

Console.WriteLine("\n=== RA 11261, on the certificate AND the clearance ===");
var okJ = fees.Assess(jose, DocumentType.FirstTimeJobseekerCertificate);
var noJ = fees.Assess(carlo, DocumentType.FirstTimeJobseekerCertificate);
Check("Jose (14 months) allowed and free", !okJ.IsBlocked && okJ.FinalFee == 0m);
Check("Carlo (2 months) BLOCKED", noJ.IsBlocked);
Console.WriteLine($"     -> {noJ.BlockReason}");

var waiver = fees.Assess(jose, DocumentType.BarangayClearance,
    new RequestInput(ApplyJobseekerWaiver: true));
Check("the clearance can claim the waiver too", waiver.FinalFee == 0m && waiver.MarksJobseekerAvailment);
Check("without the claim the clearance costs 100",
    fees.Assess(jose, DocumentType.BarangayClearance).FinalFee == 100m);

var waivedClearance = repo.CreateRequest(jose, DocumentType.BarangayClearance,
    "Test", new RequestInput(ApplyJobseekerWaiver: true));
waivedClearance.StartProcessing();
waivedClearance.MarkReadyForRelease();
waivedClearance.Release();
Check("releasing a waived clearance marks the one-time availment",
    jose.HasAvailedFirstTimeJobseeker);
Check("a second RA 11261 claim is BLOCKED",
    fees.Assess(jose, DocumentType.BarangayClearance,
        new RequestInput(ApplyJobseekerWaiver: true)).IsBlocked);

Console.WriteLine("\n=== The workflow, unchanged from v3 ===");
var req = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Abroad));
Check("an abroad request costs 200", req.Fee == 200m, req.Fee.ToString());
req.StartProcessing();
req.MarkReadyForRelease();
bool blocked = false;
try { req.Release(); } catch (InvalidOperationException) { blocked = true; }
Check("unpaid release is REFUSED", blocked);
req.RecordPayment("OR-1");
req.Release();
Check("released once paid", req.Status == RequestStatus.Released);
Check("reference number format",
    System.Text.RegularExpressions.Regex.IsMatch(req.GetReferenceNumber(), @"^BMP-\d{4}-\d{4}$"),
    req.GetReferenceNumber());

Console.WriteLine("\n=== RA 11032 aging ===");
Check("4 weekdays counted correctly",
    DocumentRequest.WorkingDaysBetween(new DateTime(2026, 9, 21), new DateTime(2026, 9, 25)) == 4);
Check("a weekend is not counted",
    DocumentRequest.WorkingDaysBetween(new DateTime(2026, 9, 25), new DateTime(2026, 9, 28)) == 1);
var aged = DocumentRequest.Rehydrate(9_999, juan, DocumentType.OtherCertification,
    "Old", DateTime.Today.AddDays(-6), null, RequestStatus.Pending, 100m, "x", false, "", "");
Check("6 calendar days = 4+ working days, past the 3-day standard",
    aged.IsBeyondRA11032Standard(3));
Check("a released request never counts as aged",
    !req.IsBeyondRA11032Standard(3));

Console.WriteLine("\n=== The printed documents ===");
var renderer = new DocumentRenderer(BarangayProfile.Current, fees);
Check("all 24 document types have a readable name",
    Enum.GetValues<DocumentType>().All(t => !string.IsNullOrWhiteSpace(FeeSchedule.NameOf(t))));
Check("all 24 document types have a template",
    Enum.GetValues<DocumentType>().All(t => renderer.TemplateFor(t) is not null));

int rendered = 0;
foreach (DocumentType type in Enum.GetValues<DocumentType>())
{
    var sample = DocumentRequest.Rehydrate(1, juan, type, "the stated purpose of this request",
        DateTime.Today, null, RequestStatus.ReadyForRelease, 100m, "test", false, "", "");
    string doc = renderer.RenderText(sample);
    if (doc.Contains("BARANGAY MAGUGPO POBLACION") &&
        doc.Contains("HON. EUGENIA SOLIS HINGPIT, MD") &&
        !doc.Contains("[SET"))
        rendered++;
    else
        Console.WriteLine($"     !! {type} rendered incompletely");
}
Check("every document type renders on the real letterhead", rendered == Enum.GetValues<DocumentType>().Length,
    $"{rendered}/{Enum.GetValues<DocumentType>().Length}");

string jobseekerDoc = renderer.RenderText(
    DocumentRequest.Rehydrate(2, jose, DocumentType.FirstTimeJobseekerCertificate,
        "NBI application", DateTime.Today, null, RequestStatus.ReadyForRelease, 0m, "RA 11261", false, "", ""));
Check("the jobseeker certificate carries the Oath of Undertaking",
    jobseekerDoc.Contains("OATH OF UNDERTAKING"));

string cedulaDoc = renderer.RenderText(
    DocumentRequest.Rehydrate(3, juan, DocumentType.CommunityTaxCertificate,
        "Annual community tax", DateTime.Today, null, RequestStatus.ReadyForRelease, 155m,
        "RA 7160 Sec. 156", true, "OR-2", "",
        new RequestInput(GrossAnnualIncome: 150_000m)));
Check("the cedula prints its computation",
    cedulaDoc.Contains("₱150,000.00") && cedulaDoc.Contains("₱155.00"));
Check("the cedula cites RA 7160", cedulaDoc.Contains("156"));

// =====================================================================
//  v3.1.1 — the checks below were ported from Jonathan F. Del Rosario's
//  Draft-branch test suite and adapted to the real fee schedule: his
//  rejection, payment-guard and receipt-uniqueness rules, which the
//  harness did not cover before, expressed against THIS project's rules
//  in DocumentRequest and the store.
// =====================================================================

Console.WriteLine("\n=== Rejection rules (v3.1.1) ===");
var rejected = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Local));
rejected.Reject("Resident withdrew the application");
Check("a pending request can be rejected with a reason",
    rejected.Status == RequestStatus.Rejected);

bool refused = false;
try { rejected.StartProcessing(); } catch (InvalidOperationException) { refused = true; }
Check("a rejected request cannot be processed", refused);
refused = false;
try { rejected.Release(); } catch (InvalidOperationException) { refused = true; }
Check("a rejected request cannot be released", refused);
refused = false;
try { rejected.Reject("   "); } catch (ArgumentException) { refused = true; }
Check("a blank reason is refused", refused);
refused = false;
try { req.Reject("Too late"); } catch (InvalidOperationException) { refused = true; }
Check("a released document cannot be rejected", refused);

var paidThenRejected = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Local));
paidThenRejected.StartProcessing();
paidThenRejected.MarkReadyForRelease();
paidThenRejected.RecordPayment("OR-V311-1");
decimal collectedBefore = repo.GetStatistics().TotalCollected;
paidThenRejected.Reject("Approved too late — the resident no longer needs it");
Check("a paid request can still be rejected before release",
    paidThenRejected.Status == RequestStatus.Rejected);
Check("a rejected payment keeps its receipt recorded",
    paidThenRejected.IsPaid && paidThenRejected.OfficialReceiptNo == "OR-V311-1");
Check("rejecting a paid request does not change the collection total",
    repo.GetStatistics().TotalCollected == collectedBefore);

Console.WriteLine("\n=== Payment guards and receipt uniqueness (v3.1.1) ===");
var freeRequest = repo.CreateRequest(maria, DocumentType.CertificateOfResidency, "Test");
refused = false;
try { freeRequest.RecordPayment("OR-V311-2"); } catch (InvalidOperationException) { refused = true; }
Check("no payment is due on a free document", refused);

var paidTwice = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Local));
paidTwice.RecordPayment("OR-V311-3");
refused = false;
try { paidTwice.RecordPayment("OR-V311-4"); } catch (InvalidOperationException) { refused = true; }
Check("a request cannot be paid twice", refused);

var noReceipt = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Local));
refused = false;
try { noReceipt.RecordPayment("   "); } catch (ArgumentException) { refused = true; }
Check("a receipt number is required", refused);

Check("a fresh receipt number is not on file", !repo.ReceiptNumberExists("OR-V311-999"));
Check("a used receipt number is detected, case-insensitively",
    repo.ReceiptNumberExists("or-v311-3"));

var clash = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test",
    new RequestInput(Scope: ClearanceScope.Local));
clash.RecordPayment("OR-V311-3");
refused = false;
try { repo.SaveRequest(clash); } catch (InvalidOperationException) { refused = true; }
Check("the store refuses a duplicate receipt number", refused);

// =====================================================================
//  THE MYSQL ROUND-TRIP (v3.2.0)
//
//  The checks above prove the rules. This section proves the PERSISTENCE:
//  that the MySQL repository writes what the screens write, and reads back
//  the same truth through a brand-new connection.
//
//  It is deliberately polite about the machine it runs on. No MySQL server?
//  It says SKIP and moves on - that is not a failure, the whole app falls
//  back to memory without a server (docs/05 §9's always-starts contract).
//  When a server IS running, everything happens inside a throwaway database
//  (barangay_rulecheck_tmp) that is dropped again at the end, so the real
//  barangay_magugpo demo database is never touched.
//
//  The throwaway database gets the four tables the repository needs,
//  mirroring DBContext/db/01-schema.sql's columns. Running the FULL script (triggers,
//  checks, views) is the docs/05 §3 step for a real setup, not this test's
//  job - here I am testing MY code, not the server's.
// =====================================================================
Console.WriteLine("\n=== MySQL round-trip (v3.2.0) ===");

string sqlServer = Environment.GetEnvironmentVariable("BARANGAY_TEST_SERVER")
    ?? "Server=localhost;Port=3306;Uid=root;Pwd=;SslMode=Preferred;Connection Timeout=3;";

MySqlConnection? probe = null;
try { probe = new MySqlConnection(sqlServer); probe.Open(); }
catch { /* no server, or no rights - handled below */ }

if (probe is null || probe.State != System.Data.ConnectionState.Open)
{
    Console.WriteLine("  SKIP  no MySQL server reachable - the app itself would " +
                      "fall back to memory here, so this is not a failure");
}
else
{
    probe.Dispose();
    const string scratchDb = "barangay_rulecheck_tmp";

    // The four tables, in the shape DBContext/db/01-schema.sql defines them (columns
    // only - the triggers and CHECKs there guard hand-written SQL, and my
    // repository enforces the same rules itself).
    string[] tables =
    {
        @"CREATE TABLE residents (
              resident_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
              first_name VARCHAR(60) NOT NULL, middle_name VARCHAR(60) NULL,
              last_name VARCHAR(60) NOT NULL, suffix VARCHAR(10) NULL,
              date_of_birth DATE NOT NULL,
              gender ENUM('Male','Female') NOT NULL,
              civil_status ENUM('Single','Married','Widowed','Separated','Divorced')
                  NOT NULL DEFAULT 'Single',
              purok VARCHAR(40) NOT NULL, address_line VARCHAR(160) NULL,
              contact_number VARCHAR(20) NULL, occupation VARCHAR(80) NULL,
              date_of_residency DATE NOT NULL,
              is_registered_voter BOOLEAN NOT NULL DEFAULT FALSE,
              has_availed_jobseeker BOOLEAN NOT NULL DEFAULT FALSE,
              created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
              updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  ON UPDATE CURRENT_TIMESTAMP,
              PRIMARY KEY (resident_id)) ENGINE=InnoDB",
        @"CREATE TABLE classification_types (
              classification_code VARCHAR(20) NOT NULL,
              display_name VARCHAR(40) NOT NULL,
              legal_basis VARCHAR(120) NULL,
              grants_fee_exemption BOOLEAN NOT NULL DEFAULT FALSE,
              PRIMARY KEY (classification_code)) ENGINE=InnoDB",
        @"CREATE TABLE resident_classifications (
              resident_id INT UNSIGNED NOT NULL,
              classification_code VARCHAR(20) NOT NULL,
              PRIMARY KEY (resident_id, classification_code)) ENGINE=InnoDB",
        @"CREATE TABLE document_requests (
              request_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
              resident_id INT UNSIGNED NOT NULL,
              document_type ENUM('BarangayClearance','CertificateOfResidency',
                  'CertificateOfIndigency','BarangayBusinessClearance','BarangayID',
                  'FirstTimeJobseekerCertificate','CertificateOfGoodMoralCharacter',
                  'CertificateOfLowIncome','SoloParentCertification',
                  'MedicalAssistanceCertification','FinancialAssistanceCertification',
                  'BurialAssistanceCertification','IpScholarshipCertification',
                  'FourPsScholarshipCertification','EmploymentCertification',
                  'AcceptanceCertificate','GadRelatedDocumentation',
                  'BlotterRelatedIncident','CsoDocumentation','OtherCertification',
                  'CommunityTaxCertificate','LuponCaseFiling',
                  'BarangayFacilityRental','OtherTarifaProcessingFee') NOT NULL,
              scope ENUM('Local','Abroad') NOT NULL DEFAULT 'Local',
              purpose VARCHAR(200) NOT NULL,
              date_requested DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
              date_released DATETIME NULL,
              status ENUM('Pending','Processing','ReadyForRelease','Released','Rejected')
                  NOT NULL DEFAULT 'Pending',
              fee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
              fee_basis VARCHAR(255) NULL,
              assessed_amount DECIMAL(10,2) NULL,
              hours_of_use DECIMAL(6,2) NULL,
              declared_income DECIMAL(12,2) NULL,
              fee_detail VARCHAR(200) NULL,
              availed_jobseeker_act BOOLEAN NOT NULL DEFAULT FALSE,
              is_paid BOOLEAN NOT NULL DEFAULT FALSE,
              official_receipt_no VARCHAR(40) NULL,
              remarks VARCHAR(255) NULL,
              created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
              updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                  ON UPDATE CURRENT_TIMESTAMP,
              PRIMARY KEY (request_id)) ENGINE=InnoDB"
    };

    try
    {
        using (var setup = new MySqlConnection(sqlServer))
        {
            setup.Open();
            void Run(string sql)
            {
                using var c = new MySqlCommand(sql, setup);
                c.ExecuteNonQuery();
            }
            Run($"DROP DATABASE IF EXISTS {scratchDb};");
            Run($"CREATE DATABASE {scratchDb} CHARACTER SET utf8mb4;");
            Run($"USE {scratchDb};");
            foreach (string sql in tables) Run(sql + ";");
            Run(@"INSERT INTO classification_types
                  (classification_code, display_name, grants_fee_exemption) VALUES
                  ('SENIOR_CITIZEN','Senior Citizen',TRUE),('PWD','PWD',TRUE),
                  ('INDIGENT','Indigent',TRUE),('STUDENT','Student',FALSE),
                  ('SOLO_PARENT','Solo Parent',FALSE);");
        }

        string scratch = sqlServer + $"Database={scratchDb};";
        var sqlFees = new FeeSchedule();

        // First connection: provisioning seeds the same demo as memory.
        var first = new BarangayDocumentSystem.DBContext.MySqlBarangayRepository(scratch, sqlFees);
        Check("a fresh database seeds itself with 7 residents", first.Residents.Count == 7,
            first.Residents.Count.ToString());
        Check("the seeded requests carried their fee rules with them",
            first.Requests.Count == 12 &&
            first.GetStatistics().TotalCollected == 255m,
            $"requests={first.Requests.Count}, collected={first.GetStatistics().TotalCollected}");

        // The whole point of the database: a NEW connection must see what
        // the OLD session wrote - the thing the in-memory store could never do.
        var sqlJuan = first.Residents.First(r => r.LastName == "Dela Cruz");
        first.AddResident(new ResidentDetails(
            "Test", "Round", "Trip", "", new DateTime(1990, 1, 2),
            Gender.Female, CivilStatus.Single, "Purok Sampaguita",
            "1 Test St", "09990000000", "Tester",
            new DateTime(2020, 1, 1), false, ResidentClassification.PWD));
        first.CreateRequest(sqlJuan, DocumentType.BarangayClearance,
            "Round trip", new RequestInput(Scope: ClearanceScope.Local));

        var reopened = new BarangayDocumentSystem.DBContext.MySqlBarangayRepository(scratch, sqlFees);
        Check("a resident added last session is still here",
            reopened.Residents.Any(r => r.LastName == "Trip" && r.FirstName == "Test"));
        Check("her PWD tag survived the round trip",
            reopened.Residents.Any(r => r.LastName == "Trip" &&
                r.HasClassification(ResidentClassification.PWD)));

        // A paid release must read back released-and-paid, receipt and all.
        var sqlMaria = reopened.Residents.First(r => r.LastName == "Reyes");
        var paid = reopened.CreateRequest(sqlMaria, DocumentType.CertificateOfResidency,
            "Round trip payment", new RequestInput(Scope: ClearanceScope.Local));
        paid.StartProcessing();
        paid.MarkReadyForRelease();
        paid.RecordPayment("OR-RT-77");
        paid.Release();
        reopened.SaveRequest(paid);

        var third = new BarangayDocumentSystem.DBContext.MySqlBarangayRepository(scratch, sqlFees);
        var reloaded = third.Requests.First(r => r.Purpose == "Round trip payment");
        Check("a released-and-paid request reads back exactly so",
            reloaded.Status == RequestStatus.Released && reloaded.IsPaid &&
            reloaded.OfficialReceiptNo == "OR-RT-77" && reloaded.Fee == 0m,
            $"{reloaded.Status}, paid={reloaded.IsPaid}, receipt={reloaded.OfficialReceiptNo}");
        Check("the receipt index survives the restart, case-insensitively",
            third.ReceiptNumberExists("or-rt-77"));

        Check("Maria's senior waiver was priced into the seeded row",
            third.Requests.Any(r => r.Resident.LastName == "Reyes" &&
                r.DocumentType == DocumentType.CertificateOfResidency &&
                r.Fee == 0m && r.Status == RequestStatus.Released));
    }
    catch (Exception sqlProblem)
    {
        Check("MySQL round-trip completed", false, sqlProblem.Message);
    }
    finally
    {
        try
        {
            using var cleanup = new MySqlConnection(sqlServer);
            cleanup.Open();
            using var drop = new MySqlCommand($"DROP DATABASE IF EXISTS {scratchDb};", cleanup);
            drop.ExecuteNonQuery();
        }
        catch { /* if the drop fails the scratch db is removed next run anyway */ }
    }
}

Console.WriteLine($"\n=== {pass} passed, {fail} failed ===");
return fail == 0 ? 0 : 1;
