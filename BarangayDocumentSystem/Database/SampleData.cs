// =====================================================================
//  PART:    Database - the one copy of my sample residents and requests
//  ORIGIN:  leader_draft - Clint Wood Gado (the seed from my in-memory store)
//           Fdraft - Frent Dhieniel Raborar (the idea of seeding MySQL on an
//           empty database, from his SampleDataSeeder)
//  EDITS:   Clint Wood Gado - merged his seeder and mine into one that
//           works through the repository, so it fills either store
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Database;

/// <summary>
/// My sample residents and requests, using the real purok names of Magugpo
/// Poblacion.
///
/// I gave each person a different situation on purpose, so that during the
/// demo I can show every branch of the fee rules - the flat rates, the two
/// clearance scopes, all four waivers, and every one of the v3.1 variable-fee
/// documents - without inventing data on the spot in front of the panel.
///
/// There used to be two copies of these people: mine inside the in-memory
/// store and Frent's inside his SampleDataSeeder, with different names and
/// his placeholder fees. Now there is one, and it only talks to the
/// repository's public surface (plus the internal File for the one
/// backdated request). That is why the same method can fill the in-memory
/// store for the RuleChecks harness and a freshly created MySQL database
/// on first run - and why the two can never show different demo data.
/// </summary>
internal static class SampleData
{
    public static void Seed(RepositoryBase repository)
    {
        if (repository is null) throw new ArgumentNullException(nameof(repository));

        // ---- the residents -----------------------------------------------

        // He pays full price. I gave him no exemptions at all.
        var juan = repository.AddResident(new ResidentDetails(
            "Juan", "Perez", "Dela Cruz", "", new DateTime(1985, 4, 12),
            Gender.Male, CivilStatus.Married, "Purok Tandang Sora",
            "123 Rizal Street", "09171234567", "Tricycle Driver",
            new DateTime(2010, 6, 1), true, ResidentClassification.None));

        // A senior citizen, so I can show the RA 9994 waiver.
        var maria = repository.AddResident(new ResidentDetails(
            "Maria", "Santos", "Reyes", "", new DateTime(1955, 9, 3),
            Gender.Female, CivilStatus.Widowed, "Purok Orchids",
            "45 Bonifacio Avenue", "09181234567", "Retired",
            new DateTime(1998, 1, 15), true, ResidentClassification.SeniorCitizen));

        // 14 months resident, so he PASSES my RA 11261 six-month test.
        var jose = repository.AddResident(new ResidentDetails(
            "Jose", "Cruz", "Bautista", "Jr.", new DateTime(2004, 2, 20),
            Gender.Male, CivilStatus.Single, "Purok Sampaguita",
            "78 Mabini Street", "09191234567", "Fresh Graduate",
            DateTime.Today.AddMonths(-14), true, ResidentClassification.None));

        // A solo parent who runs a business. I use her to prove that personal
        // exemptions do not apply to a business clearance.
        var ana = repository.AddResident(new ResidentDetails(
            "Ana", "Lopez", "Villanueva", "", new DateTime(1992, 11, 8),
            Gender.Female, CivilStatus.Single, "Purok Sunflower",
            "12 Quezon Street", "09201234567", "Sari-sari Store Owner",
            new DateTime(2015, 3, 20), true, ResidentClassification.SoloParent));

        // An indigent resident, so I can show that waiver too.
        var pedro = repository.AddResident(new ResidentDetails(
            "Pedro", "Ramos", "Mendoza", "", new DateTime(1978, 7, 25),
            Gender.Male, CivilStatus.Married, "Purok Cristo Rey",
            "90 Magsaysay Street", "09211234567", "Carpenter",
            new DateTime(2005, 8, 10), false, ResidentClassification.Indigent));

        // A hyphenated surname, and TWO classifications at once - the
        // resident who proves the classification column must hold a set of
        // flags and not a single value.
        var liza = repository.AddResident(new ResidentDetails(
            "Liza", "Garcia", "Santos-Reyes", "", new DateTime(1999, 5, 30),
            Gender.Female, CivilStatus.Single, "Purok Orchids",
            "56 Del Pilar Street", "09221234567", "Student",
            new DateTime(2019, 6, 1), true,
            ResidentClassification.Student | ResidentClassification.PWD));

        // A surname with ñ, a DITO number, and only 2 months of residency, so
        // he FAILS my RA 11261 test and the request is refused with a reason.
        var carlo = repository.AddResident(new ResidentDetails(
            "Carlo", "Diaz", "Peña", "", new DateTime(2003, 12, 5),
            Gender.Male, CivilStatus.Single, "Purok Lapu-Lapu",
            "34 Luna Street", "08951234567", "Unemployed",
            DateTime.Today.AddMonths(-2), false, ResidentClassification.None));

        // ---- the requests ------------------------------------------------
        // Each request that moves past Pending ends with SaveRequest, which
        // is what writes the new status to MySQL. For the in-memory store
        // that call does nothing, and that is fine.

        // A clearance I took all the way through to release.
        var r1 = repository.CreateRequest(juan, DocumentType.BarangayClearance,
                                          "Employment Requirement");
        r1.StartProcessing();
        r1.MarkReadyForRelease();
        r1.RecordPayment("OR-2026-00101");
        r1.Release();
        repository.SaveRequest(r1);

        // A senior citizen, waived, and released with no payment at all. I
        // include this row because it proves a free document does not get
        // stuck waiting for a payment that is never going to come.
        var r2 = repository.CreateRequest(maria, DocumentType.CertificateOfResidency,
                                          "Pension Claim");
        r2.StartProcessing();
        r2.MarkReadyForRelease();
        r2.Release();
        repository.SaveRequest(r2);

        // Free under RA 11261, and I left it still being processed.
        var r3 = repository.CreateRequest(jose, DocumentType.FirstTimeJobseekerCertificate,
                                          "NBI Clearance Application");
        r3.StartProcessing();
        repository.SaveRequest(r3);

        // Free, and I left it sitting ready to collect.
        var r4 = repository.CreateRequest(pedro, DocumentType.CertificateOfIndigency,
                                          "Medical Assistance at Davao Regional Medical Center");
        r4.StartProcessing();
        r4.MarkReadyForRelease();
        repository.SaveRequest(r4);

        // ₱500 - the business clearance whose amount VARIES with the law
        // violated, which is the whole reason the v3.1 fee schedule takes
        // an assessed amount. I charge the full ₱500 despite her solo-parent
        // tag, for the reason I gave above.
        repository.CreateRequest(ana, DocumentType.BarangayBusinessClearance,
            "Sari-sari Store Renewal",
            new RequestInput(Amount: 500m,
                Detail: "Barangay Ordinance No. 12-2024, operating beyond the approved business line"));

        // A PWD, so I waive it under RA 10754.
        repository.CreateRequest(liza, DocumentType.CertificateOfGoodMoralCharacter,
            "Scholarship Application");

        // ₱200, because this one is for work abroad - the charter's higher
        // rate, and the reason I needed the scope field at all.
        repository.CreateRequest(carlo, DocumentType.BarangayClearance,
            "Overseas Employment Requirement",
            new RequestInput(Scope: ClearanceScope.Abroad));

        // ---- the v3.1 variable-fee documents ------------------------------

        // The cedula: ₱5 basic + ₱150 additional on ₱150,000 of sworn gross
        // income = ₱155. I paid it and released it, so the printed certificate
        // shows a completed computation.
        var r8 = repository.CreateRequest(juan, DocumentType.CommunityTaxCertificate,
            "Annual community tax, CY " + DateTime.Now.Year,
            new RequestInput(GrossAnnualIncome: 150_000m));
        r8.StartProcessing();
        r8.MarkReadyForRelease();
        r8.RecordPayment("OR-2026-00102");
        r8.Release();
        repository.SaveRequest(r8);

        // A Katarungang Pambarangay filing at the flat ₱150.
        repository.CreateRequest(pedro, DocumentType.LuponCaseFiling,
            "Boundary dispute with the adjacent lot owner");

        // Barangay covered court at ₱200 per hour; two and a half hours is
        // billed as three, so the fee is ₱600.
        repository.CreateRequest(ana, DocumentType.BarangayFacilityRental,
            "Barangay covered court - birthday program",
            new RequestInput(Hours: 2.5m, Detail: "Barangay covered court"));

        // An "other processing fee" the Barangay Taripa prices, assessed by
        // the clerk at ₱50 for certified copies.
        repository.CreateRequest(maria, DocumentType.OtherTarifaProcessingFee,
            "Certified copies of a barangay resolution",
            new RequestInput(Amount: 50m, Detail: "Certified true copies - 10 pages at ₱5.00"));

        // A first-time jobseeker claiming RA 11261 on the CLEARANCE itself -
        // the law covers both documents - left pending so the demo can walk
        // it through.
        repository.CreateRequest(jose, DocumentType.BarangayClearance,
            "First local employment application",
            new RequestInput(Scope: ClearanceScope.Local, ApplyJobseekerWaiver: true));

        // An ordinary certification left ageing in the queue, so the RA 11032
        // highlight in the request list has something to point at on demo
        // day. This is the one place I file with a date in the past, which
        // is why File exists alongside CreateRequest.
        repository.File(liza, DocumentType.OtherCertification,
            "Certification for a school requirement",
            RequestInput.Default, DateTime.Today.AddDays(-6));
    }
}
