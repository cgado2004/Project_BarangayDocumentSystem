# Barangay Resident and Document Request Management System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**
Windows Forms · **.NET Framework 4.8** · Visual Studio 2022

## Group

| Member | Working branch | What of theirs is in this build |
|---|---|---|
| Clint Wood Gado | `leader_draft` | Fee rules and legal basis, palette and logo, models, document templates, the shared repository skeleton, tests, docs |
| Del Rosario, Jonathan F. | `Draft` | The screen design: flat sidebar, page header, six summary cards, breakdown tables, status strip |
| Raborar, Frent Dhieniel | `Fdraft` | The project structure and the MySQL persistence (repository, initializer, settings, schema) |
| Dagamac, Emmanuelle Philippe | `draft3` | — (stale .NET 8 layout; not integrated) |

Every source file opens with a `PART / ORIGIN / EDITS / VOICE` header that
says which branch and teammate the part came from and what was changed, so
each part can be attributed. All comments are written in the first person by
Clint.

## Build and run on Windows

1. Install Visual Studio 2022 with **.NET desktop development** and the
   **.NET Framework 4.8 targeting pack/developer pack**.
2. Open **`BarangayDocumentSystem.sln`**.
3. Set **BarangayDocumentSystem** as the startup project and press **F5**.

This is a classic Framework WinForms project, not .NET 8 or a web app.
It uses the C# 10 compiler supplied by Visual Studio 2022, but targets the
Framework 4.8 runtime. `CompilerShims.cs` supplies the `init` marker used by
immutable records. The one NuGet package, **MySql.Data 8.4.0**, is a
`PackageReference`; Visual Studio restores it on the first build (a
Developer Command Prompt needs `msbuild /t:Restore` first, as below).

Before F5: start **MySQL** in the XAMPP Control Panel. The program creates
the `barangay_db` database and its tables by itself on first run and loads
sample data into it. Nothing has to be pasted into phpMyAdmin. If MySQL is
not running the program says so and closes; see
[the database guide](docs/05-database-guide.md).

From a Visual Studio Developer Command Prompt:

```bat
msbuild BarangayDocumentSystem.sln /t:Restore
msbuild BarangayDocumentSystem.sln /t:Rebuild /p:Configuration=Debug
msbuild tests\RuleChecks\RuleChecks.csproj /t:Rebuild /p:Configuration=Debug
tests\RuleChecks\bin\Debug\RuleChecks.exe
```

The RuleChecks harness uses the in-memory store, so it needs no MySQL.

## Integration baseline (v3.2)

- **Fdraft (Frent):** the baseline. One application project with
  `BusinessRules`, `CustomControls`, `Database`, `Forms`, `Interfaces`,
  `Models`, `UIHelpers` and `Views`; shell and entry point at the root; the
  working MySQL persistence, ported as it was and extended for the richer
  request model. `Fdraft` itself was not modified.
- **Draft (Jonathan):** the screen design — flat sidebar with the seal,
  page title and subtitle over the content, six summary cards, tabular
  status / document / purok breakdowns, status strip.
- **leader_draft (Clint):** the rules and regulations
  ([`docs/07`](docs/07-fee-schedule-and-legal-basis.md), unchanged), the
  palette and logo, the models, document templates and tests.

The order of work was pseudocode → object model
([`docs/09`](docs/09-object-model.md)) → code. The
[integration notes](docs/08-integration-notes.md) list what came from whom,
the duplication that was removed, and the bugs fixed on the way.

## Layout and OOP

```text
BarangayDocumentSystem.sln
BarangayDocumentSystem/
  Program.cs                 composition root and configuration reader
  MainShell.cs               navigation and shared page headings
  Assets/                    original barangay seal
  BusinessRules/             fee schedule, formatting, document rendering
    DocumentTemplates/       IDocumentTemplate implementations
  CustomControls/            navigation sidebar, dashboard summary card
  AppSettings.cs             the one reader of App.config
  Database/                  RepositoryBase, MySQL and in-memory stores, initializer, schema.sql, sample data
  Forms/                     dialogs and their Designer files
  Interfaces/                repository and template contracts
  Models/                    residents, requests, enums, barangay profile
  UIHelpers/                 palette, validation, reusable controls, DPI support
  Views/                     dashboard, residents, requests, shared base view
  App.config                 profile, fee and storage settings
  app.manifest               Windows compatibility and visual styles
 docs/                       guides, fee/legal basis, object model (09), integration notes (08)
 tests/RuleChecks/           Framework 4.8 regression-check console application
 scripts/check_structure.py  cross-platform structural checks
```

Forms and views receive the repository and fee schedule through their
constructors; `Program.cs` is the only file that names a concrete store.
Request state transitions and payment guards live in the model, fee
decisions in `FeeSchedule`, document wording behind `IDocumentTemplate`, and
everything both storages share — queries, statistics, the price-on-filing
rule, the sample data — in `RepositoryBase`, so the MySQL and in-memory
repositories are each only the part that differs. The SOLID/DRY map is in
[`docs/09-object-model.md`](docs/09-object-model.md) §7.

## Storage and fees

**Storage is MySQL by default** (`Storage=MySQL` in `App.config`, connection
string `BarangayDb`, XAMPP defaults: `root`, no password, database
`barangay_db`). The program creates the database and tables on first run and
upgrades an older table in place. The status bar always says where the data
is. `Storage=Memory` keeps the sample-data demo for a machine without MySQL;
it is labelled *nothing is saved*.

Never commit a real password in `App.config` — the repository is public. The
environment variable `BARANGAY_DB_CONNECTION` overrides the file.

Fees and waivers are the leader branch's documented rules
([`docs/07`](docs/07-fee-schedule-and-legal-basis.md)): ₱100 / ₱200
clearance, ₱100 certification, indigency and low-income free, RA 9994 /
10754 / 11291 exemptions, RA 11261 once-only with six months' residency, RA
7160 §156 cedula, ₱150 lupon filing, ₱200/hour facilities, Taripa items. The
numbers live in `App.config`; the law behind each lives in `FeeSchedule`.

## Verification status

- `python scripts/check_structure.py`: checks Framework target, source inclusion,
  duplicate project entries, layout, XML, assets and known incompatible APIs.
- `git diff --check`: whitespace validation.
- `tests/RuleChecks`: fees, residency restrictions, payment/release guards,
  document-template coverage and accented-name search, against the
  in-memory store.
- **Build, regression execution, designer, printing, the first MySQL run and
  UI behavior have not been verified here.** The editing environment is Linux
  without .NET/MSBuild and without NuGet access. Run the commands above on
  Windows; `docs/08` §6 lists what the first run should confirm.

Before submission, check navigation by mouse and keyboard, add/edit/search,
request creation and status changes, paid/free releases, print preview, actual
printing, and layout at 100%, 125%, 150% DPI and across monitors. Framework DPI
opt-in is in `App.config`, not the .NET 8 startup API.
