# Demo walkthrough and manual checks — v3.1.1

*Adapted from Jonathan F. Del Rosario's walkthrough for the Draft branch,
rewritten for this codebase: the single-project structure, the charter fee
schedule, the in-memory store, and the v3.1.1 fixes.*

Run through this list before the defence, and again whenever the layout or
the rules change. The automated checks in `tests/RuleChecks` cover the
arithmetic; this covers what a human sees.

---

## 1. Fresh checkout

1. Open **`BarangayDocumentSystem.slnx`** in Visual Studio 2022 (on 17.10–17.12
   enable the *"Use the XML solution format"* preview feature first).
2. Confirm the startup project is **BarangayDocumentSystem** (that setting is
   gitignored — every teammate sets it once after cloning).
3. Leave `Storage=Memory` in `BarangayDocumentSystem/App.config` — the demo
   needs no database.
4. Press **F5**. Then stop, and open `MainShell`, `ResidentForm`,
   `RequestForm`, `PaymentForm`, `RejectionForm` and `DocumentPreviewForm` in
   the designer — each should load without a parsing error.

## 2. First run (sample data)

The app seeds fictional people on start, chosen to exercise every branch of
the fee rules. The dashboard should match the reference preview in
[`docs/dashboard-preview.png`](dashboard-preview.png), regenerated for
v3.1.1 — it should show:

- **7 residents** across the real puroks of Magugpo Poblacion.
- **13 requests**: 8 pending, 1 processing, 1 ready for release, 3 released.
- **₱255.00 collected** (₱100 clearance + ₱155 cedula).
- 1 document issued free of charge.
- One aged request highlighted under the RA 11032 note.

Everything is in-memory: records reset when the app closes. That is
deliberate until the MySQL repository lands (see
[`docs/05-database-guide.md`](05-database-guide.md), §9, for the reviewed
persistence design).

## 3. Residents

1. Search for Maria, then clear the search.
2. Register a resident with every required field; save.
3. Edit that resident's address, save, and find the new address by search.
4. Try an empty first name, an alphabetic contact number, a birth date in the
   future, and a residency date before the birth date — each must show a
   message and save nothing.
5. Delete the new resident — it should succeed.
6. Try deleting Juan — it must be blocked, because he has request history.

## 4. Request through release

1. File a **Barangay Clearance** for Juan, purpose "Employment requirement".
   The form should show **₱100.00** (local employment, per the charter).
2. In Document requests, select the new reference, **Start processing**,
   then **Mark ready**.
3. **Release** must stay disabled while the request is unpaid — try it via
   the model if in doubt; the queue refuses an unpaid release.
4. **Record payment** with a new official receipt number (e.g. `OR-TEST-1`).
   After OK, the queue's *Paid* column must say **Yes** — this is the check
   that failed before v3.1.1, when the OK click recorded nothing.
5. **Record payment** again on a different request with the SAME receipt
   number — it must be refused with an explanation.
6. **Release**, and confirm the status becomes Released.
7. **Preview document**, then print — choose *Microsoft Print to PDF* and
   cancel; cancelling must be harmless.

A Pending request can be previewed, but printing stays disabled until the
document is finalized.

## 5. Fees and eligibility

- Certificate of Residency and Certificate of Good Moral Character for Juan:
  **₱100.00** each, with the charter basis shown and saved with the request.
- Maria's personal certificates are **free** (RA 9994).
- Maria's business clearance still costs the standard **₱200.00** — personal
  exemptions never touch regulatory amounts.
- Liza's personal certificates are **free** (RA 10754).
- Certificate of Indigency is **free**.
- Carlo (2 months' residency) is **refused** a first-time jobseeker
  certificate, with the reason shown.
- Jose has a first-time jobseeker certificate already processing. Release it,
  then try another RA 11261 claim — the second claim must be **blocked**,
  on the certificate *and* on the clearance.

## 6. Rejection (new in v3.1.1)

1. Reject a pending request — the dialog names the request and demands a
   reason; an empty reason cannot be confirmed.
2. Record a payment on a request, then reject it while it is still waiting
   for release. The dialog must warn that **the payment stays in the
   collection history — no refunds**. Confirm.
3. The dashboard collection total must be unchanged by that rejection.
4. Rejecting an already-RELEASED request is impossible — the button refuses.
5. A rejected request accepts no processing, payment, or release.

## 7. When something goes wrong

Unhandled errors are logged to
`%LOCALAPPDATA%\BarangayDocumentSystem\errors.log` and shown with a plain
explanation instead of dying silently. During the demo, that path is what
you quote if a panelist asks how failures are traced.

## 8. Automated checks

```bash
dotnet run --project tests/RuleChecks
```

The harness asserts the charter rates, the variable computations, the four
waivers and where each stops, the workflow, the aging, the printed pages —
and, since v3.1.1, the rejection rules, the payment guards, and receipt
uniqueness across requests.
