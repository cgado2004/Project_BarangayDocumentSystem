# Software Requirements — Version 3.1

**Barangay Resident and Document Request Management System**
Barangay Magugpo Poblacion, City of Tagum, Davao del Norte

*Prepared by Clint Wood Gado for IT13.

> v3.1 supersedes the v3 requirements. §VI lists every change this
> version makes, so the three documents (PDF → v3 → v3.1) can be reconciled
> rather than silently diverging.*

> This supersedes the requirements in `Documentation IT13.pdf`. Everything that
> changed is listed in §V so the two can be reconciled rather than silently
> diverging.

---

## I. Scope and Limitations

### A. Scope

A Windows desktop application (WinForms, .NET 8) for the staff of Barangay
Magugpo Poblacion. It covers:

- A resident registry — register, search, edit, delete.
- Resident classifications (senior citizen, PWD, indigent, student, solo
  parent) which determine fee exemptions.
- Filing and processing requests for **20 document types**, being the full
  list of frontline services on the barangay's own tarpaulin.
- Automatic fee assessment **using the real rates on the Barangay Citizen's
  Charter**, together with the legal basis for every amount charged or waived.
- Tracking through Pending → Processing → Ready for Release → Released, or
  Rejected, with payment recorded against an official receipt before release.
- Generation and preview of the finished document on the barangay letterhead.
- A dashboard of resident counts, request counts by status, revenue collected,
  documents issued free, and breakdowns by document type and purok.

### B. Limitations

- **No database by default.** Records live in memory and are lost on exit. A
  MySQL implementation of the same interface exists and is selected by one
  line in `Program.cs`.
- **No login, accounts, or roles.** A clerk and the punong barangay are not
  distinguished.
- **No photograph or biometric capture** for Barangay IDs.
- **No blotter or case module.** A clearance *asserts* the resident has no
  pending case; it does not verify it.
- Filipino citizenship under RA 11261 is assumed, not verified.
- Single computer. No network, multi-user, or online request feature.
- Windows only, .NET 8 runtime required.

---

## II. Functional Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | Register a resident with name, date of birth, gender, civil status, purok, address, contact number, occupation, voter status and date of residency. | High |
| FR-02 | View, search, edit and delete resident records. | High |
| FR-03 | Tag a resident with one or more classifications. | High |
| FR-04 | File a document request for a registered resident, stating the purpose. | High |
| FR-05 | Support the 20 document types listed on the barangay's frontline-services board. | High |
| FR-06 | Compute the fee automatically and display the legal basis for the amount. | High |
| FR-07 | Waive fees for senior citizens (RA 9994), PWDs (RA 10754) and indigents; issue the Certificate of Indigency and Certificate of Low Income free (Citizen's Charter, DILG MC 2019-177). | High |
| FR-08 | Exclude the Barangay Business Clearance from all personal exemptions, it being a regulatory fee on an enterprise. | Medium |
| FR-09 | Verify First-Time Jobseeker eligibility under RA 11261 — at least six months' residency and not previously availed — and block with an explanation when either fails. | High |
| FR-10 | Move a request Pending → Processing → Ready for Release → Released, allowing rejection with a required reason at any stage before release. | High |
| FR-11 | Prevent release of a fee-bearing document until payment is recorded against an official receipt number (RA 11032). | High |
| FR-12 | Reject invalid status transitions with an explanatory message rather than terminating. | Medium |
| FR-13 | Generate a printable document on the barangay letterhead with the wording proper to its type, including the RA 11261 Oath of Undertaking. | High |
| FR-14 | Preview the generated document before printing. | Medium |
| FR-15 | Display a dashboard of counts, revenue, free issuances, and breakdowns by type and purok. | Medium |
| FR-16 | Validate all input: mandatory fields, impossible dates, and a date of residency earlier than the date of birth. | High |
| **FR-17** | **Charge the Barangay Clearance at the rate matching its stated use — ₱100 for local employment, ₱200 for employment abroad — per the Citizen's Charter.** | **High** |
| **FR-18** | **Every figure on the dashboard shall be clickable and shall navigate to the filtered list it summarises.** | **Medium** |
| **FR-19** | **Show the request history of the selected resident on the Residents screen, without navigating away.** | **Medium** |
| **FR-20** | **Offer only the status actions legal for the selected request; all others shall be disabled.** | **Medium** |
| **FR-21** | **Support the four Citizen's Charter services the tarpaulin did not carry: the cedula (computed under RA 7160 Sec. 156 from sworn gross income), the Katarungang Pambarangay filing (₱150), barangay facility use (₱200 per hour or part), and other processing fees under the Barangay Taripa (assessed with the item stated).** | **High** |
| **FR-22** | **Price the Business Clearance by the law violated: the clerk assesses the amount and names the ordinance, and both travel onto the request, the receipt and the clearance. ₱200 is the standard rate when nothing was violated.** | **High** |
| **FR-23** | **Allow a qualifying first-time jobseeker to claim the RA 11261 one-time waiver on the Barangay Clearance itself, and mark the availment on release exactly as for the certificate.** | **High** |
| **FR-24** | **Print the official receipt (payment record) with the reference, amount and legal basis, from the payment dialog.** | **Medium** |
| **FR-25** | **Count working days each open request has waited and flag anything past the RA 11032 three-working-day standard for a simple transaction.** | **Medium** |
| **FR-26** | **Refuse a cedula for a person under eighteen, citing RA 7160 Sec. 156.** | **High** |

---

## III. Non-Functional Requirements

| ID | Requirement | Priority |
|---|---|---|
| NFR-01 | Usability — one sidebar with three destinations (Dashboard, Residents, Requests) so a clerk needs no training beyond a short walkthrough. | High |
| NFR-02 | Reliability — invalid operations raise handled exceptions and produce a message dialog; the application does not crash on a mis-click or malformed input. | High |
| NFR-03 | Performance — every screen action completes in under one second on a standard office workstation. | Medium |
| NFR-04 | Maintainability — all fee amounts and exemption rules reside in a single class (`FeeSchedule`), and every amount can be overridden from `App.config` without rebuilding. | High |
| **NFR-05** | **Modularity — a single `net8.0-windows` project with the layers as folders (Models, Service, Interfaces, DBContext, Helper), so the group's prescribed structure is followed while the dependency direction still points inward: views know the rules, the rules know nothing of the views.** *(revised again — see §VI)* | High |
| NFR-06 | Extensibility — adding a document type requires adding one enum value and one branch in the renderer, without modifying any view. | Medium |
| NFR-07 | Portability — data access goes through `IBarangayRepository`, so the in-memory store can be replaced with MySQL by changing one line in `Program.cs`. | Medium |
| NFR-08 | Compatibility — runs on Windows 10 or later with .NET 8. Typography resolves at startup through the Inter / SF Pro / Roboto / Segoe UI fallback stack, so layout cannot break on another machine and no font is *required* to be installed. | Medium |
| NFR-09 | Accuracy — fee computation applies statutory exemptions in a fixed order and returns both the amount and its legal basis. | High |
| NFR-10 | Auditability — every collection is recorded against an official receipt number and every released document remains on record. | Medium |
| NFR-11 | Security — a production deployment shall support clerk and punong barangay roles before handling live resident data. | Low |
| NFR-12 | Data integrity — a resident record and its released documents are independent, so later edits do not alter documents already issued. | Medium |
| **NFR-13** | **Adaptability — the window sizes itself to 92% of the available work area, capped at 1360×860 with a floor of 1000×640; every view scrolls rather than clipping; grids fill their width; dialogs are resizable with minimum sizes; DPI scaling is enabled.** | **High** |
| **NFR-14** | **The interface shall follow the "digital government service" visual language — white canvas, lavender-blue gradients, rounded cards, pill controls.** | **Low** |

---

## IV. Fee Schedule (from the Barangay Citizen's Charter)

These are the **real posted rates**, replacing the placeholder values used in
versions 1 and 2.

| Service | Fee | Note |
|---|---|---|
| Barangay Clearance | **₱100** | local employment |
| Barangay Clearance | **₱200** | for work abroad |
| Barangay Certification (residency, good moral, other purpose) | **₱100** | |
| Certificate of Indigency | **FREE** | |
| Certificate of Low Income | **FREE** | |
| Barangay Business Clearance | **VARIES** | amount assessed per the law violated; ₱200 standard. No personal exemptions. |
| Cedula (community tax) | **VARIES** | ₱5 + ₱1 per ₱1,000 sworn gross annual income, additional capped at ₱5,000 (RA 7160 Sec. 156) |
| Katarungang Pambarangay — filing a case | **₱150** | |
| Other processing fees (Barangay Taripa) | **VARIES** | clerk assesses; item stated |
| Barangay facilities (gym) | **₱200/hr** | hour or any part of an hour |
| Assistance and social-service papers | **FREE** | medical, financial, burial, 4Ps, IP, solo parent, GAD, CSO, blotter |

Statutory waivers applied on top: **RA 9994** (senior citizens), **RA 10754**
(PWDs), indigent status (**RA 11291**), and **RA 11261** (first-time
jobseekers — once only, six months' residency, covering both the certificate
and the barangay clearance). Full legal reference:
[`07-fee-schedule-and-legal-basis.md`](07-fee-schedule-and-legal-basis.md).

---

## V. Changes from the submitted PDF

Recorded plainly so the marker can see what moved and why.

1. **NFR-05 changed from three layers to two.** The PDF specifies Domain,
   Infrastructure and UI. The group's instruction was to use two folders
   because three read as an incomplete structure. `Infrastructure` has been
   merged into `Core`, which now holds entities, rules and data access. **The
   dependency rule that NFR-05 actually protects is unchanged and still
   enforced by the compiler:** `Core` targets plain `net8.0` and references
   nothing, so a WinForms type cannot be used inside it.
2. **Document types expanded from 7 to 20** — the full frontline-services
   list from the barangay tarpaulin, not just the original seven.
3. **Fees are no longer placeholders.** The PDF limitation "the fee amounts
   used are placeholder values… they must be replaced with the rates fixed by
   the barangay revenue ordinance" is now **closed**. The rates come from the
   posted Citizen's Charter.
4. **The Punong Barangay is no longer a placeholder** — HON. EUGENIA SOLIS
   HINGPIT, MD, per the City Budget Office Letter of Review of 27 November
   2024.
5. **Puroks are the real ones** (Tandang Sora, Lapu-Lapu, Sulgreg, Dagohoy,
   Orchids, Sampaguita, Paraiso, Marilag 2, Sunflower, Calachuchi, Tindalo,
   Cristo Rey, Talisay, Arellano) rather than "Purok 1…5", taken from the
   barangay's FY 2025 20% Development Fund project list.
6. **FR-17 to FR-20 and NFR-13, NFR-14 are new**, covering the two-tier
   clearance fee, clickable figures, adaptive layout and the visual language.
7. **The ₱200 business clearance rate has been confirmed** against the
   revenue ordinance and is no longer provisional.
8. **Settings moved into `App.config`.** Fees, barangay details and the choice
   of storage are read at startup, so a group-mate can correct a rate or point
   the app at their own MySQL server without touching code or rebuilding.

---

## VI. Changes in v3.1

Recorded as plainly as §V, so the marker can see what moved and why.

1. **The structure is one project.** The group's v3.1 reference structure
   prescribes a single `BarangayDocumentSystem` project (`net8.0-windows`)
   with the layers as folders — `DBContext`, `Helper`, `Interfaces`,
   `Models`, `Service` — plus the shell, the three views and the four forms
   at the project root, and a `BarangayDocumentSystem.slnx` solution. The
   `src/Core` + `src/App` split of v3 is gone; the dependency *direction*
   it protected survives, because `Service` and `Models` still reference
   nothing above them (the document renderer resolves its own fonts for
   exactly this reason).
2. **Four more document types (20 → 24):** the cedula, the Katarungang
   Pambarangay filing, barangay facility use, and the other processing
   fees under the Barangay Taripa — the rest of the charter's posted fee
   schedule, all of it priced by rule (FR-21 to FR-26).
3. **The Business Clearance now really varies.** v3 charged a flat ₱200
   with a comment; v3.1 assesses the amount against the law violated, as
   the charter actually posts it.
4. **The document text moved into templates.** `IDocumentTemplate` plus the
   template classes in `Service/Templates` own the wording of each document;
   `DocumentRenderer` is a GDI+ layout engine that draws the page, powers a
   true print preview, and prints. Adding a document is now one small class
   (NFR-06 honoured more literally than v3 managed).
5. **Receipts print.** The payment dialog produces the official-receipt
   preview with the amount and the legal basis.
6. **RA 11032 aging** appears in the request queue: working days in the
   open, with anything past three working days highlighted.
7. **The six core technical fixes** (see the README): designer support,
   PerMonitorV2 high-DPI in three agreeing places, the Inter/SF Pro/Roboto
   font stack with ClearType, percent-sized responsive grids, composited
   smooth scrolling, and WM_DPICHANGED handlers in the shell and the
   preview.
8. **Citations corrected.** DILG MC 2019-177 is the clearance-integration
   circular under RA 11032 §11(f), not the free-indigency basis; the
   indigent waiver now cites RA 11291. The full legal reference is
   [`07-fee-schedule-and-legal-basis.md`](07-fee-schedule-and-legal-basis.md).
9. **No NuGet packages at all.** App.config is read with `System.Xml.Linq`,
   so the solution restores and builds with no network.
