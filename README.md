# Barangay Document System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**

Windows Forms · C# on .NET Framework 4.8 · Visual Studio 2022 ·
MySQL or SQL Server · Crystal Reports when installed

---

## 1. Project layout

```
BarangayDocumentSystem.sln            open this in Visual Studio 2022
BarangayDocumentSystem/
  App.config                          every fee, rule, hour and folder
  Program.cs                          where the app starts; the only file
                                      that chooses concrete classes
  Config/        AppConfig (the only reader of App.config), AppLog
  Models/        Resident, Dependent, DocumentRequest, BusinessDetails,
                 OfficialReceipt, UserAccount, ActivityLogEntry, enums
  Interfaces/    the contracts screens depend on (never SQL types)
  Data/          DBContext, DBHelper, the in-memory repository,
                 Sql/ the one repository that talks to a real database
  Database/      DatabaseInitializer, SampleData,
                 Scripts/MySql, Scripts/SqlServer (embedded in the .exe)
  Security/      password hashing, permissions, session, config sealing
  Services/      the rules: fees, office window, residents, requests,
                 receipts, users, activity log, census
    Documents/   the printed documents, one class per document type
    Reports/     report definitions, the report service, Crystal gateway
  UI/            AppTheme, UiFactory, Dialog
    Controls/    shared controls (the name GroupBox lives here)
    Dialogs/     small prompt windows
    Forms/       sign-in, resident, request, payment, workflow windows
    Views/       dashboard, residents, requests, census, reports, admin
  Assets/        barangay-logo.png  (drawn, never edited)
tests/RuleChecks/                     a console program that proves the rules
scripts/check_structure.py            static checks, no Windows needed
docs/                                 the documentation (list in §5)
```

## 2. Build and run on Windows

1. Install **Visual Studio 2022** with the **.NET desktop development**
   workload and the **.NET Framework 4.8 targeting pack**.
2. Start **MySQL** (XAMPP is fine — you only need the MySQL module).
3. Open **`BarangayDocumentSystem.sln`**, set **BarangayDocumentSystem** as
   the startup project, press **F5**.

On the first run the program creates the database
`barangay_document_system`, the tables, and the stored procedures by
itself, creates the first two accounts from `App.config`, and loads sample
residents so no screen is blank. Nothing is pasted into phpMyAdmin.

The one NuGet package, **MySql.Data 8.4.0**, is a `PackageReference` —
Visual Studio restores it on the first build. From a Developer Command
Prompt:

```
msbuild /t:Restore BarangayDocumentSystem.sln
msbuild BarangayDocumentSystem.sln /p:Configuration=Debug
```

**Run without a database at all:** set `Storage` to `Memory` in
`App.config`. Nothing is saved — useful for a demonstration.
**Use SQL Server instead:** set `Storage` to `SqlServer`; the second
connection string in `App.config` is used.

### Sign in

The first accounts come from `App.config`: `admin` / `Barangay@2026` and
`clerk` / `Clerk@2026`, both forced to change their password at first
sign-in. **Change these before real use** — this repository is public.

### The rule checks

`tests/RuleChecks` is a small console program that runs the real classes
against the in-memory store and prints PASS or FAIL for each rule — the
fees, the office window, the workflow, the passwords, the receipts, the
census and the reports:

```
msbuild /t:Restore BarangayDocumentSystem.sln
msbuild BarangayDocumentSystem.sln /p:Configuration=Debug
tests\RuleChecks\bin\Debug\RuleChecks.exe
```

It exits with code 0 when everything passed.

### Structure checks (no Windows needed)

```
python scripts/check_structure.py
```

It verifies that the project file lists exactly the files that exist, that
the four database scripts are embedded, that both engines' stored
procedures have the same names and parameter order, that no password is
committed, and that the seal is byte-identical to the original.

---

## 3. What I built, in my own words

I built a counter system for a barangay hall: the residents and their
households, the documents they ask for, the fees the barangay charges, the
official receipts the money is recorded against, the census that comes out
of all of it, and a log of who did what.

This is the **re-designed build**. The list of every request and the file
that answers it is in **[docs/09-revamp-notes.md](docs/09-revamp-notes.md)**
— read that first if you are reviewing the work.

### The decisions I made and why

* **Sign-in before anything.** I open the sign-in window first and create
  the main window only after a user signs in. There is no self-registration
  — anyone who could create their own account could read the residents'
  records. An administrator creates staff accounts from the Users screen.
  Passwords are PBKDF2-hashed with a per-account salt, accounts lock after
  repeated failures, and an idle session locks itself.
* **Nothing is ever deleted.** A resident is Active, Inactive or Archived;
  a voided receipt stays in the register; the activity log has no edit and
  no delete. I did this because a released certificate and an official
  receipt point back at their records — deleting them would destroy the
  audit the barangay is required to keep.
* **The database sets itself up.** I embedded the schema and the stored
  procedures in the `.exe`, so the program always installs the schema that
  matches its own code and nobody can lose a `.sql` file from a USB stick.
  An older database is upgraded in place — I never drop a table that might
  hold real residents.
* **One rule, one place.** The fee schedule, the office-window policy, and
  the request workflow each live in exactly one class. A screen can mirror
  those rules to grey out a button, but it never re-implements them — so a
  rule cannot be right on one screen and wrong on another.
* **Money is never approximate.** Fees are `DECIMAL`, frozen at filing time
  together with the legal basis behind them. The basis is recorded but not
  printed at the counter, which is what the barangay asked for.
* **The office window is honest.** Filed 8:00 AM–4:00 PM with nothing to
  check → **Cleared** on the spot; after 4:00 PM → **Pending** until the
  next window; a business clearance → **Processing** for inspection. The
  next-morning clearances are written into each request's history with the
  reason, exactly like a change a staff member made.

### Where each part of my work lives

| Part | Where |
|---|---|
| The money rules (fees, discounts, exemptions, cedula) | `Services/FeeSchedule.cs`, rates in `App.config` |
| The 8 AM–4 PM rule | `Services/TimeWindowPolicy.cs`, `sp_clear_waiting_requests` |
| The request workflow (no release without payment) | `Models/DocumentRequest.cs` |
| Receipts and booklets | `Models/OfficialReceipt.cs`, `Services/ReceiptService.cs` |
| Census | `Services/CensusService.cs`, `UI/Views/CensusView.cs` |
| The eleven reports | `Services/Reports/`, stored procedures in `Database/Scripts/` |
| Database setup and upgrades | `Database/DatabaseInitializer.cs` |
| Sign-in and permissions | `Security/`, `Services/AuthenticationService.cs` |

## 4. The group

| Member | Working branch | What of theirs is in this build |
|---|---|---|
| Clint Wood Gado | `leader_draft` | The system design and the rewrite: models, services, data layer, database scripts, security, reports, tests, docs |
| TBD | `Draft` | The screen look the new interface keeps: flat sidebar, page headings, summary cards, status strip |
| TBD | `Fdraft` | The persistence idea (repository + initializer + settings) and the MySQL access, carried into `Data/` and `Database/` |
| TBD | `draft3` | — (a stale .NET 8 layout; not integrated) |

## 5. Documentation

| File | What it covers |
|---|---|
| [docs/09-revamp-notes.md](docs/09-revamp-notes.md) | **Start here.** Every request and where it is implemented |
| [docs/01-requirements.md](docs/01-requirements.md) | The requirements (FR/NFR) |
| [docs/02-project-timeline.md](docs/02-project-timeline.md) | The project timeline |
| [docs/03-database-guide.md](docs/03-database-guide.md) | Setting up MySQL or SQL Server, and debugging it |
| [docs/04-pushing-to-github.md](docs/04-pushing-to-github.md) | How the group pushes to GitHub |
| [docs/05-fee-schedule-and-legal-basis.md](docs/05-fee-schedule-and-legal-basis.md) | The fees and the laws behind them |
| [docs/06-crystal-reports.md](docs/06-crystal-reports.md) | Crystal Reports: how it is loaded, and the columns each layout gets |
| [docs/07-integration-notes.md](docs/07-integration-notes.md) | How the group's drafts became one program |
| [docs/08-object-model.md](docs/08-object-model.md) | The old v3.2 design, kept for history (marked outdated) |

## 6. The seal

`Assets/barangay-logo.png` is the barangay's own seal. It is drawn on the
sign-in card, the sidebar and every printed document, and it is **not to be
edited, recoloured or replaced** — the structure check compares its SHA-256
against the original file for exactly that reason.
