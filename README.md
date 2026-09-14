# Barangay Resident and Document Request Management System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**

A Windows Forms desktop application for barangay staff: maintain the resident
registry, accept document requests, assess fees against the correct statutory
exemptions, track each request through its workflow, and print the finished
certificate.

---

## Group Members

| Name |
|---|
| Dagamac, Emmanuelle Philippe |
| Del Rosario, Jonathan |
| Gado, Clint Wood |
| Raborar, Frent Dhieniel |

> Roles and task assignments: **TBD**.

---

> **v2 — refactored.** Split into three projects for SOLID, duplication
> removed for DRY, and the TabControl replaced with a sidebar shell.
> Full write-up: [`docs/04-refactor-notes.md`](docs/04-refactor-notes.md).

## Running it

**Visual Studio 2022:** open `BarangayDocumentSystem.sln`, set
**BarangayDocumentSystem.UI** as the startup project, press **F5**.

**Command line:**
```bash
dotnet run --project src/BarangayDocumentSystem.UI
```

> Three projects now, so the startup project matters. Domain and
> Infrastructure are class libraries and cannot be launched.

Requires the **.NET 8 SDK** and the **".NET desktop development"** workload.
Sample data loads automatically — no database needed.

---

## What it does

Three destinations in the left sidebar.

### Residents
Register, edit, search, and delete residents. Tracks name, birth date, gender,
civil status, purok, address, contact, occupation, voter status, date of
residency, and **classifications** (senior citizen, PWD, indigent, student,
solo parent).

### Document Requests
File a request, then move it through the workflow:

```
Pending → Processing → Ready for Release → Released
   └──────────┴────────────────┴──────────→ Rejected
```

Record payment against an official receipt number, and view or print the
finished document.

### Dashboard
Resident counts, request counts by status, revenue collected, documents issued
free of charge, and breakdowns by document type and purok.

---

## Documents supported

| Document | Base fee* | Notes |
|---|---|---|
| Barangay Clearance | ₱50 | Most requested |
| Certificate of Residency | ₱50 | Proof of address |
| Certificate of Indigency | **FREE** | DILG MC 2019-177 |
| Barangay Business Clearance | ₱200 | Personal exemptions do **not** apply |
| Barangay ID | ₱100 | |
| First-Time Jobseeker Certificate | **FREE** | RA 11261 |
| Certificate of Good Moral Character | ₱50 | |

\* **Placeholders — see the warning below.**

---

## ⚠️ Fee amounts must be replaced before real use

Under the **Local Government Code (RA 7160, secs. 152–186)** a barangay may
only collect a fee fixed by a **duly enacted barangay revenue ordinance**.
Collecting without one is **illegal exaction**.

The amounts in `Domain/Services/FeeSchedule.cs` are typical Philippine ranges used so
the program runs. **Replace them with the actual Magugpo Poblacion ordinance
rates.** They are all declared as constants at the top of that one file for
exactly that reason.

---

## Statutory exemptions built in

These are real Philippine laws, not invented rules — which is what makes this
a domain model rather than a generic CRUD app.

### RA 11261 — First Time Jobseekers Assistance Act
Barangay clearance and certification are **free** for a qualified first-time
jobseeker. The system enforces all three conditions:

1. **Six months' residency** — computed from the resident's `DateOfResidency`
2. **Once only** — a flag is set when the certificate is released
3. Filipino citizenship — assumed for registered residents

If a resident fails either testable condition, the request dialog **disables
the submit button and explains why**. The printed certificate includes the
Oath of Undertaking that receiving agencies (NBI, PSA, BIR) look for.

### Others
| Law | Effect |
|---|---|
| **RA 9994** | Senior citizen — document fees waived |
| **RA 10754** | PWD — document fees waived |
| **DILG MC 2019-177** | Certificate of indigency issued free |
| **RA 11032** | Official receipt required for every collection |

> **Business clearance is deliberately excluded** from personal exemptions —
> it is a regulatory fee on an enterprise, not a personal document. A senior
> citizen still pays it.

---

## Project layout

Three projects. The dependency arrow points **inward** — UI knows Domain,
Domain knows nobody.

```
BarangayDocumentSystem/
├── BarangayDocumentSystem.sln          ← open this in Visual Studio
├── README.md
├── docs/
│   ├── 01-system-design.md             domain rules & workflow
│   └── 04-refactor-notes.md            DRY / SOLID write-up
└── src/
    ├── BarangayDocumentSystem.Domain/           net8.0 — no UI reference
    │   ├── Abstractions/
    │   │   ├── IBarangayRepository.cs   storage contract + ResidentDetails
    │   │   └── IDocumentTemplate.cs     one-document contract + BarangayProfile
    │   ├── Entities/
    │   │   ├── Resident.cs              registry record
    │   │   ├── DocumentRequest.cs       request + guarded status transitions
    │   │   └── Enums.cs                 DocumentType, RequestStatus, …
    │   ├── Services/
    │   │   ├── FeeSchedule.cs           ALL fee rules and exemptions
    │   │   └── DocumentRenderer.cs      page layout, written once
    │   └── Templates/                   one class per document (7)
    │
    ├── BarangayDocumentSystem.Infrastructure/   net8.0
    │   └── InMemoryBarangayRepository.cs        swap for MySQL later
    │
    └── BarangayDocumentSystem.UI/               net8.0-windows ← WinForms only here
        ├── Program.cs                   composition root — wires everything
        ├── MainShell.cs                 sidebar + content + status bar
        ├── Theme/AppTheme.cs            every colour, font, spacing value
        ├── Common/
        │   ├── Dialog.cs                all message boxes (6 methods)
        │   ├── InputValidator.cs        reusable field validation
        │   ├── UiFactory.cs             themed control construction
        │   └── NavigationSidebar.cs     left nav rail
        ├── Views/                       Dashboard / Residents / Requests
        └── Forms/                       modal dialogs
```

**`Domain` targets `net8.0`, not `net8.0-windows`, and references no other
project.** Using a WinForms type there is a compile error, not a code-review
note — Dependency Inversion enforced by the build.

**No fee arithmetic and no status rules live in the UI.** They sit in
`FeeSchedule` and `DocumentRequest`, so they hold regardless of what the
interface does — and can be tested without clicking anything.

---

## Sample data

Seven residents across Puroks 1–5, chosen to exercise every rule:

| Resident | Why they're there |
|---|---|
| Juan Dela Cruz | Ordinary resident — pays full fees |
| Maria Reyes | **Senior citizen** — fees waived |
| Jose Bautista Jr. | 14 months' residency — **qualifies** under RA 11261 |
| Ana Villanueva | Solo parent, business owner |
| Pedro Mendoza | **Indigent** — fees waived |
| Liza Torres | **Student + PWD** — multiple classifications |
| Carlo Aquino | 2 months' residency — **fails** the RA 11261 six-month test |

Try filing a First-Time Jobseeker Certificate for **Carlo** — the dialog blocks
it and explains why. Then try **Jose** — it goes through, free of charge.

---

## Expected behaviour

> **These are design intentions, not test results.** The project has **not been
> compiled or run** — the environment it was written in could not install the
> .NET SDK — and there are **no automated tests**. The fee and eligibility
> logic was checked by porting it to a scratch script and running the cases
> below; the rules were then written into C# to match. Everything here should
> be confirmed by actually running the app.

| Scenario | Expected outcome | Enforced by |
|---|---|---|
| Senior requests clearance | ₱0, cites RA 9994 | `FeeSchedule.Assess` |
| PWD requests clearance | ₱0, cites RA 10754 | `FeeSchedule.Assess` |
| Indigent requests clearance | ₱0 | `FeeSchedule.Assess` |
| Ordinary resident, clearance | ₱50 | `FeeSchedule.Assess` |
| Senior requests **business** clearance | **₱200** — not waived | `FeeSchedule.Assess` (checked before personal exemptions) |
| Jose (14 mo) → jobseeker cert | Allowed, free | `CanIssueJobseekerCertificate` |
| Carlo (2 mo) → jobseeker cert | **Blocked** — under 6 months | `CanIssueJobseekerCertificate` |
| Already availed → jobseeker cert | **Blocked** — once only | `HasAvailedFirstTimeJobseeker` |
| Release unpaid fee-bearing doc | **Blocked** until paid | `DocumentRequest.Release` |
| Reject a released document | **Blocked** | `DocumentRequest.Reject` |

Each row names the method that enforces it, so any claim can be checked
against the source.

---

## Notable techniques

- **`[Flags]` enum** for classifications — a resident can be senior *and*
  indigent; combined with bitwise `|=`
- **Guarded state transitions** in `DocumentRequest` — illegal moves throw,
  and the UI catches and reports instead of crashing
- **Live fee assessment** — the fee and its legal basis update as the document
  type changes, before anything is committed
- **`TryParse`, never `Parse`**, and `KeyPress` filtering on numeric fields
- **`PrintDocument`** for real printing — framework only, no NuGet package
- **`Prompt.cs`** built entirely in code — proof the designer is a
  convenience, not a requirement

Added in the v2 refactor:

- **Two interfaces** (`IBarangayRepository`, `IDocumentTemplate`) — the UI
  names a concrete storage class in exactly one place, `Program.cs`
- **One template class per document** — adding an eighth certificate means
  adding a file and one line, never editing the renderer
- **`Dialog` / `InputValidator` / `UiFactory`** — 18 raw `MessageBox.Show`
  calls reduced to 0; validation reads as a chain of conditions
- **`AppTheme`** — every colour, font and spacing value in one file
- **System fonts only** (Segoe UI, Consolas). A font that is not installed
  does not error; Windows substitutes different metrics and the layout
  silently breaks on the grader's machine

---

## Not included

Honest scope notes:

- **No database.** `InMemoryBarangayRepository` holds everything in memory and
  data is lost on exit. It is the only class that knows where data lives, so a
  MySQL version implements `IBarangayRepository` and is selected by one line
  in `Program.cs` — no view or form changes.
- **No login or user roles.** A real deployment needs at least clerk vs.
  captain separation.
- **No photo or biometric capture** for barangay IDs.
- **Punong Barangay name is a placeholder** — set it in
  `BarangayProfile.MagugpoPoblacion` (`Domain/Abstractions/IDocumentTemplate.cs`).
- **No blotter/case module** — a real clearance checks for pending cases;
  here that is asserted, not verified.
- **No unit tests.** The refactor makes them possible — `IBarangayRepository`
  can now be faked — but none are written yet.

---

## Before submitting

- [ ] Replace the fee constants with the real Magugpo Poblacion ordinance rates
- [ ] Set the actual Punong Barangay name in `BarangayProfile.MagugpoPoblacion`
      (`Domain/Abstractions/IDocumentTemplate.cs`)
- [ ] Push to GitHub — **keep it private** until the module ends
