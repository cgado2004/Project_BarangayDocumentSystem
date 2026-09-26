using System;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;


/// Puts seven demonstration residents (and a few requests) into an EMPTY
/// database so the app has something to show on first run.
///
/// The residents are chosen to exercise every fee rule and the RA 11261
/// (First Time Jobseekers) test — see the README for the list.
public static class SampleDataSeeder
{
    public static void Seed(IBarangayRepository repository)
    {
        var today = DateTime.Today;

        Resident Add(string fn, string mn, string ln, string sfx, DateTime dob, Gender g,
                     CivilStatus cs, string purok, string addr, string contact, string occ,
                     DateTime since, bool voter, ResidentClassification cls) =>
            repository.AddResident(new ResidentDetails(fn, mn, ln, sfx, dob, g, cs, purok, addr,
                                                       contact, occ, since, voter, cls));

        var juan = Add("Juan", "Perez", "Dela Cruz", "", new DateTime(1985, 4, 12),
            Gender.Male, CivilStatus.Married, "Purok 1", "123 Rizal Street",
            "09171234567", "Tricycle Driver", new DateTime(2010, 6, 1), true,
            ResidentClassification.None);

        var maria = Add("Maria", "Santos", "Reyes", "", new DateTime(1955, 9, 3),
            Gender.Female, CivilStatus.Widowed, "Purok 2", "45 Bonifacio Avenue",
            "09181234567", "Retired", new DateTime(1998, 1, 15), true,
            ResidentClassification.SeniorCitizen);

        // 14 months' residency, never availed — qualifies under RA 11261.
        var jose = Add("Jose", "Cruz", "Bautista", "Jr.", new DateTime(2004, 2, 20),
            Gender.Male, CivilStatus.Single, "Purok 3", "78 Mabini Street",
            "09191234567", "Fresh Graduate", today.AddMonths(-14), true,
            ResidentClassification.None);

        var ana = Add("Ana", "Lopez", "Villanueva", "", new DateTime(1992, 11, 8),
            Gender.Female, CivilStatus.Single, "Purok 4", "12 Quezon Street",
            "09201234567", "Sari-sari Store Owner", new DateTime(2015, 3, 20), true,
            ResidentClassification.SoloParent);

        var pedro = Add("Pedro", "Ramos", "Mendoza", "", new DateTime(1978, 7, 25),
            Gender.Male, CivilStatus.Married, "Purok 5", "90 Magsaysay Street",
            "09211234567", "Carpenter", new DateTime(2005, 8, 10), false,
            ResidentClassification.Indigent);

        var liza = Add("Liza", "Garcia", "Torres", "", new DateTime(1999, 5, 30),
            Gender.Female, CivilStatus.Single, "Purok 2", "56 Del Pilar Street",
            "09221234567", "Student", new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD);

        // Only 2 months — deliberately FAILS the RA 11261 six-month test.
        Add("Carlo", "Diaz", "Aquino", "", new DateTime(2003, 12, 5),
            Gender.Male, CivilStatus.Single, "Purok 1", "34 Luna Street",
            "09231234567", "Unemployed", today.AddMonths(-2), false,
            ResidentClassification.None);

        // Workflow changes happen on the request object, so each one is
        // followed by SaveRequest to store it.
        var r1 = repository.CreateRequest(juan, DocumentType.BarangayClearance, "Employment Requirement");
        r1.StartProcessing(); r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101"); r1.Release();
        repository.SaveRequest(r1);

        var r2 = repository.CreateRequest(maria, DocumentType.CertificateOfResidency, "Pension Claim");
        r2.StartProcessing(); r2.MarkReadyForRelease(); r2.Release();
        repository.SaveRequest(r2);

        var r3 = repository.CreateRequest(jose, DocumentType.FirstTimeJobseekerCertificate,
            "NBI Clearance Application");
        r3.StartProcessing();
        repository.SaveRequest(r3);

        var r4 = repository.CreateRequest(pedro, DocumentType.CertificateOfIndigency,
            "Medical Assistance at Davao Regional Medical Center");
        r4.StartProcessing(); r4.MarkReadyForRelease();
        repository.SaveRequest(r4);

        repository.CreateRequest(ana, DocumentType.BarangayBusinessClearance, "Sari-sari Store Renewal");
        repository.CreateRequest(liza, DocumentType.CertificateOfGoodMoralCharacter, "Scholarship Application");
    }
}
