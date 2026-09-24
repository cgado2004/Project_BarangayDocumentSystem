# BarangayDocumentSystem — `revamp/`

One Windows Forms app assembled from the team's work:

- **UI** — Jonathan F. Del Rosario's Draft branch (forms, controls, templates,
  services), wearing this repository's navy theme and the Barangay Magugpo
  Poblacion logo. The theme is applied through partial-class files
  (`*.Theme.cs`) and `Helpers/ModernTheme.cs`, so his original forms stay
  byte-for-byte his.
- **Database** — Frent Raborar's Draft2 branch (MySQL via `MySql.Data`,
  `Database/DatabaseSettings.cs`, `Database/DatabaseInitializer.cs`),
  connected to Jonathan's `IBarangayRepository` contract through
  `Database/MySqlBarangayRepository.cs` (see `ASSEMBLY.md` for the seam).
- **Identity** — Barangay Magugpo Poblacion, City of Tagum, Davao del Norte,
  with real purok names in the sample data.

## Build and run

1. Visual Studio 2022 with the **.NET desktop development** workload
   (the app targets **.NET Framework 4.8** — the Windows box builds it;
   this sandbox could not run `msbuild`).
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

## Layout

| Folder | What lives there |
| --- | --- |
| `Models/`, `Services/`, `Documents/`, `Printing/` | Jonathan's domain (models, fees, request lifecycle, the seven document templates) |
| `Forms/`, `Controls/` | Jonathan's UI + `*.Theme.cs` partials from the restyle package |
| `Helpers/` | Jonathan's `ErrorLogger`/`UiFeedback` + the navy `ModernTheme`/`ModernControls` |
| `Database/` | Frent's MySQL plumbing + the seam repository + `schema.sql` |
| `Interfaces/` | Jonathan's `IBarangayRepository`/`IDocumentTemplate` + Frent's `RepositoryException` |
| `Assets/` | Barangay seal/logo and the Inter font (SIL OFL license) |
| `Tests/` | Jonathan's behavioral checks (in-memory), plus theme checks |
