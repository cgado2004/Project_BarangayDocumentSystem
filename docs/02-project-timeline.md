# Project Timeline

> **Note (re-design):** this document describes the earlier build. The
> re-designed system and the list of every request with the file that
> implements it are in `docs/10-revamp-notes.md`; the current database
> scripts are in `BarangayDocumentSystem/Database/Scripts/`.


**Barangay Resident and Document Request Management System**  
September 22–27, 2026

**Group Members:** Dagamac, Emmanuelle Philippe · Del Rosario, Jonathan F. · Gado, Clint Wood · Raborar, Frent Dhieniel

> **Status Update (Sept 28):** All team members have populated their individual logs for the week. On the database task, instead of using individual `01-schema.sql` / `02-seed-data.sql` scripts, Frent's working MySQL persistence from `Fdraft` was integrated and redundant scripts were retired — see `docs/08-integration-notes.md` and `docs/05-database-guide.md` §6.

---

## Week Plan

| Member | Sept 22 (Tue) | Sept 23 (Wed) | Sept 24 (Thu) | Sept 25 (Fri) | Sept 26 (Sat) | Sept 27 (Sun) |
|---|---|---|---|---|---|---|
| **Dagamac, Emmanuelle Philippe** | Error handling for user input | Database queries and DB connections | — | — | — | — |
| **Del Rosario, Jonathan F.** | Checked project structure and fee references; tested input validation | Implemented database persistence | — | Updated technical documentation | Fee values, DocumentRenderer fix, base UI theme | UI overhaul, validation, merge with `master` |
| **Gado, Clint Wood** | Added real Citizen's Charter fees, purok names, and Punong Barangay; applied new UI theme | Ran app on Windows, captured screenshots, resolved initial runtime issues | Conducted validation pass for contact numbers, name fields, and date rules; handled edge cases | Assisted with documentation assembly; verified FR/NFR compliance against actual build | Reorganized directory structure, summarized contributions, refined system inputs using OOP principles and baselines | Updated ERD and UML diagrams; adjusted project files; checked team members' status |
| **Raborar, Frent Dhieniel** | Added real Citizen's Charter fees, purok names, and Punong Barangay; applied new UI theme | Ran app on Windows, captured screenshots, resolved initial runtime issues | Conducted validation pass for contact numbers, name fields, and date rules; handled edge cases | Assisted with final documentation assembly; verified FR/NFR compliance against actual build | Reorganized directory structure, summarized contributions, refined system inputs using OOP principles and baselines | — |

---

## What is Genuinely Finished as of Sept 27
- **Citizen's Charter Pricing:** Updated real fee structures, replacing ₱50 placeholders.
- **Barangay Clearance Tiers:** Two-tier clearance system implemented (₱100 local, ₱200 abroad).
- **Expanded Document Catalog:** 20 document types from the frontline services board (24, including Citizen's Charter financial services).
- **Localized Data:** Added real purok names and the designated Punong Barangay across all certificate templates.
- **Diia-Style UI:** White canvas, lavender gradients, rounded card layouts, and pill buttons.
- **Dashboard & Navigation:** Clickable dashboard, live search functionality, per-resident request history, and status-aware action buttons.
- **Responsive Layout:** Adaptive sizing, scrollable views, resizable dialogs, and high-DPI scaling support.
- **Build Status:** Both projects compile with **0 errors**; **23/23 rule tests pass**.

---

## Suggested Split for Remaining Work

Offered as a flexible baseline for group consensus:

| Task Area | Target Role / Lead |
|---|---|
| Running the app on Windows and reporting runtime breakages | Anyone with Visual Studio installed |
| Capturing UI screenshots for documentation | First member to execute a clean run |
| MySQL setup on XAMPP/NuGet Packages and executing migration scripts | Single owner (to maintain consistent DB state) |
| Proofreading FR/NFR requirements against the actual build | Non-author reviewer (fresh perspective catches missing items) |
| Slide deck preparation and walkthrough script | Designated presenter |
