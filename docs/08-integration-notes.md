# Branch integration notes — v3.2

*Written by Clint Wood Gado.* How the three drafts became one program, what
came from whom, and what each principle cost or saved. The object model the
code was written from is `docs/09-object-model.md`.

## 1. Precedence

| Concern | Taken from | Why |
|---|---|---|
| Project structure, runtime | `Fdraft` (Frent) | Classic WinForms, .NET Framework 4.8, one project, the folder layout everybody already knew |
| Persistence | `Fdraft` (Frent) | The only branch whose database code actually ran: `MySqlBarangayRepository`, `DatabaseInitializer`, `DatabaseSettings`, embedded `schema.sql` |
| Screen design | `Draft` (Jonathan) | Flat sidebar with the seal, page title over the content, six summary cards, three breakdown tables, status strip |
| Rules and regulations | `leader_draft` (Clint) | The Citizen's Charter rates and the statutes behind every waiver, `docs/07`; the classroom ₱50 placeholders in the other branches were never the barangay's fees |
| Palette, logo, typography, documents, models, tests | `leader_draft` (Clint) | `AppTheme`, `Assets/barangay-logo.png`, `DocumentTemplates`, `RequestInput`, RuleChecks |
| When MySQL is unreachable | `Fdraft` (Frent) | Explain and exit. No silent fallback to demo data |

`Fdraft` itself was not modified; it stays as Frent left it. This work was
done on a working branch off `leader_draft` and merged back into
`leader_draft`.

## 2. The order I worked in

1. **Pseudocode** (`docs/10-pseudocode.md`, committed in step 1 and deleted
   in the last step — the history keeps it). Decisions and procedures
   before any code.
2. **Object model** (`docs/09-object-model.md`). Class diagrams, the ERD
   the program actually creates, the file-to-release sequence, the
   SOLID/DRY map.
3. **OOP** — persistence first, then the screens.
4. **Attribution headers** on every file, all comments in my voice.
5. **Docs**, this file included.

## 3. What changed, by principle

**DRY — the duplication I removed**

| Was in two places | Now in one |
|---|---|
| Working set, `Apply`, `SearchResidents`, `GetStatistics` (my in-memory store *and* Frent's MySQL store) | `Database/RepositoryBase.cs` |
| The seven sample residents (my seed *and* Frent's `SampleDataSeeder`, with different names and fees) | `Database/SampleData.cs`, filling either store through the repository |
| The schema (my `01-schema.sql` + `02-seed-data.sql` *and* Frent's embedded `schema.sql`) | `Database/schema.sql`, embedded, applied on start-up |
| Reading `App.config` (my `AppSettings` *and* `ConfigurationManager` in Frent's `DatabaseSettings`) | `AppSettings.cs`; `DatabaseSettings` reads through it |
| Grid styling (`ResidentsView.StyleGrid`, reached into by two sibling views) | `UiFactory.StyleGrid` |
| Dashboard card = two designer labels × six | `CustomControls/SummaryCard.cs` × six |
| "Save failed" handling (would have been three views × three ways) | `ViewBase.Persist` |

**SOLID — where it is, in one line each** (the full table is in
`docs/09-object-model.md` §7)

- *S*: `FeeSchedule` prices; `DocumentRequest` guards state; repositories
  store; `DocumentRenderer` lays out; `AppSettings` reads config.
- *O*: a new storage is one `RepositoryBase` subclass and one line in
  `Program.cs`; a new document is one `IDocumentTemplate` class.
- *L*: `InMemoryBarangayRepository` and `MySqlBarangayRepository` are
  interchangeable behind `IBarangayRepository`; the RuleChecks harness
  runs the same rules on the in-memory one.
- *I*: screens depend on `IBarangayRepository`, never on a `MySqlConnection`.
- *D*: every view and form receives the repository and fee schedule through
  its constructor; `Program.cs` is the only file that names a concrete
  store.

## 4. Bugs fixed along the way

- **Record payment never recorded the payment** (flagged in PR #3): the
  dialog validated the receipt number and the view saved the request
  without ever calling `request.RecordPayment`. `RequestsView.RecordPayment`
  now goes through `Step(x => x.RecordPayment(receipt))`.
- **The v3.1 in-memory seed did not compile**: it passed a
  `ClearanceScope` where a `RequestInput` was expected. `SampleData` uses
  `new RequestInput(Scope: ...)`.
- **Frent's schema could not hold my request model**: no scope, no
  assessed amount, no hours, no income, no RA 11261 flags. Seven columns
  added; older databases upgraded in place by `EnsureColumns`.

## 5. Known limits, stated plainly

- One running copy of the program per database. Two clerks on two machines
  will not see each other's changes until one reloads (Frent's original
  limit, unchanged).
- `docs/02-erd.svg` and `docs/03-uml.svg` still show the v3.1 four-table
  design. The current diagrams are the Mermaid ones in
  `docs/09-object-model.md`; the SVGs should be regenerated from them or
  removed.
- The classification junction table from v3.1 is recorded in `docs/05` §6
  as the upgrade to make when an indexed "every senior citizen" query is
  needed.

## 6. Verification

`python scripts/check_structure.py` (Framework target, every `.cs` in the
project file, folders, XML, assets, incompatible APIs) and `git diff --check`
pass. Brace and parenthesis balance was checked on every changed file, and
every removed member was grepped for. **No compiler was available in the
environment this was written in** (Linux, no MSBuild, no NuGet access), so
build the solution on Windows and run `tests\RuleChecks\bin\Debug\RuleChecks.exe`
before trusting any of it; the README has the commands. The first Windows
run should also confirm: first start creates `barangay_db` and seeds it, a
second start does not re-seed, stopping MySQL mid-session produces the
"was not saved" message and a reload, and `Storage=Memory` still works.
