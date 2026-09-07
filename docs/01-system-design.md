# System Design — Barangay Resident & Document Request Management System

**Barangay Magugpo Poblacion, City of Tagum, Davao del Norte**

---

## 1. Problem

The barangay maintains a resident registry and issues documents — clearances,
certificates of residency and indigency, business clearances, barangay IDs.
Done on paper this is slow, fees get applied inconsistently, and the statutory
exemptions residents are legally entitled to are easy to miss.

This system computerises the registry, the request workflow, and — most
importantly — **the fee rules**, so exemptions are applied automatically
rather than depending on whether the clerk on duty remembers them.

---

## 2. Classes

| Class | Responsibility |
|---|---|
| `Resident` | A registry record. Name, demographics, address, classifications |
| `DocumentRequest` | One request for one document, with guarded status transitions |
| `FeeSchedule` | **All** fee rules and statutory exemptions |
| `DocumentPrinter` | Generates the printable certificate text |
| `BarangayRepository` | Data store (in-memory; swappable for MySQL) |

### Relationship

```
Resident  1 ──────── 0..*  DocumentRequest
```

A plain **association**. A resident exists independently of any request, and a
request is a historical record that outlives edits to the resident's details.
Not composition — a released clearance remains a fact even if the resident's
address later changes.

---

## 3. Why the fee logic is separate

`FeeSchedule` contains every rule about money. `MainForm` never computes a
fee — it calls `Assess(resident, documentType)` and displays what comes back,
including the **basis**.

Three payoffs:

1. **Testable without a UI.** The rules were verified by running the same
   logic outside the app.
2. **One place to change.** When Magugpo Poblacion passes a new revenue
   ordinance, one file changes.
3. **Auditable.** Every assessment returns *why*, not just *how much* — which
   matters when a resident asks.

---

## 4. Statutory exemptions

### RA 11261 — First Time Jobseekers Assistance Act

Free barangay clearance/certification, subject to:

| Condition | How the system enforces it |
|---|---|
| At least 6 months' residency | `GetMonthsOfResidency()` vs `DateOfResidency` |
| Availed once only | `HasAvailedFirstTimeJobseeker` set on release |
| Filipino citizen | Assumed for registered residents |

The check runs **twice**: when the document type is selected (disabling the
submit button with an explanation) and again on submit. Belt and braces —
a UI guard alone can be bypassed by an unexpected event order.

The printed certificate includes the **Oath of Undertaking**, because NBI,
PSA, and BIR look for it.

### Others

| Basis | Effect |
|---|---|
| RA 9994 | Senior citizen — fees waived |
| RA 10754 | PWD — fees waived |
| DILG MC 2019-177 | Indigency certificate free; >₱50 held "excessive" |
| RA 11032 | Official receipt required for every collection |

### Deliberate exclusion

**Business clearance is never waived by personal status.** It is a regulatory
fee on an enterprise. A senior citizen renewing a sari-sari store permit still
pays. This is checked before the personal exemptions in `Assess()` — order
matters.

---

## 5. Request workflow

```
Pending ──► Processing ──► ReadyForRelease ──► Released
   │             │                │
   └─────────────┴────────────────┴──────────► Rejected
```

Rules enforced in `DocumentRequest`, not the UI:

- Only a **pending** request can start processing
- Only a **processing** request can be marked ready
- Only a **ready** request can be released
- A request with an **unpaid fee cannot be released** ← the real control
- A **released** document cannot be rejected
- Rejection **requires a reason**

Illegal transitions throw `InvalidOperationException`; the form catches it and
shows the message. The app never crashes on a mis-click.

---

## 6. Validation

| Field | Rule |
|---|---|
| First / last name | Required |
| Date of birth | Not future; not >130 years ago |
| Date of residency | Not future; not before birth |
| Purok | Required |
| Contact number | Required; digits, spaces, `+`, `-` only — filtered at keystroke |
| Senior classification | Warns if the resident is under 60 |
| Purpose | Required — it is printed on the document |
| O.R. number | Required when recording payment |

Two layers throughout: `KeyPress` blocks bad characters as typed, and a
`TryParse`/explicit check runs on submit — because a `KeyPress` filter is
bypassed by pasting.

---

## 7. Known limitations

- **No persistence.** Data is lost on exit. `BarangayRepository` is the only
  class that knows where data lives, so adding MySQL means changing one class.
- **No authentication or roles.**
- **No blotter module.** A clearance asserts "no pending case" rather than
  verifying it against records.
- **Fee amounts are placeholders** pending the actual ordinance.
- **No photo capture** for barangay IDs.

---

## 8. Moving to MySQL

1. Extract `IBarangayRepository` from the current class
2. Implement `MySqlBarangayRepository` against a `residents` / `document_requests` schema
3. Change one line in `MainForm`

No form changes. That is what the layering buys.
