# Barangay Resident and Document Request Management System

**Barangay Magugpo Poblacion · City of Tagum · Davao del Norte**

A Windows Forms desktop application for barangay staff: maintain the resident
registry, accept document requests, assess fees against the correct statutory
exemptions, track each request through its workflow, and print the finished
certificate.

---

## Running it

**Visual Studio 2022:** open `BarangayDocumentSystem.sln`, press **F5**.

**Command line:**
```bash
dotnet run --project src/BarangayDocumentSystem
```

Requires the **.NET 8 SDK** and the **".NET desktop development"** workload.
Sample data loads automatically — no database needed.

---

## What it does

### Residents tab
Register, edit, search, and delete residents. Tracks name, birth date, gender,
civil status, purok, address, contact, occupation, voter status, date of
residency, and **classifications** (senior citizen, PWD, indigent, student,
solo parent).

### Document Requests tab
File a request, then move it through the workflow:

```
Pending → Processing → Ready for Release → Released
   └──────────┴────────────────┴──────────→ Rejected
```

Record payment against an official receipt number, and view or print the
finished document.

### Dashboard tab
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

The amounts in `Services/FeeSchedule.cs` are typical Philippine ranges used so
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

```
BarangayDocumentSystem/
├── BarangayDocumentSystem.sln     ← open this in Visual Studio
├── README.md
├── docs/
│   ├── 01-system-design.md        Design rationale & class model
│   └── screenshots/               put your screenshots here
└── src/BarangayDocumentSystem/
    ├── Program.cs                 entry point + message loop
    ├── Models/
    │   ├── Resident.cs            registry record
    │   ├── DocumentRequest.cs     request + guarded status transitions
    │   └── Enums.cs               DocumentType, RequestStatus, …
    ├── Services/
    │   ├── FeeSchedule.cs         ALL fee rules and exemptions
    │   └── DocumentPrinter.cs     certificate text generation
    ├── Data/
    │   └── BarangayRepository.cs  in-memory store (swap for MySQL later)
    └── Forms/
        ├── MainForm.cs            three-tab shell
        ├── ResidentForm.cs        register / edit
        ├── RequestForm.cs         file a request, live fee assessment
        ├── PaymentForm.cs         record payment + O.R.
        ├── DocumentPreviewForm.cs preview, save, print
        └── Prompt.cs              code-built input dialog
```

**No fee arithmetic and no status rules live in the forms.** They sit in
`FeeSchedule` and `DocumentRequest`, so they hold regardless of what the UI
does — and can be tested without clicking anything.

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

## Verified behaviour

| Test | Expected | Result |
|---|---|---|
| Senior requests clearance | ₱0, cites RA 9994 | ✅ |
| PWD requests clearance | ₱0, cites RA 10754 | ✅ |
| Indigent requests clearance | ₱0 | ✅ |
| Ordinary resident, clearance | ₱50 | ✅ |
| Senior requests **business** clearance | **₱200** — not waived | ✅ |
| Jose (14 mo) → jobseeker cert | Allowed, free | ✅ |
| Carlo (2 mo) → jobseeker cert | **Blocked** — under 6 months | ✅ |
| Already availed → jobseeker cert | **Blocked** — once only | ✅ |
| Release unpaid fee-bearing doc | **Blocked** until paid | ✅ |
| Reject a released document | **Blocked** | ✅ |

---

## Notable techniques

- **`[Flags]` enum** for classifications — a resident can be senior *and*
  indigent; combined with bitwise `|=`
- **Guarded state transitions** in `DocumentRequest` — illegal moves throw,
  and the UI catches and reports instead of crashing
- **Live fee assessment** — the fee and its legal basis update as the document
  type changes, before anything is committed
- **`TryParse`, never `Parse`**, and `KeyPress` filtering on numeric fields
- **RadioButtons scoped by GroupBox** — grouping is by *container*, not name
- **The `CheckedChanged` double-fire guard** — changing a radio raises the
  event twice; `if (sender is RadioButton { Checked: true })` filters it
- **`PrintDocument`** for real printing — framework only, no NuGet package
- **`Prompt.cs`** built entirely in code — proof the designer is a
  convenience, not a requirement

---

## Not included

Honest scope notes:

- **No database.** `BarangayRepository` is in-memory; data is lost on exit.
  It is the only class that knows where data lives, so swapping in MySQL means
  changing one class, not the forms.
- **No login or user roles.** A real deployment needs at least clerk vs.
  captain separation.
- **No photo or biometric capture** for barangay IDs.
- **Punong Barangay name is a placeholder** in `DocumentPrinter.cs`.
- **No blotter/case module** — a real clearance checks for pending cases;
  here that is asserted, not verified.

---

## Before submitting

- [ ] Replace the fee constants with the real Magugpo Poblacion ordinance rates
- [ ] Set the actual Punong Barangay name in `DocumentPrinter.cs`
- [ ] Screenshot the interface and a completed transaction
- [ ] Push to GitHub — **keep it private** until the module ends
