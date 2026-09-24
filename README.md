# BarangayDocumentSystem

One Windows Forms app assembled from the team's work (see **ASSEMBLY.md**
for the full provenance and every seam):

- **UI** — Jonathan F. Del Rosario's Draft branch (forms, controls, templates,
  services), wearing this repository's navy theme and the Barangay Magugpo
  Poblacion logo. The theme is applied through partial-class files
  (`*.Theme.cs`) and `Helpers/ModernTheme.cs`, so his original forms stay
  byte-for-byte his.
- **Database** — Frent Raborar's Draft2 branch (MySQL via `MySql.Data`,
  `Database/DatabaseSettings.cs`, `Database/DatabaseInitializer.cs`),
  connected to Jonathan's `IBarangayRepository` contract through
  `Database/MySqlBarangayRepository.cs` (the seam is documented in
  `ASSEMBLY.md`).
- **Identity** — Barangay Magugpo Poblacion, City of Tagum, Davao del Norte,
  with real purok names in the sample data and the Citizen's Charter fees
  (barangay clearance: PHP 100 local / PHP 200 abroad).

## Build and run

1. Visual Studio 2022 with the **.NET desktop development** workload
   (the app targets **.NET Framework 4.8**).
2. Install [XAMPP](https://www.apachefriends.org) and start **MySQL**
   from the XAMPP Control Panel. The app creates its own database
   (`barangay_db`) and tables on first launch. Default connection is
   `root` with a **blank password** (stock XAMPP) — change it in
   `App.config` (`BarangayDatabase`) if yours differs, or set the
   `BARANGAY_DB_CONNECTION` environment variable.
3. Open `BarangayDocumentSystem.sln`, restore packages (MySql.Data 8.4.0
   restores automatically), and run. `LoadSampleData=true` seeds seven
   residents and six requests on the first run only.
4. The console test project (`Tests`) runs 20+ behavioral checks — run it
   first from the debugger; it is the fastest way to see the rules work.

<<<<<<< HEAD
## Layout

=======
## Troubleshooting the build

**Errors like `CS0246: type or namespace 'MySql' could not be found` or
"metadata file ...exe could not be found"** mean the NuGet package was not
restored on the machine — they are not code errors. Fix:

1. In Visual Studio: **Right-click the solution → Restore NuGet Packages**
   (needs internet access to nuget.org; `NuGet.config` in the repo pins
   that source).
2. If errors persist, the clone likely carries stale `bin/obj` from the
   old .NET 8 app: close VS, delete the `bin` and `obj` folders, reopen,
   restore again, then Build Solution.
3. The `IDE0151 Convert to file-scoped namespace` message is a style
   suggestion only — safe to ignore.

## Layout

>>>>>>> 880f0b7 (build: pin nuget.org via NuGet.config and document the MySql restore fix)
| Folder | What lives there |
| --- | --- |
| `Models/`, `Services/`, `Documents/`, `Printing/` | Jonathan's domain (models, fees, request lifecycle, the seven document templates) |
| `Forms/`, `Controls/` | Jonathan's UI + `*.Theme.cs` partials from the restyle package |
| `Helpers/` | Jonathan's `ErrorLogger`/`UiFeedback` + the navy `ModernTheme`/`ModernControls` |
| `Database/` | Frent's MySQL plumbing + the seam repository + `schema.sql` |
| `Interfaces/` | Jonathan's `IBarangayRepository`/`IDocumentTemplate` + Frent's `RepositoryException` |
| `Assets/` | Barangay seal/logo and the Inter font (SIL OFL license) |
| `Tests/` | Jonathan's behavioral checks (in-memory), plus theme checks |
| `docs/` | Project documentation: requirements, ERD/UML, timeline, fee schedule and legal bases, the legacy v3.2.2 writeup |
| `DBContext/db/` | Reference SQL from the earlier iteration (schema + seed data) |
