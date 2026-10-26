# Barangay Document System

Windows Forms project for Barangay Magugpo Poblacion, Tagum City.
Uses **.NET Framework 4.8** and **MySQL**.

## Group

- Dagamac, Emmanuelle Philippe
- Del Rosario, Jonathan F.
- Gado, Clint Wood
- Raborar, Frent Dhieniel

## Run

1. Install Visual Studio 2022 or newer with .NET desktop development and the .NET Framework 4.8 targeting pack.
2. Start MySQL and set the connection in `BarangayDocumentSystem/App.config`. The `BARANGAY_DB_CONNECTION` environment variable overrides it; keep real credentials out of Git.
3. Open `BarangayDocumentSystem.sln`, restore NuGet packages, build, and press F5.
4. Sign in. An empty users table offers first-account setup; existing resident and request data stays in place.

Startup creates missing tables and recreates the stored procedures. The configured database account needs the corresponding permissions. Sample records are added when `SeedSampleData` is enabled and no residents exist. Disable it for real data.

## Screens

- **Dashboard:** resident, request, and collection totals.
- **Residents / Archive:** add, edit, transfer, and restore residents.
- **Requests:** file requests, record payment, and move documents through release. Check **Filed on** to filter by request date; uncheck it for all dates.
- **Records:** search released documents and print them. Check **Released on** to filter by release date. This is not a payment-date report.
- **Settings:** staff profile, barangay details, and logout.

Unreleased documents open as marked drafts with Copy, Save, and Print disabled. Complete release, then use **Records > View / Print**. Reprints use the stored release date.

## Current limits

Sign-in exists, but roles, account management, password recovery, persistent login throttling, audit logging, and idle locking still need team work. See [UI security notes](docs/UiSecurity.md).

Fee rules are in `BusinessRules/FeeSchedule.cs`. Residency and good moral currently use PHP 100 base fees. Other rates, exemptions, and the 50% student discount still need barangay policy verification. Required-document checks are incomplete.

The app caches database data and has no conflict protection for simultaneous editors. Released documents still use current resident/profile details when reprinted; immutable historical snapshots remain unfinished. Households, census, office-hour rules, and Crystal Reports are also unfinished in this branch. Existing printing uses `PrintDocument`.

## Code

`Program.cs` connects the database and opens sign-in. `MainShell.cs` hosts the pages. `Forms/` and `Views/` contain the UI, `UIHelpers/` contains shared controls/layout, `BusinessRules/` contains fees and document rendering, and `Database/` contains MySQL access and SQL scripts.

Build with Visual Studio/MSBuild. No automated test project is checked into this branch; local verification does not replace testing on the presentation laptop.
