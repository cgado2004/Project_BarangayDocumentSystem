# Barangay Resident and Document Request Management System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**

A Windows Forms desktop application for barangay staff: maintain the resident
registry, accept document requests, assess fees against the correct statutory
exemptions, track each request through its workflow, and print the finished
certificate.

---

## Group Members

| Name |
|---|
| Dagamac, Emmanuelle Philippe |
| Del Rosario, Jonathan, F |
| Gado, Clint Wood |
| Raborar, Frent Dhieniel |

> Roles and task assignments: **TBD**.

---

> **v2 — refactored.** Layered folders for SOLID, duplication
> removed for DRY, and the TabControl replaced with a sidebar shell.
> Full write-up: [`docs/04-refactor-notes.md`](docs/04-refactor-notes.md).

## Running it

This is a **.NET Framework 4.8** WinForms app (not .NET / .NET Core), so it
builds with classic MSBuild — Visual Studio on Windows, not the `dotnet` CLI.

**1. Install prerequisites** (once): Visual Studio 2022 with the **".NET
desktop development"** workload, which includes the **.NET Framework 4.8
targeting pack**.

**2. Start MySQL** (XAMPP, MySQL Server, or MariaDB — anything listening on
port 3306).

**3. Check the login** in `BarangayDocumentSystem/App.config`:

```xml
<connectionStrings>
  <add name="BarangayDb"
       connectionString="Server=localhost;Port=3306;Database=barangay_db;User ID=root;Password=;CharSet=utf8mb4;" />
</connectionStrings>
<appSettings>
  <add key="SeedSampleData" value="true" />
</appSettings>
```

The default matches a stock local install (user `root`, blank password).
Change it to match your server. After building, the same settings live in
`bin\Debug\BarangayDocumentSystem.exe.config` next to the .exe — edit that
copy to reconfigure an already-built app without recompiling. To keep a real
password out of source control, set the environment variable
`BARANGAY_DB_CONNECTION` instead — it overrides the file.

**4. Open `BarangayDocumentSystem.sln` in Visual Studio and press F5.** The
first build restores the `MySql.Data` NuGet package automatically.

**You do not create the database by hand.** On start the app creates
`barangay_db` and its tables if they are missing
(`Database/schema.sql`) and every stored procedure
(`Database/procedures.sql`), and — only when the `residents` table is empty —
loads the sample data below. Set `SeedSampleData` to `false` in App.config
for real use. If the system cannot connect, it shows a general error and exits.

---

## What it does

### Stored procedures
The app does not send SQL text. All queries are stored procedures defined in
`Database/procedures.sql` (`sp_residents_get_all`, `sp_resident_add`,
`sp_resident_update`, `sp_resident_archive`, `sp_resident_restore`,
`sp_requests_get_all`, `sp_request_add`, `sp_request_save`, `sp_user_count`,
`sp_user_find`, `sp_user_add`). They are dropped and created again each time the
app starts, so to change a query, edit that file and restart. The MySQL user in
App.config needs the CREATE ROUTINE and EXECUTE privileges (root has them).

### Sign in
Sign in with a staff account. An empty database offers first-account setup;
the database blocks further setup accounts. There is no signup shortcut.
New setup passwords need 15–128 characters. Use **Settings → Log out** to
end your session. See [UI security notes](docs/UiSecurity.md) for the remaining
authentication work.

Six destinations in the left sidebar.

### Residents
Resident controls only: **Add Resident**, **Edit** and **Transfer**, plus
search. Tracks name, birth date, gender, civil status, purok, address, contact,
occupation, voter status, date of residency, and **classifications** (senior
citizen, PWD, indigent, solo parent). Student is not a resident category: it is
confirmed on each document request. Residents are never deleted:
**Transfer** asks where they are moving to and moves them to the Archive.

### Archive
Residents who transferred out, with the destination, reason and date. Their
record and request history are kept. **Edit** corrects an archived record, and
**Restore to Residents** opens the edit form first so details can be updated, then
brings the resident back. Archived residents are left out of the dashboard counts and cannot be
picked for a new document.

### Document Requests
All document work happens here. **New Request** picks a resident, then the
document and purpose, and files the request straight into Processing. Tick
**Student** (school ID verified) to apply the student discount, 50% off by
default (`FeeSchedule.StudentDiscountRate`, a placeholder like the other fees). Then
move it through the workflow:

```
Pending → Processing → Ready for Release → Released
   └──────────┴────────────────┴──────────→ Rejected
```

Record payment against an official receipt number, and view or print the
finished document.

Every save asks for confirmation first: adding or editing a resident, filing a
request, recording a payment, and transferring a resident. **Release** first
opens a preview of the finished document, then asks you to confirm.

### Records
Released documents. When a request is released it leaves the Requests page and
is kept here as a permanent record: search it, and open **View / Print** to
reprint the document. Records cannot be edited or deleted.

### Dashboard
Resident counts, request counts by status, revenue collected, documents issued
free of charge, and breakdowns by document type and purok.

---

## Documents supported

| Document | Base fee* | Notes |
|---|---|---|
| Barangay Clearance | ₱50 | Most requested |
| Certificate of Residency | ₱50 | Proof of address |
| Certificate of Indigency | **FREE** | DILG MC 2019-177 |
| Barangay Business Clearance | ₱200 | Personal exemptions do **not** apply |
| Barangay ID | ₱100 | |
| First-Time Jobseeker Certificate | **FREE** | RA 11261 |
| Certificate of Good Moral Character | ₱50 | |

---

## Statutory exemptions built in

These are real Philippine laws, not invented rules — which is what makes this
a domain model rather than a generic CRUD app.

### RA 11261 — First Time Jobseekers Assistance Act
Barangay clearance and certification are **free** for a qualified first-time
jobseeker. The system enforces all three conditions:

1. **Six months' residency** — computed from the resident's `DateOfResidency`
2. **Once only** — a flag is set when the certificate is released
3. Filipino citizenship — assumed for registered residents

If a resident fails either testable condition, the request dialog **disables
the submit button and explains why**. The printed certificate includes the
Oath of Undertaking that receiving agencies (NBI, PSA, BIR) look for.

### Others
| Law | Effect |
|---|---|
| **RA 9994** | Senior citizen — document fees waived |
| **RA 10754** | PWD — document fees waived |
| **DILG MC 2019-177** | Certificate of indigency issued free |
| **RA 11032** | Official receipt required for every collection |

> **Business clearance is deliberately excluded** from personal exemptions —
> it is a regulatory fee on an enterprise, not a personal document. A senior
> citizen still pays it.

---

## Project layout

One WinForms project, targeting **.NET Framework 4.8**. Each folder name says
what its contents are for.

```
BarangayDocumentSystem/
├── BarangayDocumentSystem.sln          ← open this in Visual Studio
├── README.md
├── docs/
└── BarangayDocumentSystem/             .NET Framework 4.8
    ├── App.config                      DB connection string, DPI, .NET runtime version
    ├── app.manifest                    execution level, supported OS, DPI awareness
    ├── Properties/
    │   └── AssemblyInfo.cs             assembly title, version, GUID
    ├── Database/                       everything that talks to MySQL
    │   ├── schema.sql                  table definitions (also embedded in the .exe)
    │   ├── procedures.sql              every query, as a stored procedure (embedded too)
    │   ├── DatabaseSettings.cs         reads App.config / env variable
    │   ├── DatabaseInitializer.cs      creates the database + tables if missing
    │   ├── MySqlBarangayRepository.cs  calls the stored procedures: load, add, edit, archive, save
    │   │                               (+ MySqlUserRepository and PasswordHasher for sign-in)
    │   └── SampleDataSeeder.cs         demo residents for an empty database
    ├── Models/                         the data: what a resident / request IS
    │   ├── Resident.cs
    │   ├── DocumentRequest.cs          request + guarded status transitions
    │   └── Enums.cs                    DocumentType, RequestStatus, …
    ├── Interfaces/                     contracts the rest of the app codes against
    │   ├── IBarangayRepository.cs      storage contract (+ ResidentDetails, statistics)
    │   ├── IDocumentTemplate.cs        one-document contract (+ BarangayProfile)
    │   └── RepositoryException.cs      "the database failed"
    ├── BusinessRules/                  the barangay's rules, no screens, no SQL
    │   ├── FeeSchedule.cs              ALL fee rules and exemptions
    │   ├── DocumentRenderer.cs         page layout, written once
    │   └── DocumentTemplates/          one class per certificate (7)
    ├── Views/                          the main pages (sidebar destinations)
    │   ├── ViewBase.cs / DashboardView.cs / ResidentsView.cs (+ ArchiveView) / RequestsView.cs
    ├── Forms/                          pop-up windows
    │   ├── ResidentForm / RequestForm / PaymentForm / DocumentPreviewForm / Prompt (+ LoginForm, TransferForm, ResidentPickerForm)
    ├── CustomControls/
    │   └── NavigationSidebar.cs        the left navigation rail
    ├── UIHelpers/                      shared look-and-feel code
    │   ├── AppTheme.cs                 every colour, font, spacing value
    │   ├── Dialog.cs                   all message boxes
    │   ├── InputValidator.cs           reusable field validation
    │   ├── UiFactory.cs                themed control construction
    │   ├── CueBanner.cs                the grey placeholder text in empty text boxes
    │   └── CompilerShims.cs            lets `record` types compile on .NET Framework
    ├── Program.cs                      start-up: connects DB, wires everything
    ├── MainShell.cs                    main window: sidebar + content + status bar
    └── BarangayDocumentSystem.csproj   classic (non-SDK) project file
```

Namespaces follow the folders (`BarangayDocumentSystem.Models`,
`.Database`, `.Views`, …).

**Why PackageReference instead of `packages.config`.** The MySQL driver pulls
in several of its own dependencies. `packages.config` requires every one of
those to be listed and referenced by hand, with exact versions — brittle, and
easy to get subtly wrong without a real NuGet client to generate it.
`PackageReference` (supported in classic .NET Framework projects since Visual
Studio 2017, and what current VS templates use by default) resolves all of
that automatically on restore, so the `.csproj` only ever names `MySql.Data`
itself.

**What .NET 6+ WinForms gives you for free that .NET Framework doesn't:**
those are the pieces this project had to add back by hand — `CueBanner.cs`
(there is no `TextBox.PlaceholderText`), `CompilerShims.cs` (the `record`
types need a marker type .NET Framework doesn't ship), and explicit `using
System.Windows.Forms;` / `using System.Drawing;` in every file that needs
them (.NET Framework has no implicit/global usings).

**How saving works.** The app loads everything from MySQL at start and writes
each change to MySQL *first*; only when the database accepts it does the
screen update. Workflow actions (Mark Ready, Release, Record Payment,
Reject) change the request in memory, so the view then calls
`Repository.SaveRequest(request)` to store it. If a save fails, the user gets
a message and the data is reloaded from MySQL so the screen never shows
something the database does not have. It assumes one running copy of the app.

> **Trade-off of the single project:** fee arithmetic and status rules live in
> `BusinessRules/` and `Models/`, not in the forms, but the compiler no longer
> *forces* that. `internal` now means "the whole app", so keep to the
> convention by hand: forms call `IBarangayRepository`, never
> `new DocumentRequest(...)`.

---

## Sample data

Seven residents across Puroks 1–5, chosen to exercise every rule:

| Resident | Why they're there |
|---|---|
| Juan Dela Cruz | Ordinary resident — pays full fees |
| Maria Reyes | **Senior citizen** — fees waived |
| Jose Bautista Jr. | 14 months' residency — **qualifies** under RA 11261 |
| Ana Villanueva | Solo parent, business owner |
| Pedro Mendoza | **Indigent** — fees waived |
| Liza Torres | **PWD** — fees waived |
| Carlo Aquino | 2 months' residency — **fails** the RA 11261 six-month test |

Try filing a First-Time Jobseeker Certificate for **Carlo** — the dialog blocks
it and explains why. Then try **Jose** — it goes through, free of charge.

---

## Expected behaviour

> **These are design intentions, not test results.** The project has **not been
> compiled or run** — the environment it was written in could not install the
> .NET SDK — and there are **no automated tests**. The fee and eligibility
> logic was checked by porting it to a scratch script and running the cases
> below; the rules were then written into C# to match. Everything here should
> be confirmed by actually running the app.

| Scenario | Expected outcome | Enforced by |
|---|---|---|
| Senior requests clearance | ₱0, cites RA 9994 | `FeeSchedule.Assess` |
| PWD requests clearance | ₱0, cites RA 10754 | `FeeSchedule.Assess` |
| Indigent requests clearance | ₱0 | `FeeSchedule.Assess` |
| Ordinary resident, clearance | ₱50 | `FeeSchedule.Assess` |
| Senior requests **business** clearance | **₱200** — not waived | `FeeSchedule.Assess` (checked before personal exemptions) |
| Jose (14 mo) → jobseeker cert | Allowed, free | `CanIssueJobseekerCertificate` |
| Carlo (2 mo) → jobseeker cert | **Blocked** — under 6 months | `CanIssueJobseekerCertificate` |
| Already availed → jobseeker cert | **Blocked** — once only | `HasAvailedFirstTimeJobseeker` |
| Release unpaid fee-bearing doc | **Blocked** until paid | `DocumentRequest.Release` |
| Reject a released document | **Blocked** | `DocumentRequest.Reject` |

Each row names the method that enforces it, so any claim can be checked
against the source.

---

## Notable techniques

- **`[Flags]` enum** for classifications — a resident can be senior *and*
  indigent; combined with bitwise `|=`
- **Guarded state transitions** in `DocumentRequest` — illegal moves throw,
  and the UI catches and reports instead of crashing
- **Live fee assessment** — the fee and its legal basis update as the document
  type changes, before anything is committed
- **`TryParse`, never `Parse`**, and `KeyPress` filtering on numeric fields
- **`PrintDocument`** for real printing — framework only, no NuGet package
- **`Prompt.cs`** built entirely in code — proof the designer is a
  convenience, not a requirement

Added in the v2 refactor:

- **Two interfaces** (`IBarangayRepository`, `IDocumentTemplate`) — the UI
  names a concrete storage class in exactly one place, `Program.cs`
- **One template class per document** — adding an eighth certificate means
  adding a file and one line, never editing the renderer
- **`Dialog` / `InputValidator` / `UiFactory`** — 18 raw `MessageBox.Show`
  calls reduced to 0; validation reads as a chain of conditions
- **`AppTheme`** — every colour, font and spacing value in one file
- **System fonts only** (Segoe UI, Consolas). A font that is not installed
  does not error; Windows substitutes different metrics and the layout
  silently breaks on the grader's machine

---

## Not included

Honest scope notes:

- **Single-user database access.** The data is stored in MySQL, but the app
  caches it in memory, so two copies running at once will not see each
  other's changes until restarted. The connection password sits in plain text
  in `appsettings.json` (or use the environment variable).
- **No login or user roles.** A real deployment needs at least clerk vs.
  captain separation.
- **No photo or biometric capture** for barangay IDs.
- **Punong Barangay name is a placeholder** — set it in
  `BarangayProfile.MagugpoPoblacion` (`Interfaces/IDocumentTemplate.cs`).
- **No blotter/case module** — a real clearance checks for pending cases;
  here that is asserted, not verified.
- **No unit tests.** The refactor makes them possible — `IBarangayRepository`
  can now be faked — but none are written yet.
