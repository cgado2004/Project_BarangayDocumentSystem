# The revamp — what changed, and where to find it

This build is a re-design of the Barangay Document System on top of the
`leader_draft` code. It is still **C# on .NET Framework 4.8** (Visual Studio
2022, Windows Forms), still opens `BarangayDocumentSystem.sln`, and still
talks to **MySQL by default** — now with **SQL Server as a switch** and an
**in-memory store** for demonstrations and rule checks.

Everything below is a request that was made of me, and the place in the code
where it is done. If a reviewer asks "where is it?", this is the page to read.

## The list, item by item

| What was asked | How it is done | Where |
|---|---|---|
| Log-in is required | The program opens the sign-in window first; the main window is only created after a user signs in. Closing the sign-in window closes the program. | `UI/Forms/LoginForm.cs`, `Program.cs` |
| **Register removed** | There is no sign-up screen to remove — accounts are created by an administrator from the Users screen, and the first two accounts come from `App.config`. | `UI/Forms/AccountForms.cs` (`UserForm`), `Database/DatabaseInitializer.cs` |
| Spacing / clean layout | A spacing scale (`Gap1..Gap5` = 4/8/12/16/24 px), a 24 px page margin, one control height for every field and button, and one factory that builds all the furniture. No screen sets its own random padding. | `UI/AppTheme.cs`, `UI/UiFactory.cs` |
| **Name shown in a Group Box** | The resident form (and the dependent form) put the name fields inside a real `GroupBox` titled *Name*. | `UI/Forms/ResidentForm.cs`, `UI/Controls/GroupBoxes.cs` (`NameGroupBox`) |
| **Erase Address** | There is no `Address` field on the resident, no Address column, and no address anywhere on a printed document. The database column is gone from both schema scripts too. | `Models/Resident.cs`, `UI/Forms/ResidentForm.cs`, `Services/Documents/*`, `Database/Scripts/*` |
| **Head of the family included** | The resident form has a *Household* section only when the person is ticked as head of the family; the head's dependents are registered right there, and the census counts them. | `UI/Forms/ResidentForm.cs`, `Models/Dependent.cs`, `Services/ResidentService.cs`, `Services/CensusService.cs` |
| **Classification: erase Student** | `ResidentClassification` has no `Student` member. The classifications are senior citizen, PWD, indigent, solo parent and 4Ps beneficiary. | `Models/Enums.cs` |
| **Student becomes a fee category** | A tick box on the resident form, *Student fee category*, gives a configurable discount on the listed documents and nothing else. Business clearance is excluded. | `Models/Resident.cs` (`IsStudentFeeCategory`), `Services/FeeSchedule.cs`, `App.config` (`Fee.Student.*`) |
| **Dependents (registration)** | Dependents are registered under their head of family with their relation, birthday and whether they are studying. | `Models/Dependent.cs`, `UI/Forms/DependentForm.cs`, `Services/ResidentService.cs` |
| **Deactivation / Archive** | A resident is **Active, Inactive or Archived** — never deleted. Changing state asks for a reason, records who did it and when, and keeps the history. The Users screen likewise deactivates accounts instead of deleting them. | `Models/Enums.cs` (`RecordState`), `Models/Resident.cs`, `UI/Forms/ResidentStateForm.cs`, `UI/Views/ResidentsView.cs`, `Services/UserService.cs` |
| **New Request replaces "New Document"** | The Requests screen's button is *New request*; the wording everywhere is request/document, never "New Document". | `UI/Forms/RequestForms.cs`, `UI/Views/RequestsView.cs` |
| **Barangay Business Clearance (if classified)** | Ticking *Business owner* on the resident (or choosing the business clearance) brings out the business fields — name, nature, purok, ownership, registration, employees, renewal. The clearance goes to **Processing** for inspection and is never waived. | `Models/BusinessDetails.cs`, `UI/Forms/RequestForms.cs`, `Services/FeeSchedule.cs`, `Services/TimeWindowPolicy.cs` |
| **Erase Basis (NFR)** | The legal basis is **not shown at the counter**. It is still stored and it still appears in the activity log and the fee report, which is what makes the amounts defensible; no screen prints "RA 7160 Sec. …" at the resident. The permission `ViewFeeBasis` exists for the administrator. | `Services/FeeSchedule.cs` (`Basis` is `internal`), `Security/Permissions.cs` |
| **Confirmation on New Request** | After the form is filled, a confirmation dialog shows the resident, the document, the fee and which status the request will start in. What the dialog says is what gets saved — both come from the same `Preview`. | `UI/Forms/RequestForms.cs` (`ConfirmationForm`), `Services/RequestService.cs` (`Preview`) |
| **Another classification under FR 07** | 4Ps beneficiary (`FourPsBeneficiary`) was added to the classifications, with the same exemption treatment as the other personal classifications. | `Models/Enums.cs`, `Services/FeeSchedule.cs` |
| **Time shift 8AM–4PM cleared, 4:01PM–7:59AM pending** | One policy class decides: inside the window and nothing to validate → **Cleared** on the spot; outside the window → **Pending**; anything that needs checking (business) → **Processing**. Requests left waiting are cleared automatically the next morning, and every request records why it started where it did. | `Services/TimeWindowPolicy.cs`, `Services/RequestService.cs` (`ClearWaitingRequests`), `App.config` (`Rule.OfficeWindow*`) |
| **Government OR / control receipt** | A receipt needs three numbers that must agree — booklet series, control number and OR number — drawn from a booklet registered in the booklets screen. A receipt can be voided with a reason; a voided receipt stays in the register but stops counting as income. | `Models/OfficialReceipt.cs`, `Services/ReceiptService.cs`, `UI/Forms/PaymentForm.cs`, `UI/Views/AdminViews.cs` (`ReceiptsView`) |
| **Activity log** | Every sign-in, sign-out, failed sign-in, record change, status change, collection and void is appended with who, when, and what module. The log is read-only by design — there is no edit and no delete. | `Services/ActivityLogService.cs`, `UI/Views/AdminViews.cs` (`ActivityLogView`), `Database/Scripts/*` (`activity_log`) |
| **Per-day transaction filter** | The Requests screen has Today / Yesterday / This week / This month / Last 30 days / Choose dates, and the dashboard plus the *Per-day transactions* report group the same figures by day. | `UI/Views/RequestsView.cs`, `UI/Views/ViewBase.cs` (`ApplyQuickRange`), `Services/Reports/ReportDefinitions.cs` |
| **Status Temporary / Permanent / Newcomer** | Residency status is suggested from the date of residency (newcomer under 6 months, temporary until 5 years, permanent after) and the clerk can see why. | `Models/Enums.cs` (`ResidencyStatus`), `Models/Resident.cs` (`SuggestResidencyStatus`), `Services/ResidentService.cs` |
| **Crystal Report** | Real Crystal Reports support, loaded at run time so the project still builds without it; eleven report definitions, each with a plain-paper fallback that prints the same figures. | `Services/Reports/*`, `docs/08-crystal-reports.md` |
| **Processing if applied to business** | A business clearance cannot start Cleared: it is forced through Processing, and the workflow screen only offers the buttons that are valid for the state it is in. | `Services/TimeWindowPolicy.cs`, `Models/DocumentRequest.cs`, `UI/Forms/RequestWorkflowForm.cs` |
| **Census** | Population, households, dependents, age brackets, purok, classifications and residency mix, with a CSV export and a printable census summary. | `Services/CensusService.cs`, `UI/Views/CensusView.cs` |

## The non-functional list

| What was asked | How it is done |
|---|---|
| **Must be `.config`-friendly** | Every fee, hour, rule, threshold and folder is a line in `App.config` with a sentence explaining it. One class reads the file; if a key is missing the program falls back to the value it had before, so a half-copied config never stops the system. `AppConfig.SettingsFile` even tells you which file it read. |
| **DBHelper found** | One connection helper that both providers share: it opens connections, times every command, scrubs passwords out of what it logs, and knows how MySQL and SQL Server differ when calling a stored procedure (`CALL name(@a, @b)` versus named parameters). |
| **DBContext found** | The unit of work: it holds the chosen provider, the connection string, creates the database if it is missing, checks that the tables and the eleven stored procedures are there, and installs what is missing. |
| **Database connected to the repo** | The schema and the stored procedures live **in this repository** as the only copy that matters, and they are embedded in the `.exe` as resources so the installed program always creates the schema that matches its own code. |
| **Stored procedures exist** | Eleven of them, with the same names and the same parameter order in both scripts: per-day transactions, document register, collections, business clearances, census summary, population by purok, population by age, resident masterlist, activity log search, dashboard summary, clear-waiting-requests. |
| **Comments are first-person and not too technical** | Every source file opens with a plain-language header and the comments say what the code does in my own words ("I put this here because …"), not in textbook language. |
| **Adaptive to VS 2022+** | C# 10 with `LangVersion` set explicitly, no designer files to break, no Crystal reference to install, and the one NuGet package (`MySql.Data`) restores itself. |
| **Logically arranged** | Folders by layer: `Config`, `Models`, `Interfaces`, `Data` (+`Data/Sql`), `Database`, `Security`, `Services` (+`Documents`, `Reports`), `UI` (+`Controls`, `Dialogs`, `Forms`, `Views`). |
| **Data secured** | PBKDF2 passwords (100,000 rounds, per-account salt), lock-out after repeated failures, role-based permissions enforced *inside the services* rather than only by hiding buttons, parameterised SQL everywhere via `SqlArguments`, and an optional DPAPI-sealed or environment-variable connection string. |
| **OOD / OOM / OOP** | Objects that own their own rules (`Resident`, `DocumentRequest`, `OfficialReceipt`), interfaces for every screen-facing contract, the composition root in `Program.cs`, and services that take their dependencies through constructors. |
| **Working properly** | `tests/RuleChecks` runs the real classes and checks the money rules, the office window, the workflow, the passwords, the receipts, the census and the reports. |

## How to run it

1. Open `BarangayDocumentSystem.sln` in Visual Studio 2022.
2. Press **F5** with MySQL running (XAMPP is fine). The program creates the
   database, the tables and the stored procedures by itself and seeds the two
   accounts from `App.config`.
3. To show it without a database at all, set `Storage` to `Memory` in
   `App.config`. Nothing is saved, which is exactly what a demonstration
   wants.
4. Sign in as `admin` / `Barangay@2026` (or `clerk` / `Clerk@2026`) and change
   the password when asked. **Change those passwords before real use.**

## What I removed, and why

* The **sign-up screen** and the **Delete** buttons. Nothing in this system
  deletes a person, an account or a log entry; things are deactivated,
  archived or voided with a reason.
* The **address**, from the form, the database and every printed paper.
* **Student** as a classification — it is a fee category now.
* The **legal basis text at the counter** — recorded, not displayed.
* The old `BusinessRules`, `UIHelpers`, `CustomControls`, `Forms`, `Views`
  and `MainShell` folders, which the new layers replace completely.
