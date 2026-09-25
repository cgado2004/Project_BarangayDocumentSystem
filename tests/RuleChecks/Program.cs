using System;
using System.Linq;
using BarangayDocumentSystem.Database;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.BusinessRules;

// =====================================================================
//  My rule checks, v3.1.
//
//  I wrote this because compiling only proves my code is grammatical - it
//  says nothing about whether the fees are right. This runs the actual
//  business rules and compares them against the Citizen's Charter and the
//  laws behind it.
//
//  v3.1 grows the list for the new money documents: the cedula computed
//  under RA 7160 Sec. 156, the ₱150 lupon filing, facilities at ₱200 an
//  hour, the Taripa items, and the business clearance whose amount now
//  VARIES with the law violated.
//
//  Build with MSBuild, then run tests\RuleChecks\bin\Debug\RuleChecks.exe
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
    ((DocumentType[])Enum.GetValues(typeof(DocumentType))).All(t => !string.IsNullOrWhiteSpace(FeeSchedule.NameOf(t))));
Check("all 24 document types have a template",
    ((DocumentType[])Enum.GetValues(typeof(DocumentType))).All(t => renderer.TemplateFor(t) is not null));

int rendered = 0;
foreach (DocumentType type in ((DocumentType[])Enum.GetValues(typeof(DocumentType))))
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
Check("every document type renders on the real letterhead", rendered == ((DocumentType[])Enum.GetValues(typeof(DocumentType))).Length,
    $"{rendered}/{((DocumentType[])Enum.GetValues(typeof(DocumentType))).Length}");

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

Check("accented-name search is case-insensitive", repo.SearchResidents("PEÑA").Any());

Console.WriteLine($"\n=== {pass} passed, {fail} failed ===");
return fail == 0 ? 0 : 1;
