# Software Requirements and Project Timeline

**Barangay Resident and Document Request Management System**
Barangay Magugpo Poblacion, City of Tagum, Davao del Norte

**Members**
Dagamac, Emmanuelle Philippe · Del Rosario, Jonathan F. · Gado, Clint Wood ·
Raborar, Frent Dhieniel

> Sections I–III follow `Docu v7.pdf` (27 September 2026), and the ERD and
> UML diagrams (`02-erd.svg`, `03-uml.svg`) are redrawn from its pages V and
> VI. Section IV, the Fee Schedule, is **not** part of the PDF: it is kept
> as the barangay's posted Citizen's Charter rates, to be merged separately.

---

## I. Scope and Limitations

### A. Scope

The system is a Windows desktop application, built on .NET Framework 4.8
with a MySQL database, for the staff of Barangay Magugpo Poblacion, City of
Tagum, Davao del Norte. It covers the following:

- Maintenance of a resident registry, including registration, searching,
  editing, and deletion of resident records, persisted in a MySQL database.
- Recording of resident classifications — senior citizen, person with
  disability, indigent, student, and solo parent — which determine fee
  exemptions.
- Filing and processing of requests for **seven document types**: Barangay
  Clearance, Certificate of Residency, Certificate of Indigency, Barangay
  Business Clearance, Barangay ID, First-Time Jobseeker Certificate, and
  Certificate of Good Moral Character.
- Automatic assessment of document fees, together with the legal basis for
  each amount charged or waived, applying the exemptions provided under
  RA 9994, RA 10754, RA 11261, and DILG MC 2019-177.
- Tracking of each request through the workflow Pending → Processing →
  Ready for Release → Released, or Rejected, with payment recorded against
  an official receipt number before release.
- Generation, preview, and printing of the finished document using the
  barangay letterhead and the wording proper to each document type, via the
  .NET Framework `PrintDocument` class.
- A dashboard summarising resident counts, request counts by status,
  revenue collected, documents issued free of charge, and breakdowns by
  document type and by purok.
- Automatic creation of the database and its tables on first launch, with
  optional seeding of sample residents for demonstration.

### B. Limitations

The following are outside the coverage of this version of the system:

- The application supports a single running copy at a time. Data is stored
  in MySQL but cached in memory, so two copies running at once will not see
  each other's changes until restarted.
- The system has no login, user accounts, or user roles. It does not
  distinguish between a clerk and the punong barangay. (X)
- The database credential is read from `App.config` in plain text unless
  the `BARANGAY_DB_CONNECTION` environment variable is set instead.
- The fee amounts used are placeholder values based on typical Philippine
  rates. They must be replaced with the rates fixed by the barangay revenue
  ordinance before the system is used to collect actual payments. (X)
- The system does not capture photographs or biometric data for Barangay
  IDs.
- The punong barangay's name printed on documents is a placeholder that
  must be set in `BarangayProfile` before real use.
- The system has no blotter or case module. A clearance asserts that the
  resident has no pending case rather than verifying it against records.
- Filipino citizenship under RA 11261 is assumed for every registered
  resident and is not separately verified.
- The project has not yet been compiled or run in its development
  environment, and it has no automated tests.
- The system runs only on Windows, and requires the .NET Framework 4.8
  runtime and a MySQL-compatible server reachable on port 3306.

---

## II. Functional Requirements

Functional requirements describe what the system must do — the services it
provides to barangay staff and the rules it enforces.

| ID | Requirement |
|---|---|
| FR-01 | The system shall allow staff to register a new resident with name, date of birth, gender, civil status, purok (X), address (X), contact number, occupation, voter status, and date of residency. |
| FR-02 | The system shall allow staff to view, search, edit, and delete existing (X) resident records. |
| FR-03 | The system shall allow a resident to be tagged with one or more classifications (senior citizen, PWD, indigent, student, solo parent). |
| FR-04 | The system shall allow staff to file a document request for a registered resident, selecting the document type and stating the purpose. |
| FR-05 | The system shall support seven document types: Barangay Clearance, Certificate of Residency, Certificate of Indigency, Barangay Business Clearance, Barangay ID, First-Time Jobseeker Certificate, and Certificate of Good Moral Character. |
| FR-06 | The system shall automatically compute the fee for each request and display the legal basis for the amount charged or waived. |
| (N)FR-07 (X) | The system shall waive document fees for senior citizens (RA 9994), persons with disability (RA 10754), and indigent residents, and shall issue the Certificate of Indigency free of charge (DILG MC 2019-177). |
| FR-08 (On going) | The system shall exclude the Barangay Business Clearance from all personal fee exemptions, as it is a regulatory fee on an enterprise. |
| FR-09 | The system shall verify First-Time Jobseeker eligibility under RA 11261 by checking at least six months of residency and that the benefit has not been availed before, and shall block and explain a request that fails either condition. |
| FR-10 (Major adjustments) | The system shall move a request through the workflow Pending → Processing → Ready for Release → Released, and shall allow rejection with a required reason from any stage before release. |
| FR-11 (X) | The system shall prevent the release of a fee-bearing document until payment is recorded against an official receipt number (RA 11032). |
| FR-12 | The system shall reject any invalid status transition and display an explanatory message instead of terminating. |
| FR-13 | The system shall generate a printable document for each request, using the barangay letterhead and the wording required for that document type, including the Oath of Undertaking for the First-Time Jobseeker Certificate. |
| FR-14 | The system shall provide a preview of the generated document before printing. |
| FR-15 | The system shall display a dashboard showing resident counts, request counts by status, total revenue collected, documents issued free of charge, and breakdowns by document type and by purok. |
| FR-16 | The system shall validate all user input, requiring mandatory fields and rejecting impossible dates, non-numeric contact numbers, and a date of residency earlier than the date of birth. |
| FR-17 | The system shall generate Crystal Report. |
---

## III. Non-Functional Requirements

Non-functional requirements describe how the system must behave — the
quality attributes and constraints it must satisfy.

| ID | Requirement |
|---|---|
| NFR-01 | Usability — The interface shall use a single sidebar navigation with three destinations (Dashboard, Residents, Requests) so that a barangay clerk can complete any task without training beyond a short walkthrough. |
| NFR-02 | Reliability — Invalid operations shall raise handled exceptions and produce a message dialog; the application shall not crash on a mis-click or on malformed input. |
| NFR-03 | Performance — Every screen action (search, save, fee assessment, status change) shall complete in under one second on a standard office workstation. |
| NFR-04 | Maintainability — All fee amounts and exemption rules shall reside in a single class (FeeSchedule) so that a new barangay revenue ordinance requires editing one file only. |
| NFR-05 | Modularity — Screens shall depend on the IBarangayRepository and IDocumentTemplate interfaces rather than on concrete classes, so that MySqlBarangayRepository is named in exactly one place, Program.cs. |
| NFR-06 | Extensibility — Adding a new document type shall require adding one template class implementing IDocumentTemplate and registering it, without modifying the renderer or any view. |
| NFR-07 | Data Persistence — Every add, edit, and workflow action shall be written to the MySQL database before the on-screen list is updated, so the interface never shows a state the database does not have. |
| NFR-08 | Compatibility — The system shall run on Windows with the .NET Framework 4.8 runtime and a MySQL-compatible server (MySQL, MariaDB, or XAMPP) listening on port 3306, and shall use only system-installed fonts (Segoe UI, Consolas) so that layout does not break on another machine. |
| NFR-09 (X) | Accuracy — Fee computation shall apply statutory exemptions in a fixed order and shall return both the amount and its legal basis for every assessment. |
| NFR-10 | Auditability — Every collection shall be recorded against an official receipt number, and every released document shall remain in the record as a historical fact. |
| NFR-11 (X) | Security — A production deployment shall support user accounts with separate clerk and punong barangay roles, and shall keep the database credential out of source control (via an environment variable) before handling live resident data. |
| NFR-12 | Data Integrity — A resident record and its document requests shall be independent, so that later edits to a resident's details do not alter documents already released. |
| NFR-13 | Startup Resilience — On launch, the system shall create the database and its tables automatically if they do not already exist, and shall report the reason and exit cleanly, rather than crash, if the database is unreachable. |

---

## IV. Fee Schedule (from the Barangay Citizen's Charter)

These are the **real posted rates**, replacing the placeholder values used in
versions 1 and 2.

| Service | Fee | Note |
|---|---|---|
| Barangay Clearance | **₱100** | local employment |
| Barangay Clearance | **₱200** | for work abroad |
| Barangay Certification (residency, good moral, other purpose) | **₱100** | |
| Certificate of Indigency | **FREE** | |
| Certificate of Low Income | **FREE** | |
| Barangay Business Clearance | **VARIES** | amount assessed per the law violated; ₱200 standard. No personal exemptions. |
| Cedula (community tax) | **VARIES** | ₱5 + ₱1 per ₱1,000 sworn gross annual income, additional capped at ₱5,000 (RA 7160 Sec. 156) |
| Katarungang Pambarangay — filing a case | **₱150** | |
| Other processing fees (Barangay Taripa) | **VARIES** | clerk assesses; item stated |
| Barangay facilities (gym) | **₱200/hr** | hour or any part of an hour |
| Assistance and social-service papers | **FREE** | medical, financial, burial, 4Ps, IP, solo parent, GAD, CSO, blotter |

Statutory waivers applied on top: **RA 9994** (senior citizens), **RA 10754**
(PWDs), indigent status (**RA 11291**), and **RA 11261** (first-time
jobseekers — once only, six months' residency, covering both the certificate
and the barangay clearance). Full legal reference:
[`07-fee-schedule-and-legal-basis.md`](07-fee-schedule-and-legal-basis.md).
