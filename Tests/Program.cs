using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BarangayDocumentSystem.Configuration;
using BarangayDocumentSystem.Controls;
using BarangayDocumentSystem.Data;
using BarangayDocumentSystem.Documents;
using BarangayDocumentSystem.Forms;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Printing;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Tests
{
    internal static class Program
    {
        private static int passed;
        private static int failed;
        private static int skipped;
        private static bool useSql;

        [STAThread]
        private static int Main(string[] args)
        {
            useSql = args.Contains("--sql");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("Process: " + (IntPtr.Size * 8) + "-bit; CLR: " + Environment.Version);
            Console.WriteLine("Storage: in memory (the revamp\u0027s --sql switch is retired; MySQL is covered by the app itself)");
            Run("Dashboard totals include paid rejections and empty categories", Reporting);
            Run("Stale resident edits cannot overwrite newer records", StaleEdits);
            Run("Transactions roll back related changes", TransactionRollback);
            Run("Resident create, search, edit, and delete", ResidentCrud);
            Run("Invalid input is rejected without changing stored records", InvalidResidents);
            Run("Repository copies isolate unsaved edits", CopyIsolation);
            Run("All document rates and exemption precedence", FeeRules);
            Run("Charter certification fees are displayed and saved", CharterCertificationFees);
            Run("Valid workflow and required payment", Workflow);
            Run("Invalid transitions leave requests unchanged", InvalidTransitions);
            Run("Receipts are required and unique across requests", ReceiptRules);
            Run("Rejected requests preserve payments and history", RejectionHistory);
            Run("Jobseeker eligibility and once-only use", JobseekerRules);
            Run("Released documents survive resident edits", ReleasedSnapshot);
            Run("Business details and document purposes are required", RequestValidation);
            Run("All seven templates generate distinct documents", DocumentTemplates);
            Run("Text pagination consumes every character", Pagination);
            Run("Printer preview renders a multi-page document without printing", PrinterPreview);
            Run("Sample data and empty startup both work", SampleRecords);
            Run("Main window loads the dashboard and navigates to all pages", MainNavigation);
            Run("Page filters and workflow buttons are connected", PageInteractions);
            Run("Dialog cancel buttons close without saving", CancelDialogs);
            Run("Forms and navigation render at normal and minimum sizes", UiSmoke);
            Run("Navy theme tokens, fonts, and drawing helpers", ThemeChecks.Run);
            Console.WriteLine();
            Console.WriteLine(passed + " passed; " + failed + " failed; " + skipped + " skipped.");
            return failed == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (NotSupportedException error) { skipped++; Console.WriteLine("SKIP " + name + ": " + error.Message); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + Environment.NewLine + error); }
            finally
            {
                foreach (var database in databases)
                {
                    try { database.Dispose(); }
                    catch (Exception error) { failed++; Console.WriteLine("FAIL test database cleanup: " + error.Message); }
                }
                databases.Clear();
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Reporting()
        {
            var fixture = new Fixture();
            var empty = fixture.Reporting.GetStatistics();
            Check(empty.TotalResidents == 0 && empty.TotalCollected == 0m, "Empty dashboard must have zero totals.");
            Check(empty.RequestsByStatus.Count == 5 && empty.RequestsByStatus.All(item => item.Value == 0), "Empty status categories disappeared.");
            FixtureData.Load(fixture.Residents, fixture.Requests);
            var totals = fixture.Reporting.GetStatistics();
            Check(totals.TotalResidents == 7 && totals.TotalRequests == 6 && totals.TotalCollected == 200m, "Sample dashboard totals differ.");
            Check(totals.PendingRequests == 2 && totals.ReadyRequests == 1 && totals.FreeDocumentsReleased == 1, "Workflow totals differ.");
            Check(totals.RequestsByDocument.Sum(item => item.Value) == 6 && totals.ResidentsByPurok.Sum(item => item.Value) == 7, "Grouped totals differ.");
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId);
            fixture.Requests.RecordPayment(request.RequestId, "REPORT-OR");
            fixture.Requests.Reject(request.RequestId, "Cancelled by resident");
            Check(fixture.Reporting.GetStatistics().TotalCollected == 300m, "Rejecting a paid request erased collection history.");
        }

        private static void StaleEdits()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var oldEdit = fixture.Residents.Get(resident.ResidentId);
            var newEdit = fixture.Residents.Get(resident.ResidentId);
            newEdit.Address = "Newer saved address";
            fixture.Residents.Save(newEdit);
            oldEdit.Address = "Outdated address";
            Rejects(() => fixture.Residents.Save(oldEdit));
            Check(fixture.Residents.Get(resident.ResidentId).Address == "Newer saved address", "An old form overwrote a newer save.");
        }

        private static void TransactionRollback()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate);
            fixture.Requests.StartProcessing(request.RequestId);
            fixture.Requests.MarkReady(request.RequestId);
            Rejects(() => fixture.Repository.ExecuteInTransaction(() =>
            {
                fixture.Requests.Release(request.RequestId);
                fixture.AddResident();
                throw new InvalidOperationException("Simulated failure before commit");
            }));
            Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.ReadyForRelease, "Request release was not rolled back.");
            Check(!fixture.Residents.Get(resident.ResidentId).HasUsedJobseekerBenefit, "Benefit flag was not rolled back.");
            Check(fixture.Residents.Search().Count == 1, "Resident insert was not rolled back.");
            fixture.Requests.Release(request.RequestId);
            Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.Released &&
                fixture.Residents.Get(resident.ResidentId).HasUsedJobseekerBenefit, "Release and benefit flag were not committed together.");
        }

        private static void Rejects(Action action)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected an invalid operation to be rejected.");
        }

        private static Resident ValidResident(string firstName = "Test")
        {
            return new Resident
            {
                FirstName = firstName, LastName = "Resident",
                DateOfBirth = DateTime.Today.AddYears(-30), DateOfResidency = DateTime.Today.AddYears(-2),
                Address = "123 Sample Street", Purok = "Purok 1", ContactNumber = "09171234567"
            };
        }

        private static void ResidentCrud()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            Check(resident.ResidentId > 0, "Saved residents need IDs.");
            Check(fixture.Residents.Search("test").Count == 1, "Name search failed.");
            Check(fixture.Residents.Search("PUROK 1").Count == 1, "Purok search must ignore case.");
            var changed = fixture.Residents.Get(resident.ResidentId);
            changed.LastName = "Updated";
            fixture.Residents.Save(changed);
            Check(fixture.Residents.Search("Updated").Count == 1, "Edit was not saved.");
            fixture.Residents.Delete(resident.ResidentId);
            Check(fixture.Residents.Search().Count == 0, "Delete failed.");
            Rejects(() => fixture.Residents.Get(resident.ResidentId));
        }

        private static void InvalidResidents()
        {
            var fixture = new Fixture();
            Action<Resident>[] invalidEdits =
            {
                resident => resident.FirstName = " ",
                resident => resident.LastName = "",
                resident => resident.Address = "",
                resident => resident.Purok = "",
                resident => resident.ContactNumber = "+++----",
                resident => resident.ContactNumber = "0917ABC5678",
                resident => resident.ContactNumber = "12",
                resident => resident.ContactNumber = "０９１７１２３４５６７",
                resident => resident.DateOfBirth = DateTime.Today.AddDays(1),
                resident => resident.DateOfBirth = DateTime.Today.AddYears(-131),
                resident => resident.DateOfResidency = DateTime.Today.AddDays(1),
                resident => resident.DateOfResidency = resident.DateOfBirth.AddDays(-1),
                resident => resident.IsSeniorCitizen = true,
                resident => resident.Gender = (Gender)999,
                resident => resident.CivilStatus = (CivilStatus)999,
                resident => resident.FirstName = "Test\nInjected",
                resident => resident.LastName = new string('x', 81)
            };
            foreach (var edit in invalidEdits)
            {
                var resident = ValidResident();
                edit(resident);
                Rejects(() => fixture.Residents.Save(resident));
            }
            Check(fixture.Residents.Search().Count == 0, "An invalid resident was stored.");
            var saved = fixture.AddResident();
            var invalid = fixture.Residents.Get(saved.ResidentId);
            invalid.DateOfResidency = DateTime.Today.AddDays(1);
            Rejects(() => fixture.Residents.Save(invalid));
            Check(fixture.Residents.Get(saved.ResidentId).DateOfResidency == saved.DateOfResidency,
                "A failed update changed the stored resident.");
        }

        private static void CopyIsolation()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            resident.FirstName = "Unsaved";
            Check(fixture.Residents.Get(resident.ResidentId).FirstName == "Test", "Caller changed repository data.");
            var returned = fixture.Residents.Get(resident.ResidentId);
            returned.Address = "Unsaved address";
            Check(fixture.Residents.Get(resident.ResidentId).Address != returned.Address, "Read exposed mutable storage.");
            var request = fixture.Create(resident.ResidentId);
            request.ResidentSnapshot.LastName = "Tampered";
            Check(fixture.Requests.Get(request.RequestId).ResidentSnapshot.LastName == "Resident", "Request snapshot escaped storage.");
        }

        private static void FeeRules()
        {
            var schedule = new FeeSchedule();
            var ordinary = ValidResident();
            var rates = new Dictionary<DocumentType, decimal>
            {
                { DocumentType.BarangayClearance, 100m }, { DocumentType.CertificateOfResidency, 100m },
                { DocumentType.CertificateOfIndigency, 0m }, { DocumentType.BarangayBusinessClearance, 200m },
                { DocumentType.BarangayId, 100m }, { DocumentType.FirstTimeJobseekerCertificate, 0m },
                { DocumentType.CertificateOfGoodMoralCharacter, 100m }
            };
            foreach (var rate in rates)
            {
                var assessment = schedule.Assess(ordinary, rate.Key);
                Check(assessment.Amount == rate.Value && !string.IsNullOrWhiteSpace(assessment.Basis), "Incorrect base fee or missing basis.");
            }
            foreach (Action<Resident> classify in new Action<Resident>[]
            {
                resident => resident.IsIndigent = true,
                resident => { resident.DateOfBirth = DateTime.Today.AddYears(-70); resident.IsSeniorCitizen = true; },
                resident => resident.IsPersonWithDisability = true
            })
            {
                var exempt = ValidResident();
                classify(exempt);
                Check(schedule.Assess(exempt, DocumentType.BarangayClearance).Amount == 0m, "Personal exemption missing.");
                foreach (var type in new[] { DocumentType.CertificateOfResidency, DocumentType.CertificateOfGoodMoralCharacter })
                {
                    var assessment = schedule.Assess(exempt, type);
                    Check(assessment.Amount == 0m && assessment.Basis.StartsWith("Project policy:"),
                        "Certification base fee replaced a classroom exemption or mislabeled its basis.");
                }
                Check(schedule.Assess(exempt, DocumentType.BarangayBusinessClearance).Amount == 200m, "Business fee was waived.");
            }
            ordinary.IsStudent = true;
            ordinary.IsSoloParent = true;
            Check(schedule.Assess(ordinary, DocumentType.BarangayClearance).Amount == 100m, "Clearance local fee is not the Charter rate.");
            Check(schedule.Assess(ordinary, DocumentType.BarangayClearance, ClearanceScope.Abroad).Amount == 200m, "Clearance abroad fee is not the Charter rate.");
            Rejects(() => schedule.Assess(ordinary, (DocumentType)999));
        }

        private static void CharterCertificationFees()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            Directory.CreateDirectory(folder);
            foreach (var type in new[] { DocumentType.CertificateOfResidency, DocumentType.CertificateOfGoodMoralCharacter })
            {
                using (var form = new RequestForm(fixture.Residents, fixture.Requests, fixture.Renderer, resident.ResidentId))
                {
                    var documents = (ComboBox)Field(form, "cmbDocument");
                    documents.SelectedItem = documents.Items.Cast<IDocumentTemplate>().Single(item => item.DocumentType == type);
                    ((TextBox)Field(form, "txtPurpose")).Text = "School requirement";
                    Capture(form, Path.Combine(folder, type + "-request.png"));
                    string feeText = ((Label)Field(form, "lblFee")).Text;
                    Check(feeText.Contains("PHP " + 100m.ToString("N2")) && feeText.Contains("Citizen's Charter"),
                        type + " must display the PHP 100 Charter fee.");
                    Click(form, "btnSubmit");
                    Check(form.DialogResult == DialogResult.OK, "Certification request was not submitted.");
                }
                var request = fixture.Requests.Search().Single(item => item.DocumentType == type);
                Check(request.Fee == 100m && request.FeeBasis.Contains("Citizen's Charter"),
                    type + " must save the PHP 100 fee and its Charter basis.");
            }
        }

        private static void Workflow()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId);
            fixture.Requests.StartProcessing(request.RequestId);
            fixture.Requests.MarkReady(request.RequestId);
            Rejects(() => fixture.Requests.Release(request.RequestId));
            Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.ReadyForRelease, "Failed release changed status.");
            fixture.Requests.RecordPayment(request.RequestId, "OR-001");
            fixture.Requests.Release(request.RequestId);
            var released = fixture.Requests.Get(request.RequestId);
            Check(released.Status == RequestStatus.Released && released.DateReleased.HasValue, "Release was not recorded.");
            Check(!released.ReleasedDocumentText.Contains("DRAFT -"), "Final document is marked as draft.");
            var free = fixture.Create(resident.ResidentId, DocumentType.CertificateOfIndigency);
            fixture.Requests.StartProcessing(free.RequestId);
            fixture.Requests.MarkReady(free.RequestId);
            fixture.Requests.Release(free.RequestId);
            Check(fixture.Requests.Get(free.RequestId).Status == RequestStatus.Released, "Free request needed payment.");
        }

        private static void InvalidTransitions()
        {
            var fixture = new Fixture();
            var request = fixture.Create(fixture.AddResident().ResidentId);
            Rejects(() => fixture.Requests.MarkReady(request.RequestId));
            Rejects(() => fixture.Requests.Release(request.RequestId));
            Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.Pending, "Invalid transition mutated state.");
            fixture.Requests.StartProcessing(request.RequestId);
            Rejects(() => fixture.Requests.StartProcessing(request.RequestId));
            Rejects(() => fixture.Requests.Reject(request.RequestId, " "));
            Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.Processing, "Blank rejection mutated state.");
            fixture.Requests.Reject(request.RequestId, "Incomplete supporting details");
            Rejects(() => fixture.Requests.MarkReady(request.RequestId));
            Rejects(() => fixture.Requests.Release(request.RequestId));
            Rejects(() => fixture.Requests.Reject(request.RequestId, "Rewrite the reason"));
            Rejects(() => fixture.Requests.RecordPayment(request.RequestId, "OR-100"));
            Check(fixture.Requests.Get(request.RequestId).RejectionReason == "Incomplete supporting details", "Rejection history changed.");
        }

        private static void ReceiptRules()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var first = fixture.Create(resident.ResidentId);
            var second = fixture.Create(resident.ResidentId);
            Rejects(() => fixture.Requests.RecordPayment(first.RequestId, ""));
            Check(!fixture.Requests.Get(first.RequestId).IsPaid, "Empty receipt recorded payment.");
            fixture.Requests.RecordPayment(first.RequestId, "  OR-002  ");
            Rejects(() => fixture.Requests.RecordPayment(first.RequestId, "OR-003"));
            Rejects(() => fixture.Requests.RecordPayment(second.RequestId, "or-002"));
            Rejects(() => fixture.Requests.RecordPayment(second.RequestId, "OR\n003"));
            Check(!fixture.Requests.Get(second.RequestId).IsPaid, "Duplicate receipt recorded payment.");
            var free = fixture.Create(resident.ResidentId, DocumentType.CertificateOfIndigency);
            Rejects(() => fixture.Requests.RecordPayment(free.RequestId, "OR-004"));
        }

        private static void RejectionHistory()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId);
            fixture.Requests.RecordPayment(request.RequestId, "OR-PAID");
            fixture.Requests.Reject(request.RequestId, "Supporting documents not provided");
            var rejected = fixture.Requests.Get(request.RequestId);
            Check(rejected.IsPaid && rejected.OfficialReceiptNumber == "OR-PAID", "Rejection erased the payment.");
            Rejects(() => fixture.Residents.Delete(resident.ResidentId));
            Check(fixture.Requests.Search().Count == 1, "Delete removed request history.");
        }

        private static void JobseekerRules()
        {
            var fixture = new Fixture();
            var resident = ValidResident();
            resident.DateOfResidency = DateTime.Today.AddMonths(-6).AddDays(1);
            fixture.Residents.Save(resident);
            Rejects(() => fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate));
            resident.DateOfResidency = DateTime.Today.AddMonths(-6);
            fixture.Residents.Save(resident);
            var first = fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate);
            Rejects(() => fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate));
            fixture.Requests.Reject(first.RequestId, "Applicant withdrew");
            var replacement = fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate);
            fixture.Requests.StartProcessing(replacement.RequestId);
            fixture.Requests.MarkReady(replacement.RequestId);
            resident.DateOfResidency = DateTime.Today.AddMonths(-2);
            fixture.Residents.Save(resident);
            Rejects(() => fixture.Requests.Release(replacement.RequestId));
            resident.DateOfResidency = DateTime.Today.AddYears(-1);
            fixture.Residents.Save(resident);
            fixture.Requests.Release(replacement.RequestId);
            Check(fixture.Residents.Get(resident.ResidentId).HasUsedJobseekerBenefit, "Benefit use was not recorded.");
            Rejects(() => fixture.Create(resident.ResidentId, DocumentType.FirstTimeJobseekerCertificate));
            resident.HasUsedJobseekerBenefit = false;
            Rejects(() => fixture.Residents.Save(resident));
        }

        private static void ReleasedSnapshot()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId, DocumentType.CertificateOfIndigency);
            fixture.Requests.StartProcessing(request.RequestId);
            fixture.Requests.MarkReady(request.RequestId);
            fixture.Requests.Release(request.RequestId);
            string before = fixture.Requests.Preview(request.RequestId);
            resident.FirstName = "Changed";
            resident.Address = "Completely different address";
            fixture.Residents.Save(resident);
            Check(fixture.Requests.Preview(request.RequestId) == before, "Resident edits rewrote a released document.");
            Check(fixture.Requests.Get(request.RequestId).ResidentName == "Test Resident", "Historical name changed.");
            Rejects(() => fixture.Requests.Reject(request.RequestId, "Change history"));
            Rejects(() => fixture.Requests.RecordPayment(request.RequestId, "Late receipt"));
            Rejects(() => fixture.Residents.Delete(resident.ResidentId));
        }

        private static void RequestValidation()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            Rejects(() => fixture.Requests.Create(new RequestDetails { ResidentId = resident.ResidentId }));
            Rejects(() => fixture.Requests.Create(new RequestDetails
            {
                ResidentId = resident.ResidentId, DocumentType = DocumentType.BarangayBusinessClearance, Purpose = "Permit"
            }));
            Rejects(() => fixture.Requests.Create(new RequestDetails { ResidentId = 999, Purpose = "Test" }));
            Check(fixture.Requests.Search().Count == 0, "Invalid request reached storage.");
        }

        private static void DocumentTemplates()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            Check(fixture.Renderer.Templates.Count == 7, "Expected seven registered templates.");
            foreach (var template in fixture.Renderer.Templates)
            {
                var request = fixture.Create(resident.ResidentId, template.DocumentType);
                string text = fixture.Requests.Preview(request.RequestId);
                Check(!text.Replace("\r\n", "").Contains("\n"), "Document contains line endings that the Windows text preview cannot display.");
                Check(text.Contains("DRAFT -") && text.Contains(template.Title.ToUpperInvariant()) &&
                    text.ToUpperInvariant().Contains("TEST RESIDENT"),
                    "Missing document title or resident.");
                if (template.DocumentType == DocumentType.FirstTimeJobseekerCertificate)
                    Check(text.Contains("OATH OF UNDERTAKING"), "Jobseeker oath is missing.");
                if (template.DocumentType == DocumentType.BarangayBusinessClearance)
                    Check(text.Contains("Sample Store") && text.Contains("Retail"), "Business details are missing.");
            }
        }

        private static void Pagination()
        {
            string text = string.Join(Environment.NewLine, Enumerable.Range(1, 500).Select(number =>
                "Line " + number + ": document text with enough detail to span multiple pages."));
            using (var image = new Bitmap(850, 1100))
            using (var graphics = Graphics.FromImage(image))
            using (var font = new Font("Segoe UI", 11F))
            {
                int offset = 0;
                int pages = 0;
                while (offset < text.Length)
                {
                    int count = TextPaginator.DrawPage(graphics, font, text, offset, new RectangleF(60, 60, 700, 950));
                    Check(count > 0 && count <= text.Length - offset, "Pagination made invalid progress.");
                    offset += count;
                    if (++pages > 100) throw new Exception("Pagination did not terminate.");
                }
                Check(pages > 1 && offset == text.Length, "Long document was truncated.");
                Check(TextPaginator.DrawPage(graphics, font, "", 0, new RectangleF(0, 0, 10, 10)) == 0, "Empty text generated a page.");
            }
        }

        private static void PrinterPreview()
        {
            string printer = PrinterSettings.InstalledPrinters.Cast<string>()
                .FirstOrDefault(name => name == "Microsoft Print to PDF");
            if (printer == null) throw new NotSupportedException("Microsoft Print to PDF is not installed.");
            var fixture = new Fixture();
            var request = fixture.Create(fixture.AddResident().ResidentId, DocumentType.CertificateOfIndigency);
            string text = fixture.Requests.Preview(request.RequestId) + Environment.NewLine +
                string.Join(Environment.NewLine, Enumerable.Range(1, 140).Select(number => "Pagination check line " + number));
            using (var job = new DocumentPrintJob("Preview test", text))
            {
                var controller = new PreviewPrintController();
                job.PrinterSettings.PrinterName = printer;
                job.PrintController = controller;
                job.Print();
                var pages = controller.GetPreviewPageInfo();
                try
                {
                    Check(pages.Length > 1, "Expected multiple print pages.");
                    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                    Directory.CreateDirectory(folder);
                    using (var bitmap = new Bitmap(pages[0].PhysicalSize.Width, pages[0].PhysicalSize.Height))
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.White);
                        graphics.DrawImage(pages[0].Image, new Rectangle(Point.Empty, bitmap.Size));
                        bitmap.Save(Path.Combine(folder, "printed-page.png"));
                        bool hasText = false;
                        for (int y = 50; y < bitmap.Height - 50; y += 2)
                            for (int x = 50; x < bitmap.Width - 50; x += 2)
                                if (bitmap.GetPixel(x, y).R < 180) hasText = true;
                        Check(hasText, "Printer preview page is blank.");
                    }
                }
                finally
                {
                    foreach (var page in pages) page.Image.Dispose();
                }
            }
        }

        private static void SampleRecords()
        {
            var fixture = new Fixture();
            Check(fixture.Residents.Search().Count == 0 && fixture.Requests.Search().Count == 0, "Empty repository has records.");
            FixtureData.Load(fixture.Residents, fixture.Requests);
            Check(fixture.Residents.Search().Count == 7 && fixture.Requests.Search().Count == 6, "Sample counts are wrong.");
            Check(fixture.Requests.Search().Where(request => request.IsPaid).Sum(request => request.Fee) == 200m,
                "Sample collection total is wrong.");
            var carlo = fixture.Residents.Search("Carlo").Single();
            Rejects(() => fixture.Create(carlo.ResidentId, DocumentType.FirstTimeJobseekerCertificate));
        }

        private static void MainNavigation()
        {
            var fixture = new Fixture();
            using (var main = new MainForm(fixture.Residents, fixture.Requests, fixture.Renderer, AppSettings.Load(), fixture.Reporting))
            {
                main.Opacity = 0;
                main.ShowInTaskbar = false;
                main.Show();
                Application.DoEvents();
                var content = (Panel)Field(main, "pnlContent");
                Check(content.Controls.Count == 3, "The main window must load all three pages.");
                Check(content.Controls.OfType<DashboardControl>().Single().Visible, "Dashboard must be visible at startup.");
                string[] buttons = { "btnResidents", "btnRequests", "btnDashboard" };
                Type[] pages = { typeof(ResidentsControl), typeof(RequestsControl), typeof(DashboardControl) };
                for (int index = 0; index < buttons.Length; index++)
                {
                    var button = main.Controls.Find(buttons[index], true).Single() as Button;
                    Check(button != null, "Missing navigation button: " + buttons[index]);
                    button.PerformClick();
                    Application.DoEvents();
                    var visible = content.Controls.Cast<Control>().Where(page => page.Visible).ToList();
                    Check(visible.Count == 1 && pages[index].IsInstanceOfType(visible[0]), "Navigation selected the wrong page.");
                    Check(visible[0].Width > 0 && visible[0].Height > 0 && visible[0].Controls.Count > 0,
                        "The selected page is empty or has no display area.");
                }
                main.Hide();
            }
        }

        private static void PageInteractions()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var second = ValidResident("Second");
            fixture.Residents.Save(second);
            var request = fixture.Create(resident.ResidentId);
            using (var host = new Form { Opacity = 0, ShowInTaskbar = false })
            using (var residents = new ResidentsControl(fixture.Residents, fixture.Requests, fixture.Renderer))
            using (var requests = new RequestsControl(fixture.Residents, fixture.Requests, fixture.Renderer))
            {
                host.Controls.Add(residents);
                host.Show();
                Application.DoEvents();
                var search = (TextBox)Field(residents, "txtSearch");
                var grid = (DataGridView)Field(residents, "gridResidents");
                search.Text = "Second";
                Check(grid.Rows.Count == 1 && ((Resident)grid.Rows[0].DataBoundItem).ResidentId == second.ResidentId,
                    "Resident search did not refresh the grid.");
                search.Clear();
                Check(grid.Rows.Count == 2, "Clearing resident search did not restore the rows.");

                host.Controls.Remove(residents);
                host.Controls.Add(requests);
                Application.DoEvents();
                ((Button)Field(requests, "btnProcess")).PerformClick();
                Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.Processing,
                    "Start processing button is not connected.");
                ((Button)Field(requests, "btnReady")).PerformClick();
                Check(fixture.Requests.Get(request.RequestId).Status == RequestStatus.ReadyForRelease,
                    "Mark ready button is not connected.");
                Check(!((Button)Field(requests, "btnRelease")).Enabled, "Unpaid request can be released.");
                var status = (ComboBox)Field(requests, "cmbStatus");
                var requestGrid = (DataGridView)Field(requests, "gridRequests");
                status.SelectedIndex = 1;
                Check(requestGrid.Rows.Count == 0 && !((Button)Field(requests, "btnPay")).Enabled,
                    "Status filtering did not clear the selection and its actions.");
                status.SelectedIndex = 0;
                Check(requestGrid.Rows.Count == 1, "All statuses did not restore the request.");
                ((TextBox)Field(requests, "txtSearch")).Text = "No matching resident";
                Check(requestGrid.Rows.Count == 0, "Request search did not refresh the grid.");
                host.Hide();
            }
        }

        private static void CancelDialogs()
        {
            var fixture = new Fixture();
            var resident = fixture.AddResident();
            var request = fixture.Create(resident.ResidentId);
            using (var form = new ResidentForm(fixture.Residents)) CheckCancel(form, "btnCancel");
            using (var form = new RequestForm(fixture.Residents, fixture.Requests, fixture.Renderer)) CheckCancel(form, "btnCancel");
            using (var form = new PaymentForm(fixture.Requests, request)) CheckCancel(form, "btnCancel");
            using (var form = new RejectionForm(fixture.Requests, request)) CheckCancel(form, "btnCancel");
            using (var form = new DocumentPreviewForm(request, fixture.Requests.Preview(request.RequestId))) CheckCancel(form, "btnClose");
            Check(fixture.Residents.Search().Count == 1 && fixture.Requests.Search().Count == 1,
                "Canceling created a record.");
            var saved = fixture.Requests.Get(request.RequestId);
            Check(saved.Status == RequestStatus.Pending && !saved.IsPaid, "Canceling changed the request.");
        }

        private static void CheckCancel(Form form, string buttonName)
        {
            Click(form, buttonName);
            Check(form.IsDisposed && form.DialogResult == DialogResult.Cancel, "Cancel did not close the dialog.");
        }

        private static void Click(Form form, string buttonName)
        {
            form.Opacity = 0;
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            ((Button)Field(form, buttonName)).PerformClick();
        }

        private static void UiSmoke()
        {
            var settings = AppSettings.Load();
            var fixture = new Fixture();
            FixtureData.Load(fixture.Residents, fixture.Requests);
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            Directory.CreateDirectory(folder);
            using (var main = new MainForm(fixture.Residents, fixture.Requests, fixture.Renderer, settings, fixture.Reporting))
            {
                Capture(main, Path.Combine(folder, "dashboard.png"));
                foreach (string page in new[] { "residents", "requests" })
                {
                    Invoke(main, page == "residents" ? "NavigateResidents" : "NavigateRequests", main, EventArgs.Empty);
                    Capture(main, Path.Combine(folder, page + ".png"));
                    main.Size = main.MinimumSize;
                    Capture(main, Path.Combine(folder, page + "-minimum.png"));
                    main.ClientSize = new Size(1280, 780);
                }
            }
            using (var form = new ResidentForm(fixture.Residents))
            {
                Capture(form, Path.Combine(folder, "resident-form.png"));
                ((TextBox)Field(form, "txtFirstName")).Text = "Form";
                ((TextBox)Field(form, "txtLastName")).Text = "Saved";
                ((TextBox)Field(form, "txtPurok")).Text = "Purok 6";
                ((TextBox)Field(form, "txtAddress")).Text = "42 Test Street";
                ((TextBox)Field(form, "txtContact")).Text = "09170001111";
                Click(form, "btnSaveResident");
                Check(fixture.Residents.Search("Form Saved").Count == 1, "Resident form did not save through the service.");
            }
            var resident = fixture.Residents.Search("Form Saved").Single();
            using (var form = new ResidentForm(fixture.Residents, resident))
            {
                Capture(form, Path.Combine(folder, "resident-edit-form.png"));
                ((TextBox)Field(form, "txtAddress")).Text = "Edited through the form";
                Click(form, "btnSaveResident");
                Check(fixture.Residents.Get(resident.ResidentId).Address == "Edited through the form", "Resident form did not retain the saved record version.");
            }
            using (var form = new RequestForm(fixture.Residents, fixture.Requests, fixture.Renderer, resident.ResidentId))
            {
                ((ComboBox)Field(form, "cmbDocument")).SelectedIndex = 3;
                Capture(form, Path.Combine(folder, "business-request-form.png"));
                ((TextBox)Field(form, "txtPurpose")).Text = "Permit";
                ((TextBox)Field(form, "txtBusinessName")).Text = "Form Store";
                ((TextBox)Field(form, "txtBusinessAddress")).Text = "42 Test Street";
                ((TextBox)Field(form, "txtBusinessNature")).Text = "Retail";
                Click(form, "btnSubmit");
            }
            var request = fixture.Requests.Search("Form Saved").Single();
            using (var form = new PaymentForm(fixture.Requests, request))
            {
                Capture(form, Path.Combine(folder, "payment-form.png"));
                ((TextBox)Field(form, "txtReceipt")).Text = "FORM-OR-001";
                Click(form, "btnSave");
                Check(fixture.Requests.Get(request.RequestId).IsPaid, "Payment form did not save payment.");
            }
            using (var form = new RejectionForm(fixture.Requests, fixture.Requests.Get(request.RequestId)))
                Capture(form, Path.Combine(folder, "rejection-form.png"));
            using (var form = new DocumentPreviewForm(request, fixture.Requests.Preview(request.RequestId)))
            {
                Capture(form, Path.Combine(folder, "document-preview.png"));
                Check(!((Button)Field(form, "btnPrint")).Enabled, "Draft printing must be disabled.");
            }
        }

        private static object Field(object instance, string name)
        {
            return instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        }

        private static void Invoke(object instance, string name, params object[] arguments)
        {
            instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, arguments);
        }

        private static void Capture(Form form, string path)
        {
            form.Opacity = 0;
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            form.PerformLayout();
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(path);
            }
            form.Hide();
        }

        private class Fixture
        {
            public IBarangayRepository Repository { get; private set; }
            public ReportingService Reporting { get; private set; }
            public ResidentService Residents { get; private set; }
            public RequestService Requests { get; private set; }
            public DocumentRenderer Renderer { get; private set; }

            public Fixture(IBarangayRepository repository = null)
            {
                if (repository == null) repository = useSql ? (IBarangayRepository)NewDatabase().OpenRepository() : new InMemoryBarangayRepository();
                Repository = repository;
                Reporting = new ReportingService(repository);
                Residents = new ResidentService(repository);
                Renderer = new DocumentRenderer(new BarangayProfile("Test Barangay", "Test City", "Test Province", "Test Official"),
                    new IDocumentTemplate[] { new ClearanceTemplate(), new ResidencyTemplate(), new IndigencyTemplate(),
                        new BusinessClearanceTemplate(), new BarangayIdTemplate(), new JobseekerTemplate(), new GoodMoralTemplate() });
                Requests = new RequestService(repository, new FeeSchedule(), Renderer);
            }

            public Resident AddResident()
            {
                var resident = ValidResident();
                Residents.Save(resident);
                return resident;
            }

            public DocumentRequest Create(int residentId, DocumentType type = DocumentType.BarangayClearance)
            {
                return Requests.Create(new RequestDetails
                {
                    ResidentId = residentId, DocumentType = type, Purpose = "Test purpose",
                    BusinessName = "Sample Store", BusinessAddress = "Sample Street", BusinessNature = "Retail"
                });
            }
        }
    }
}
