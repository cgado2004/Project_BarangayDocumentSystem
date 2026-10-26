# Barangay Document System

Windows Forms project for Barangay Magugpo Poblacion, Tagum City.
Uses **.NET Framework 4.8** and **MySQL**.

## Group

- Dagamac, Emmanuelle Philippe
- Del Rosario, Jonathan F.
- Gado, Clint Wood
- Raborar, Frent Dhieniel

## Screens

- **Dashboard:** resident, request, and collection totals.
- **Residents / Archive:** add, edit, transfer, and restore residents.
- **Requests:** file requests, record payment, and move documents through release. Check **Filed on** to filter by request date; uncheck it for all dates.
- **Records:** search released documents and print them. Check **Released on** to filter by release date. This is not a payment-date report.
- **Settings:** staff profile, barangay details, and logout.

Unreleased documents open as marked drafts with Copy, Save, and Print disabled. Complete release, then use **Records > View / Print**. Reprints use the stored release date.

## Code

`Program.cs` connects the database and opens sign-in. `MainShell.cs` hosts the pages. `Forms/` and `Views/` contain the UI, `UIHelpers/` contains shared controls/layout, `BusinessRules/` contains fees and document rendering, and `Database/` contains MySQL access and SQL scripts.

Build with Visual Studio/MSBuild. No automated test project is checked into this branch; local verification does not replace testing on the presentation laptop.
