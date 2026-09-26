# Project Timeline

**Barangay Resident and Document Request Management System — Version 3**
September 22 – 27, 2026

Group members: Dagamac, Emmanuelle Philippe · Del Rosario, Jonathan F. ·
Gado, Clint Wood · Raborar, Frent Dhieniel

> I have filled in my own row from what I actually did and what I know still
> needs doing. **The other three rows are deliberately left blank** — each
> member should write their own, because a timeline someone else invented for
> you is worth nothing if the professor asks you to explain it.

> **Update, Sept 26 (v3.2).** The plan below is left as I wrote it on
> Sept 22. What actually happened on the database item: instead of wiring
> my own `01-schema.sql` / `02-seed-data.sql`, I integrated Frent's working
> MySQL persistence from `Fdraft` and retired my scripts — see
> `docs/08-integration-notes.md` and `docs/05-database-guide.md` §6.

---

## Week plan

| Member | Sept 22 (Tue) | Sept 23 (Wed) | Sept 24 (Thu) | Sept 25 (Fri) | Sept 26 (Sat) | Sept 27 (Sun) |
|---|---|---|---|---|---|---|
| **Dagamac, Emmanuelle Philippe** | | | | | | |
| **Del Rosario, Jonathan F.** | | | | | | |
| **Gado, Clint Wood** | v3 restructure to two layers (Core + App); real Citizen's Charter fees; real puroks and Punong Barangay; new UI theme | Run the app on Windows, screenshot every screen; fix whatever the first real run exposes | Wire the MySQL repository into v3; run `01-schema.sql` + `02-seed-data.sql` on XAMPP | Validation pass — contact numbers, name fields, date rules; handle edge cases | Help assemble the final documentation; check FR/NFR against what the build actually does | Final walkthrough rehearsal; submission |
| **Raborar, Frent Dhieniel** | | | | | | |

---

## What is genuinely finished as of Sept 22

- Two-layer restructure — `Core` (net8.0, no UI reference) and `App` (WinForms).
- Real fees from the Barangay Citizen's Charter, replacing the ₱50 placeholders.
- Two-tier Barangay Clearance: ₱100 local, ₱200 abroad.
- 20 document types from the barangay's frontline-services board
  (24 in v3.1, with the four Citizen's Charter money services).
- Real purok names and the real Punong Barangay on every certificate.
- Diia-style interface: white canvas, lavender gradients, rounded cards, pill buttons.
- Clickable dashboard, live search, per-resident request history, status-aware buttons.
- Adaptive sizing, scrolling views, resizable dialogs, DPI scaling.
- **Both projects compile with 0 errors; 23/23 rule tests pass.**

## What is NOT finished, and must not be claimed

- **The UI has never been launched.** It compiles, but running WinForms needs
  Windows and the build machine is Linux. Not one screen has been seen on
  screen. This is the single biggest risk before the defence.
- **The MySQL scripts have never been executed** against a real server.
- No unit test project — the rule checks are a throwaway console harness.
- No login or user roles (NFR-11 is explicitly Low priority and deferred).

---

## Suggested split for the remaining work

Offered as a starting point, not a decision — the group should agree it.

| Area | Good fit for |
|---|---|
| Running the app on Windows and reporting what breaks | anyone with Visual Studio installed |
| Screenshots for the documentation | whoever runs it first |
| MySQL setup on XAMPP and running the scripts | one person, so the database state stays consistent |
| Proofreading FR/NFR against the actual build | someone who did **not** write the code — a fresh reader catches claims the author cannot see |
| Slide deck and walkthrough script | presenter |
