# Barangay Document System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**

Windows Forms · **C# on .NET Framework 4.8** · Visual Studio 2022 · MySQL or
SQL Server · Crystal Reports when it is installed

A counter system for a barangay hall: residents and their households, the
documents they ask for, the fees the barangay charges for them, the official
receipts that money is recorded against, the census that comes out of all of
it, and a log of who did what.

This is the **re-designed build**. The list of what was asked for, and where in
the code each item lives, is in
**[docs/10-revamp-notes.md](docs/10-revamp-notes.md)** — read that first.

## Group

| Member | Working branch | What of theirs is in this build |
|---|---|---|
| Clint Wood Gado | `leader_draft` | The system design and the rewrite: models, services, data layer, database scripts, security, reports, tests, docs |
| Del Rosario, Jonathan F. | `Draft` | The screen look that the new interface keeps: flat sidebar, page headings, summary cards, status strip |
| Raborar, Frent Dhieniel | `Fdraft` | The persistence idea (repository + initializer + settings) and the MySQL access, carried into `Data/` and `Database/` |
| Dagamac, Emmanuelle Philippe | `draft3` | — (a stale .NET 8 layout; not integrated) |

## What the program does

* **Sign in.** Two accounts are created from `App.config` on a brand-new
  database (`admin` and `clerk`). There is no self-registration — an
  administrator creates staff accounts from the Users screen. Passwords are
  PBKDF2-hashed with a per-account salt, accounts lock after repeated failures,
  and an idle session locks itself.
* **Residents.** Name (in a Group Box), birthday, sex, civil status, contact,
  occupation, purok, date of residency, residency status (Newcomer / Temporary
  / Permanent), classifications (senior citizen, PWD, indigent, solo parent,
  4Ps), the student **fee category**, head of the family with their dependents,
  and a record state of **Active / Inactive / Archived**. There is no address
  field and there are no Delete buttons anywhere in the system.
* **Requests.** *New request* (not "New Document"), a confirmation that shows
  the fee and the status the request will start in, the office-window rule
  (filed 8:00 AM–4:00 PM with nothing to check → **Cleared**; filed after
  4:00 PM → **Pending** until the next window; business clearances →
  **Processing** for inspection), a history of every status change, payment at
  the counter, and release only against an official receipt.
* **Money.** Fees from `App.config` (clearance ₱100 local / ₱200 abroad,
  certification ₱100, business ₱200, lupon ₱150, facility ₱200/hour, cedula
  computed the way the law says), a student discount, the statutory exemptions,
  and the first-time jobseeker waiver. The **legal basis is recorded but not
  printed at the counter**.
* **Official receipts.** Booklets with a series code and a control-number
  range; the three numbers on a receipt must agree. Receipts can be voided with
  a reason, and a voided receipt stays in the register but stops counting as
  income.
* **Census and reports.** Population, households, dependents, age brackets,
  purok breakdown, classifications; eleven reports, each printable on plain
  paper, exportable to CSV, and printable through Crystal Reports when it is
  installed (see [docs/08-crystal-reports.md](docs/08-crystal-reports.md)).
* **Activity log.** Every sign-in, change, collection and void, appended with
  who did it and when. Read-only: no edit, no delete.

## Build and run on Windows

1. Install **Visual Studio 2022** with the **.NET desktop development**
   workload and the **.NET Framework 4.8 targeting pack**.
2. Open **`BarangayDocumentSystem.sln`**.
3. Set **BarangayDocumentSystem** as the startup project and press **F5**.

The one NuGet package, **MySql.Data 8.4.0**, is a `PackageReference` — Visual
Studio restores it on the first build. From a Developer Command Prompt:

```
msbuild /t:Restore BarangayDocumentSystem.sln
msbuild BarangayDocumentSystem.sln /p:Configuration=Debug
```

Before F5, start **MySQL** (XAMPP is fine). On the first run the program
creates the database `barangay_document_system`, runs the schema and the
stored procedures from scripts embedded in the `.exe`, creates the two
accounts from `App.config` and (if `SeedSampleData` is true) loads sample
residents so no screen is blank.

**To run without a database at all**, set `Storage` to `Memory` in
`App.config` (the built copy is `BarangayDocumentSystem.exe.config`). Nothing
is saved — useful for a demonstration.

To use **SQL Server** instead, set `Storage` to `SqlServer`; the second
connection string in `App.config` is used and the SQL Server scripts are
installed the same way.

### The rule checks

`tests/RuleChecks` is a small console program that runs the real classes
against the in-memory store and prints PASS or FAIL for each rule — the fees,
the office window, the workflow, the passwords, the receipts, the census and
the reports:

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

It verifies that the project file lists exactly the files that exist, that the
four database scripts are embedded, that both engines' stored procedures have
the same names and parameter order, that no password is committed, that the
seal is byte-identical to the original, and that the removed things (Address,
Student-as-a-classification, the old v3.2 folders) really are gone.

## Settings

Everything the barangay may want to change is in **`App.config`**, each key
with a sentence explaining it: the barangay's name and seal, the fees, the
student discount, the community tax, the office window, the residency tests,
the RA 11032 working days, the sign-in rules, and the report folder. One class
reads that file (`Config/AppConfig.cs`); nothing else touches it.

The connection string can also come from the environment variable
`BARANGAY_DB_CONNECTION` (or `BARANGAY_DB_CONNECTION_SQLSERVER`), and can be
sealed with Windows DPAPI by setting `Security.ProtectConnectionString` to
true — see `Security/ConnectionStringProtector.cs` for what that does and what
it honestly does not do.

## Where things live

```
BarangayDocumentSystem/
  Config/        AppConfig (the only reader of App.config), AppLog
  Models/        Resident, Dependent, DocumentRequest, BusinessDetails,
                 OfficialReceipt, UserAccount, ActivityLogEntry, enums
  Interfaces/    Repository contracts, provider contract, report contract
  Data/          DBHelper, DBContext, SQL and in-memory repositories
  Database/      DatabaseInitializer, SampleData, Scripts/{MySql,SqlServer}
  Security/      PasswordHasher, Permissions, SessionManager,
                 ConnectionStringProtector
  Services/      The rules: fees, office window, residents, requests, receipts,
                 users, activity log, census
    Documents/   The printed/rendered documents, one class per document type
    Reports/     Report definitions, the report service, Crystal gateway
  UI/            AppTheme, UiFactory, Dialog, controls, dialogs, forms, views
  Assets/        barangay-logo.png  (drawn, never edited)
tests/RuleChecks/  the rule checks
scripts/           check_structure.py
docs/              the documentation (start with 10-revamp-notes.md)
```

## Documentation

| File | What it covers |
|---|---|
| [docs/10-revamp-notes.md](docs/10-revamp-notes.md) | **Start here.** Every request and where it is implemented |
| [docs/01-requirements.md](docs/01-requirements.md) | The requirements (FR/NFR) |
| [docs/05-database-guide.md](docs/05-database-guide.md) | Setting up MySQL or SQL Server by hand |
| [docs/08-crystal-reports.md](docs/08-crystal-reports.md) | Crystal Reports: how it is loaded, and the columns each layout gets |
| [docs/07-fee-schedule-and-legal-basis.md](docs/07-fee-schedule-and-legal-basis.md) | The fees and the laws behind them |
| [docs/06-pushing-to-github.md](docs/06-pushing-to-github.md) | How the group pushes to GitHub |

## The seal

`Assets/barangay-logo.png` is the barangay's own seal. It is drawn on the
sign-in card, the sidebar and every printed document, and it is **not to be
edited, recoloured or replaced** — the structure check compares its SHA-256
against the original file for exactly that reason.
