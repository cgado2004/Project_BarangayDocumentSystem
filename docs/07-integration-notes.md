# Branch integration notes — how the three drafts became one program

> **Note:** this file was written while I was merging the group's drafts.
> The finished re-designed system is described in `docs/11-revamp-notes.md`;
> the current database scripts are in `BarangayDocumentSystem/Database/Scripts/`.

*Written by Clint Wood Gado.* How the three drafts became one program, what
came from whom, and what each principle cost or saved. The object model the
code was written from is `docs/10-object-model.md`.

## 1. Precedence

| Concern | Taken from | Why |
|---|---|---|
| Project structure, runtime | `Fdraft` (Frent) | Classic WinForms, .NET Framework 4.8, one project, the folder layout everybody already knew |
| Persistence | `Fdraft` (Frent) | The only branch whose database code actually ran: the repository, the initializer, the settings, the embedded schema |
| Screen design | `Draft` (Jonathan) | Flat sidebar with the seal, page title over the content, summary cards, breakdown tables, status strip |
| Rules and regulations | `leader_draft` (Clint) | The Citizen's Charter rates and the statutes behind every waiver, `docs/07`; the classroom placeholder fees in the other branches were never the barangay's fees |
| Palette, logo, typography, documents, models, tests | `leader_draft` (Clint) | `AppTheme`, `Assets/barangay-logo.png`, the document templates, the request model, the RuleChecks |
| When MySQL is unreachable | `Fdraft` (Frent) | Explain and exit. No silent fallback to demo data |

`Fdraft` itself was not modified; it stays as Frent left it. This work was
done on a working branch off `leader_draft` and merged back into
`leader_draft`.

## 2. The order I worked in

1. **Pseudocode** (committed in step 1 and deleted in the last step — the
   history keeps it). Decisions and procedures before any code.
2. **Object model** (`docs/10-object-model.md`). Class diagrams, the ERD
   the program actually creates, the file-to-release sequence, the
   SOLID/DRY map.
3. **OOP** — persistence first, then the screens.
4. **Attribution headers** on every file, all comments in my voice.
5. **Docs**, this file included.

## 3. What changed, by principle

**DRY — the duplication I removed**

| Was in two places | Now in one |
|---|---|
| The shared repository behaviour (my in-memory store *and* Frent's MySQL store) | `Data/RepositoryBase.cs` |
| The sample residents (my seed *and* Frent's seeder, with different names and fees) | `Database/SampleData.cs`, filling either store through the repository |
| The schema (my hand-run scripts *and* Frent's embedded script) | `Database/Scripts/`, embedded, applied on start-up |
| Reading `App.config` (my settings class *and* direct reads in Frent's settings) | `Config/AppConfig.cs` |
| Grid styling (a method on one view, reached into by two siblings) | `UI/UiFactory.cs` |
| Dashboard cards = two designer labels × six | One card control, used six times |
| "Save failed" handling (would have been three views × three ways) | `UI/Views/ViewBase.cs` |

**SOLID — where it is, in one line each** (the full table is in
`docs/10-object-model.md` §7)

- *S*: `FeeSchedule` prices; `DocumentRequest` guards state; repositories
  store; the renderer lays out; `AppConfig` reads config.
- *O*: a new storage is one provider class and one line in the factory;
  a new document is one template class.
- *L*: the in-memory and SQL repositories are interchangeable behind
  `IBarangayRepository`; the RuleChecks harness runs the same rules on the
  in-memory one.
- *I*: screens depend on `IBarangayRepository`, never on a MySQL connection.
- *D*: every view and form receives the repository and services through
  its constructor; `Program.cs` is the only file that names a concrete
  store.

## 4. Bugs fixed along the way

- **Record payment never recorded the payment** (flagged in PR #3): the
  dialog validated the receipt number and the view saved the request
  without ever calling the model's payment method. Payment now goes
  through the model, which enforces the receipt rules itself.
- **The old in-memory seed did not compile**: it passed the wrong type to
  the request constructor. `SampleData` uses the request input model.
- **Frent's schema could not hold the new request model**: no scope, no
  assessed amount, no hours, no income, no first-time-jobseeker flags.
  The columns were added; older databases are upgraded in place by the
  initializer — nobody drops a database with real residents in it.

## 5. Known limits, stated plainly

- One running copy of the program per database. Two clerks on two machines
  will not see each other's changes until one reloads.
- `docs/02-erd.svg` and `docs/03-uml.svg` still showed the old four-table
  design, so they were removed. The current diagrams are the Mermaid ones
  in `docs/10-object-model.md`.
- The classification junction table from the old design is recorded in
  `docs/05` as the upgrade to make when an indexed "every senior citizen"
  query is needed.

## 6. Verification

`python scripts/check_structure.py` (Framework target, every `.cs` in the
project file, folders, XML, assets) passes. **No compiler was available in
the environment this was written in**, so build the solution on Windows and
run `tests\RuleChecks\bin\Debug\RuleChecks.exe` before trusting any of it;
the README has the commands. The first Windows run should also confirm:
first start creates the database and seeds it, a second start does not
re-seed, stopping MySQL mid-session produces the "was not saved" message
and a reload, and `Storage=Memory` still works.
