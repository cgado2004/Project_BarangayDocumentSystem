using System;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Services;

namespace BarangayDocumentSystem.Data
{
    public static class SampleData
    {
        public static void Load(ResidentService residents, RequestService requests)
        {
            var juan = CreateResident("Juan", "Dela Cruz", 35, "Purok Tandang Sora");
            var maria = CreateResident("Maria", "Reyes", 68, "Purok Orchids");
            maria.IsSeniorCitizen = true;
            var jose = CreateResident("Jose", "Bautista", 22, "Purok Sampaguita");
            var ana = CreateResident("Ana", "Villanueva", 34, "Purok Sunflower");
            ana.IsSoloParent = true;
            var pedro = CreateResident("Pedro", "Mendoza", 48, "Purok Cristo Rey");
            pedro.IsIndigent = true;
            var liza = CreateResident("Liza", "Torres", 25, "Purok Orchids");
            liza.IsStudent = true;
            liza.IsPersonWithDisability = true;
            var carlo = CreateResident("Carlo", "Aquino", 23, "Purok Tandang Sora");
            carlo.DateOfResidency = DateTime.Today.AddMonths(-2);
            foreach (var resident in new[] { juan, maria, jose, ana, pedro, liza, carlo }) residents.Save(resident);

            var paid = requests.Create(new RequestDetails
            {
                ResidentId = juan.ResidentId, DocumentType = DocumentType.BarangayClearance,
                Scope = ClearanceScope.Abroad, Purpose = "Employment requirement, work abroad"
            });
            requests.StartProcessing(paid.RequestId);
            requests.MarkReady(paid.RequestId);
            requests.RecordPayment(paid.RequestId, "DEMO-OR-001");
            requests.Release(paid.RequestId);
            var free = requests.Create(new RequestDetails
            {
                ResidentId = maria.ResidentId, DocumentType = DocumentType.CertificateOfResidency, Purpose = "Pension application"
            });
            requests.StartProcessing(free.RequestId);
            requests.MarkReady(free.RequestId);
            requests.Release(free.RequestId);
            var jobseeker = requests.Create(new RequestDetails
            {
                ResidentId = jose.ResidentId, DocumentType = DocumentType.FirstTimeJobseekerCertificate, Purpose = "Job application"
            });
            requests.StartProcessing(jobseeker.RequestId);
            var indigency = requests.Create(new RequestDetails
            {
                ResidentId = pedro.ResidentId, DocumentType = DocumentType.CertificateOfIndigency, Purpose = "Medical assistance"
            });
            requests.StartProcessing(indigency.RequestId);
            requests.MarkReady(indigency.RequestId);
            requests.Create(new RequestDetails
            {
                ResidentId = ana.ResidentId, DocumentType = DocumentType.BarangayBusinessClearance,
                Purpose = "Business permit application", BusinessName = "Ana's Sample Store",
                BusinessAddress = "Sample Street, Purok 4", BusinessNature = "Retail store"
            });
            requests.Create(new RequestDetails
            {
                ResidentId = liza.ResidentId, DocumentType = DocumentType.CertificateOfGoodMoralCharacter, Purpose = "Scholarship"
            });
        }

        private static Resident CreateResident(string firstName, string lastName, int age, string purok)
        {
            return new Resident
            {
                FirstName = firstName, LastName = lastName, DateOfBirth = DateTime.Today.AddYears(-age),
                DateOfResidency = DateTime.Today.AddYears(-2), Address = "Sample Street", Purok = purok,
                ContactNumber = "09170000000", Occupation = "Sample occupation",
                Gender = firstName == "Maria" || firstName == "Ana" || firstName == "Liza" ? Gender.Female : Gender.Male
            };
        }
    }
}
