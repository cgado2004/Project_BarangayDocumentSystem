using BarangayDocumentSystem.Core.Data;
using BarangayDocumentSystem.Core.Entities;
using BarangayDocumentSystem.Core.Rules;

// =====================================================================
//  My rule checks.
//
//  I wrote this because compiling only proves my code is grammatical - it
//  says nothing about whether the fees are right. This runs the actual
//  business rules and compares them against the Citizen's Charter.
//
//  I keep it out of the App project on purpose: it references Core only,
//  which proves my rules really can be tested with no user interface at all.
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
    repo.Residents.All(r => BarangayProfile.Puroks.Contains(r.Purok)));
Check("Peña keeps its ñ", repo.Residents.Any(r => r.LastName == "Peña"));

var juan  = repo.Residents.First(r => r.LastName == "Dela Cruz");
var maria = repo.Residents.First(r => r.FirstName == "Maria");
var ana   = repo.Residents.First(r => r.LastName == "Villanueva");
var liza  = repo.Residents.First(r => r.LastName == "Santos-Reyes");
var carlo = repo.Residents.First(r => r.LastName == "Peña");
var jose  = repo.Residents.First(r => r.LastName == "Bautista");

Console.WriteLine("\n=== The Citizen's Charter rates ===");
Check("clearance LOCAL  = 100", fees.Assess(juan, DocumentType.BarangayClearance, ClearanceScope.Local).FinalFee == 100m);
Check("clearance ABROAD = 200", fees.Assess(juan, DocumentType.BarangayClearance, ClearanceScope.Abroad).FinalFee == 200m);
Check("residency        = 100", fees.Assess(juan, DocumentType.CertificateOfResidency).FinalFee == 100m);
Check("indigency        FREE",  fees.Assess(juan, DocumentType.CertificateOfIndigency).FinalFee == 0m);
Check("low income       FREE",  fees.Assess(juan, DocumentType.CertificateOfLowIncome).FinalFee == 0m);
Check("business         = 200", fees.Assess(ana, DocumentType.BarangayBusinessClearance).FinalFee == 200m);

Console.WriteLine("\n=== Statutory exemptions ===");
Check("senior waived (RA 9994)",  fees.Assess(maria, DocumentType.BarangayClearance).FinalFee == 0m);
Check("PWD waived (RA 10754)",    fees.Assess(liza, DocumentType.CertificateOfGoodMoralCharacter).FinalFee == 0m);
Check("senior STILL pays the business fee", fees.Assess(maria, DocumentType.BarangayBusinessClearance).FinalFee == 200m);
Check("senior waived on the abroad rate too", fees.Assess(maria, DocumentType.BarangayClearance, ClearanceScope.Abroad).FinalFee == 0m);

Console.WriteLine("\n=== RA 11261 ===");
var okJ = fees.Assess(jose, DocumentType.FirstTimeJobseekerCertificate);
var noJ = fees.Assess(carlo, DocumentType.FirstTimeJobseekerCertificate);
Check("Jose (14 months) allowed and free", !okJ.IsBlocked && okJ.FinalFee == 0m);
Check("Carlo (2 months) BLOCKED", noJ.IsBlocked);
Console.WriteLine($"     -> {noJ.BlockReason}");

Console.WriteLine("\n=== Workflow ===");
var req = repo.CreateRequest(juan, DocumentType.BarangayClearance, "Test", ClearanceScope.Abroad);
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

Console.WriteLine("\n=== The printed certificate ===");
string doc = new DocumentRenderer(BarangayProfile.Current).Render(req);
Check("letterhead names the barangay", doc.Contains("BARANGAY MAGUGPO POBLACION"));
Check("the real Punong Barangay is printed", doc.Contains("HON. EUGENIA SOLIS HINGPIT, MD"));
Check("no placeholder text left anywhere", !doc.Contains("[SET"));
Check("all 20 document types have a readable name",
    Enum.GetValues<DocumentType>().All(t => !string.IsNullOrWhiteSpace(FeeSchedule.NameOf(t))));

Console.WriteLine($"\n=== {pass} passed, {fail} failed ===");
return fail == 0 ? 0 : 1;
