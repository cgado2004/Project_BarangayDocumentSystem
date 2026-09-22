# Barangay Resident and Document Request Management System — v3

**Barangay Magugpo Poblacion, City of Tagum, Davao del Norte**
Windows desktop application · WinForms · .NET 8

---

## Group

| Member |
|---|
| Dagamac, Emmanuelle Philippe |
| Del Rosario, Jonathan F. |
| Gado, Clint Wood |
| Raborar, Frent Dhieniel |

---

## Running it

**Visual Studio 2022:** open `BarangayDocumentSystem.sln`, set
**BarangayDocumentSystem.App** as the startup project, press **F5**.

```bash
dotnet run --project src/BarangayDocumentSystem.App
```

Requires the **.NET 8 SDK** with the **".NET desktop development"** workload.
Sample data loads on start — no database needed.

> `BarangayDocumentSystem.Core` is a class library and cannot be launched. If
> Visual Studio says *"a project with an Output Type of Class Library cannot
> be started directly"*, the startup project is wrong. That setting lives in a
> gitignored file, so **every teammate must set it once after cloning.**

---

## What changed in v3

| | v2 | **v3** |
|---|---|---|
| Folders | 3 (Domain / Infrastructure / UI) | **2 (Core / App)** |
| Document types | 7 | **20** — the full tarpaulin |
| Barangay Clearance fee | ₱50 placeholder | **₱100 local / ₱200 abroad** |
| Certification fee | ₱50 placeholder | **₱100** |
| Punong Barangay | `[SET THE NAME HERE]` | **HON. EUGENIA SOLIS HINGPIT, MD** |
| Puroks | "Purok 1…5" | **the 14 real puroks** |
| Look | generic WinForms grey | **digital-government style** |

All fee and name data is taken from the barangay's own posted documents — the
Citizen's Charter, the FY 2025 20% Development Fund project list, and the City
Budget Office Letter of Review of 27 November 2024. **The "fees are
placeholders" limitation from the original documentation is now closed.**

---

## Fees actually charged

| Service | Fee |
|---|---|
| Barangay Clearance — local employment | ₱100 |
| Barangay Clearance — for work abroad | ₱200 |
| Certification (residency, good moral, other) | ₱100 |
| Certificate of Indigency | FREE |
| Certificate of Low Income | FREE |
| Business Clearance | ₱200 — **no personal exemptions** |
| Assistance / social-service papers | FREE |

Waivers on top: **RA 9994** senior citizens · **RA 10754** PWDs · indigent
status · **RA 11261** first-time jobseekers (six months' residency, once only).

---

## Project layout

Two projects. The dependency arrow points **inward** — App knows Core, Core
knows nobody.

```
BarangayDocumentSystemV3/
├── BarangayDocumentSystem.sln       ← open this
├── docs/
│   ├── 01-requirements.md           scope, FR, NFR, changes from the PDF
│   ├── 02-erd.svg                   entity relationship diagram
│   ├── 03-uml.svg                   UML class diagram
│   └── 04-project-timeline.md       Sept 22–27 plan
├── db/
│   ├── 01-schema.sql                tables, triggers, views
│   └── 02-seed-data.sql             the same 7 residents as the demo
├── src/
│   ├── BarangayDocumentSystem.Core/          net8.0 — no UI reference
│   │   ├── Entities/     Resident, DocumentRequest, Enums
│   │   ├── Rules/        FeeSchedule, DocumentRenderer, DisplayFormat
│   │   └── Data/         IBarangayRepository, InMemory…, BarangayProfile
│   └── BarangayDocumentSystem.App/           net8.0-windows — WinForms only
│       ├── App.config    every setting that might change
│       ├── AppSettings.cs   reads App.config safely
│       ├── Program.cs    composition root — the only place that picks a store
│       ├── MainShell.cs  sidebar + content + status bar
│       ├── Theme/        AppTheme (colours, fonts), Draw (GDI+ helpers)
│       ├── Controls/     Card, PillButton, Badge, Chip, Sidebar
│       ├── Views/        Dashboard, Residents, Requests
│       └── Dialogs/      resident, request, payment, prompt, preview
└── tests/
    └── RuleChecks/       runnable checks against the Citizen's Charter
```

---

## Settings — `src/BarangayDocumentSystem.App/App.config`

Everything that might change lives here, so nobody has to rebuild to adjust a
fee or a name.

| Key | Meaning |
|---|---|
| `Storage` | `Memory` (sample data, the default) or `MySQL` |
| `Barangay.*` | Name, city, province, Punong Barangay, office hours |
| `Fee.*` | The charter rates, in pesos |
| `Rule.JobseekerResidencyMonths` | The RA 11261 residency requirement |
| `BarangayDb` | The MySQL connection string |

**Leave `Storage` on `Memory` unless you are testing the database** — see
[`docs/05-database-guide.md`](docs/05-database-guide.md).

---

## Database

`db/01-schema.sql` then `db/02-seed-data.sql`, run in that order in phpMyAdmin
or MySQL Workbench. Full instructions, including the common errors and what
they mean, are in [`docs/05-database-guide.md`](docs/05-database-guide.md).

The C# class that talks to MySQL is **not in this build yet**. If you set
`Storage=MySQL` the app tells you so plainly and starts on the sample data
rather than pretending.

**`Core` targets `net8.0`, not `net8.0-windows`, and references no other
project.** Using a WinForms type there is a compile error, not a code-review
note — the layering is enforced by the build.

---

## Interface

Modelled on a digital-government service concept: white canvas, lavender-blue
gradients, rounded cards, pill buttons, heavy headings.

**Everything is clickable.** Dashboard stat cards jump to the filtered list
they summarise; purok chips open the residents of that purok; resident rows
show that person's request history underneath; double-clicking a request opens
the printable document.

**It adapts to the screen.** The window takes 92% of the available work area
up to 1360×860 and never below 1000×640, DPI scaling is on, every view scrolls
rather than clipping, grids fill their width, and dialogs are resizable with
minimum sizes.

---

## Verified

```
dotnet build  →  Build succeeded, 0 errors, 0 warnings  (all three projects)

23 / 23 rule checks passed:
  real purok names · Peña renders with ñ intact
  clearance ₱100 local · ₱200 abroad · certification ₱100
  indigency FREE · low income FREE · business ₱200
  senior waived · PWD waived · senior STILL pays the business fee
  RA 11261: 14 months allowed, 2 months blocked with a reason
  unpaid fee-bearing release REFUSED · released after payment
  reference BMP-2026-0007 · real Punong Barangay on the certificate
  no placeholder text anywhere
```

## Not verified — please read

- **The UI has never been launched.** Everything compiles, but running WinForms
  needs Windows and this was built on Linux. No screen has been seen. Run it
  before the defence.
- **The MySQL scripts have not been executed** against a real server.
- The checks above are a console harness in `tests/RuleChecks`, not a proper
  unit-test framework. Run them with `dotnet run --project tests/RuleChecks`.

## Before submitting

- [ ] Run it on Windows and click through every screen
- [ ] Take screenshots for the documentation
- [ ] Set **BarangayDocumentSystem.App** as the startup project
- [ ] Each member fills in their own row of `docs/04-project-timeline.md`
- [x] ~~Confirm the ₱200 business clearance rate~~ — confirmed, ₱200 stands
