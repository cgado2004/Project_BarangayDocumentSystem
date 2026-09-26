# Barangay Resident and Document Request Management System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**
Windows Forms · **.NET Framework 4.8** · Visual Studio 2022

## Group

| Member | Working branch |
|---|---|
| Clint Wood Gado | `leader_draft` |
| Del Rosario, Jonathan F. | `Draft` |
| Raborar, Frent Dhieniel | `Draft2` |
| Dagamac, Emmanuelle Philippe | `Draft3` |

## Build and run on Windows

1. Install Visual Studio 2022 with **.NET desktop development** and the
   **.NET Framework 4.8 targeting pack/developer pack**.
2. Open **`BarangayDocumentSystem.sln`**.
3. Set **BarangayDocumentSystem** as the startup project and press **F5**.

This is a classic Framework WinForms project, not .NET 8 or a web app.
It uses the C# 10 compiler supplied by Visual Studio 2022, but targets the
Framework 4.8 runtime. `CompilerShims.cs` supplies the `init` marker used by
immutable records. No NuGet packages are needed for the current demo.

From a Visual Studio Developer Command Prompt:

```bat
msbuild BarangayDocumentSystem.sln /t:Rebuild /p:Configuration=Debug
msbuild tests\RuleChecks\RuleChecks.csproj /t:Rebuild /p:Configuration=Debug
tests\RuleChecks\bin\Debug\RuleChecks.exe
```

## Integration baseline

- **Draft2 (Frent):** single application project with `BusinessRules`,
  `CustomControls`, `Database`, `Forms`, `Interfaces`, `Models`, `UIHelpers`,
  and `Views` folders; main shell and entry point at the project root.
- **Draft:** flat sidebar, six dashboard summary cards, tabular status,
  document and purok breakdowns. Native navigation buttons support keyboard
  activation. The existing resident/request workflows remain; this is not
  a wholesale copy of Draft's screens or database implementation.
- **leader_draft:** retained logo, navy/blue/gold/red palette, document
  templates and fee rules. The fee reference remains unchanged:
  [`docs/07-fee-schedule-and-legal-basis.md`](docs/07-fee-schedule-and-legal-basis.md).

See [integration notes](docs/08-integration-notes.md) for boundaries and pending work.

## Layout and OOP

```text
BarangayDocumentSystem.sln
BarangayDocumentSystem/
  Program.cs                 composition root and configuration reader
  MainShell.cs               navigation and shared page headings
  Assets/                    original barangay seal
  BusinessRules/             fee schedule, formatting, document rendering
    DocumentTemplates/       IDocumentTemplate implementations
  CustomControls/            navigation sidebar
  Database/                  MySQL scripts and the in-memory repository
  Forms/                     dialogs and their Designer files
  Interfaces/                repository and template contracts
  Models/                    residents, requests, enums, barangay profile
  UIHelpers/                 palette, validation, reusable controls, DPI support
  Views/                     dashboard, residents, requests, shared base view
  App.config                 profile, fee and storage settings
  app.manifest               Windows compatibility and visual styles
 docs/                       existing reference documents; updated diagrams pending
 tests/RuleChecks/           Framework 4.8 regression-check console application
 scripts/check_structure.py  cross-platform structural checks
```

Forms receive the repository and fee schedule through constructors. Request
state transitions and payment guards live in the model, fee decisions in
`FeeSchedule`, and document wording behind `IDocumentTemplate`. Shared grid
styling and a single shell heading avoid per-screen duplication.

## Storage and fees

**Current storage is an in-memory demo. Changes are lost when the app closes.**
The sidebar status explicitly labels it as not saved. Leave `Storage=Memory`
in `App.config`. Selecting MySQL warns and falls back to sample data; no
persistent repository is connected in this integration baseline.

The existing `Database/` scripts, ERD and UML are reference material, not
confirmation of an approved final schema. Database integration and
model/schema reconciliation are deferred until the team's ERD, UML and
compiled documents arrive.

Fees and waivers continue to use the leader branch's documented rules. This
integration does not independently certify their legal interpretation; review
them against the documents the team supplies. The community-tax template now
uses the injected fee schedule instead of duplicating its base/rate amounts.

## Verification status

- `python scripts/check_structure.py`: checks Framework target, source inclusion,
  duplicate project entries, layout, XML, assets and known incompatible APIs.
- `git diff --check`: whitespace validation.
- Existing C# regression harness migrated for fees, residency restrictions, payment/release
  guards, document-template coverage and accented-name search.
- **Build, regression execution, designer, printing and UI behavior have not
  been verified here.** The editing environment is Linux without .NET/MSBuild;
  SDK download attempts failed. Run the commands above on Windows.

Before submission, check navigation by mouse and keyboard, add/edit/search,
request creation and status changes, paid/free releases, print preview, actual
printing, and layout at 100%, 125%, 150% DPI and across monitors. Framework DPI
opt-in is in `App.config`, not the .NET 8 startup API.
