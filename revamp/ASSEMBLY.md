# ASSEMBLY.md — how `revamp/` was assembled

This folder is a **composite**: one buildable .NET Framework 4.8 WinForms app
pieced together from three sources, each credited by branch and commit, with
every junction between them documented. Nothing here is rewritten from
scratch — where a teammate's file was reused it is **verbatim**; where it had
to change, the change is listed in "Adaptations" below.

## Sources

| Source | Author | Commit used | What was taken |
| --- | --- | --- | --- |
| `draft-ui-restyle` (the restyle package, `draft2-pkg` at Arena) | theme package built in this repo; UI base is Jonathan's | `b7dcee0` | `Helpers/ModernTheme.cs`, `Helpers/ModernControls.cs`, `Controls/*.Theme.cs` (3), `Forms/MainForm.Theme.cs`, `Tests/ThemeChecks.cs`, docs |
| `Draft` | **Jonathan F. Del Rosario** | `77485c6` | the whole application: `Program.cs`, `App.config`, `Configuration/`, `Models/`, `Services/`, `Documents/`, `Printing/`, `Data/` (SampleData only), `Forms/`, `Controls/`, `Tests/` harness, `Properties/`, `.sln`, csproj base |
| `Draft2` | **Frent Raborar** | `12c37db` | the MySQL stack: `Database/DatabaseSettings.cs`, `Database/DatabaseInitializer.cs`, `Interfaces/RepositoryException.cs`, the `MySql.Data` package reference, and the "app creates its own database" philosophy |
| `leader_draft` (this repository) | **Clint Wood Gado** | `87926d9`… | the identity: `Assets/barangay-logo.png`, `Assets/fonts/` (Inter, SIL OFL), the navy color palette, `.gitignore`, real barangay facts (puroks, legal bases, Citizen's Charter fees) |

Team map for reference: `leader_draft` = Clint (the user), `Draft` = Jonathan,
`Draft2` = Frent, `draft3` = Phillippe (not used here), `master` = the
original shared upload (its code is Frent's Draft2 work).

## The seam: `Database/MySqlBarangayRepository.cs`

The one file where both worlds meet. Jonathan's Draft talks to SQL Server
LocalDB through `Data/SqlBarangayRepository.cs` (Draft-only; **not** copied).
Frent's Draft2 has its own MySQL repository with a different interface. The
revamp keeps **Jonathan's interface** (`Interfaces/IBarangayRepository.cs`,
8 members: `GetResidents`, `GetResident`, `SaveResident`, `DeleteResident`,
`GetRequests`, `GetRequest`, `SaveRequest`, `ExecuteInTransaction`) and
**Frent's plumbing**, so the seam class translates Jonathan's persistence
design into MySQL:

| Jonathan's design (kept, byte-for-byte semantics) | How it lands in MySQL here |
| --- | --- |
`Version` optimistic concurrency on every save | `UPDATE … WHERE resident_id=@id AND version=@version` + row-count check; new rows get `version = 1` |
Detached copies on every read | same — repositories return fresh model instances; services hand out `.Copy()` |
Resident snapshot written with the request, one transaction | `SaveRequest` uses `ExecuteInTransaction` + a second INSERT into `resident_snapshots`; reads JOIN it |
`ExecuteInTransaction` re-entrancy, Serializable | identical pattern with `MySqlTransaction` |
Receipt number unique while paid | unique index `ux_requests_receipt`; unpaid rows store `NULL` (the app maps `""`→`NULL`, and MySQL unique indexes ignore NULLs — LocalDB needed a filtered index, MySQL 8 gets the same effect) |
Once-only first-time jobseeker (RA 11261) | generated column `jobseeker_guard` + unique index `ux_requests_jobseeker` — the trigger-free equivalent of Draft's filtered index |
Friendly errors for duplicate receipt / jobseeker / delete-with-history | `catch (MySqlException)` on numbers 1062 and 1451/1452 with Jonathan's exact message texts |
Schema exists before seeding, seed exactly once | Frent's `DatabaseInitializer.EnsureCreated` creates `barangay_db` + tables, then `Initialize` seeds under a `SELECT … FOR UPDATE` gate on `app_state` |
CHECK-guarded states (payment/release/rejection/type/status/fee) | translated to MySQL CHECK constraints (enforced on MySQL 8.0.16+; the C# services enforce the same rules regardless) |

Not carried over from Draft: `Data/SqlDatabase.cs`, `SqlBarangayRepository.cs`,
`SqlResidentMapping.cs`, `SqlRequestMapping.cs`, `Data/Schema.sql` (the
T-SQL originals), `master`-branch LocalDB scripts, and the three
`--sql` console tests (`DatabasePersistence`, `DatabaseSamples`,
`DatabaseConstraints`) and their `Tests/SqlTestDatabase.cs` helper — the
LocalDB stack is gone by decision (grill answer 3). MySQL behavior is
exercised by the app itself.

Column naming follows Frent's snake_case dialect; enum columns store INTs per
Jonathan's mapping; string lengths and decimal precision are Jonathan's.

## The full_scope seam (grill answer 2)

The Citizen's Charter prices the Barangay Clearance two ways: **₱100 local
employment, ₱200 work abroad**. That needed a concept neither branch had:

- new enum `Models/ClearanceScope.cs` (`Local`, `Abroad`), default `Local`;
- `RequestDetails.Scope` and `DocumentRequest.Scope` (travels through
  create → save → reopen; rides `MemberwiseClone` in `Copy()`);
- `document_requests.scope TINYINT` in `Database/schema.sql`;
- `FeeSchedule.Assess(resident, type, scope)` overload — the original
  two-argument signature stays and means Local, so every existing caller is
  untouched; `ClearanceFee` is now the Charter's ₱100 and
  `ClearanceAbroadFee` = ₱200;
- `RequestService.Assess/Create` thread the scope through;
- `RequestForm` gains a "Clearance scope" combo, built **at run time** in
  `BuildScopeRow()` so Jonathan's `RequestForm.Designer.cs` stays untouched;
  it enables only for the Barangay Clearance;
- `SampleData`'s paid clearance is abroad (₱200), so the dashboard's totals
  visibly exercise the new column.

## Adaptations (every deviation from verbatim sources)

1. `Database/DatabaseSettings.cs` — connection name `BarangayDb` →
   `BarangayDatabase` so Frent's settings and Jonathan's `AppSettings` read
   the same `App.config` entry. Default connection string unchanged:
   stock XAMPP, `root`, **blank password** (no credentials in source).
2. `Configuration/AppSettings.cs` — connection validation
   `SqlConnectionStringBuilder` → `MySqlConnectionStringBuilder` (one token).
3. `Program.cs` — uses `DatabaseSettings.Load()` + `MySqlBarangayRepository`;
   `ModernTheme.Resolve()` before any form is built; LocalDB help text →
   XAMPP/MySQL help text in the startup `MySqlException` handler.
4. `Helpers/UiFeedback.cs` — `SqlException` → `MySqlException` (same
   friendly fallback message).
5. `Services/FeeSchedule.cs` — Charter rates live here now: clearance
   ₱100/₱200 by scope; Residency ₱100 and Good Moral ₱100 were already the
   Charter rates in the restyle package's base. Comments name the source.
6. `Forms/MainForm.cs` — three surgical lines: the `reporting` field
   (for the theme's footer), its assignment in the DI constructor, and the
   `ShowPage` highlight block now reads `ModernTheme.PrimaryNavy` instead of
   hardcoded green, then calls `RefreshFooter()` (defined in the partial).
7. `Controls/DashboardControl.cs` — raises `StatisticsUpdated` (new event)
   at the end of `RefreshData`; `DashboardControl.Theme.cs` listens and
   paints the peso/RELEASED overlays and purok bars.
8. `Forms/RequestForm.cs` — `BuildScopeRow()` (above) + `Scope` on the
   submitted `RequestDetails`.
9. `Data/SampleData.cs` — real Magugpo Poblacion puroks (Tandang Sora,
   Orchids, Sampaguita, Sunflower, Cristo Rey) and the abroad clearance.
10. `Tests/Program.cs` — `--sql` machinery removed; `ThemeChecks.Run`
    added; fee/total expectations updated for the Charter clearance
    (₱100 local / ₱200 abroad).
11. `BarangayDocumentSystem.csproj` — rebuilt from Jonathan's file:
    target **v4.8** + `LangVersion latest` (Frent's recipe, needed by the
    theme partials), `MySql.Data 8.4.0` PackageReference, Compile list
    swapped (Sql* out; Database/* + Helpers/* + *.Theme.cs +
    `Models/ClearanceScope.cs` in), EmbeddedResource moved to
    `Database\schema.sql`, `Assets/` shipped as Content.
12. `App.config` — `BarangayDatabase` connection is MySQL/XAMPP
    (blank password by default), `supportedRuntime` → v4.8. Punong Barangay
    stays a placeholder until the real name is confirmed.
13. `Tests/BarangayDocumentSystem.Tests.csproj` — v4.8, `SqlTestDatabase.cs`
    removed, `ThemeChecks.cs` added. Project references unchanged.

Everything else in `revamp/` is a verbatim copy from the sources above.

## Build order (first run on a Windows box)

1. `git checkout unified` (see below), open `BarangayDocumentSystem.sln`.
2. Build the solution — NuGet restores `MySql.Data 8.4.0` on first build.
3. Start XAMPP → MySQL. Run the **Tests** project (in-memory checks + theme
   checks; expect all green).
4. Run the **app**. On first launch it creates `barangay_db`, the four
   tables, and (because `LoadSampleData=true`) seeds seven residents and six
   requests exactly once — reopening the app never re-seeds.
5. To verify the seam end-to-end: pay a clearance in the app, note the OR
   number, try paying another request with the same number — the app must
   refuse with the receipt message (that refusal comes from the MySQL unique
   index through the seam).

## Publishing as the `unified` branch (when ready)

The revamp deliberately does **not** overwrite Frent's `Draft2` branch.

```bash
# from the Arena working branch (which holds revamp/)
git checkout -b unified                       # wait — see note
```

Concretely, the copy-in recipe (run on the machine holding this checkout):

```bash
git branch unified arena/01a0cf05-project-barangaydocumentsystem   # start point = this session's branch
git checkout unified
git add revamp/ && git commit -m "revamp: composite app (Draft UI + Draft2 MySQL + charter rates)"
git push origin unified
```

If the team prefers `revamp/` flattened to the repo root on `unified`, do a
follow-up commit that `git mv revamp/* .` (solution paths then match
Jonathan's and Frent's layouts one level up). Either shape builds; the
csproj uses relative paths inside `revamp/` as shipped.
