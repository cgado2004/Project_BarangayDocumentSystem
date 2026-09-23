# Fee Schedule and Legal Basis — v3.1

**Barangay Resident and Document Request Management System**
Barangay Magugpo Poblacion, City of Tagum, Davao del Norte

This is the working legal reference behind every peso the system charges or
waives. Each fee in the Citizen's Charter is listed with the laws and
issuances behind it, so a clerk can answer "why am I being charged this?"
from the software rather than from memory.

---

## I. The Citizen's Charter fee schedule

| Service | Fee the system charges | Where it lives in the code |
|---|---|---|
| Barangay Clearance — local employment | ₱100.00 | `FeeSchedule` (1) |
| Barangay Clearance — work abroad | ₱200.00 | `FeeSchedule` (1) |
| Certification (residency, good moral, other) | ₱100.00 | `FeeSchedule` (2) |
| Other processing fee under the Barangay Taripa | assessed per Taripa item | `FeeSchedule.AssessTarifa` |
| Certificate of Indigency | FREE | `FeeSchedule` (1) |
| Certificate of Low Income | FREE | `FeeSchedule` (1) |
| Business Clearance | **VARIES** with the law violated; ₱200 standard | `FeeSchedule.AssessBusiness` |
| Cedula (community tax) | **VARIES** — computed from sworn income | `FeeSchedule.AssessCommunityTax` |
| Filing a case (Katarungang Pambarangay) | ₱150.00 | `FeeSchedule.AssessLuponFiling` |
| Barangay facilities | ₱200.00 per hour or part | `FeeSchedule.AssessFacility` |

(1) Every rate is read from `App.config` at startup, so a corrected
ordinance figure is an edit and a restart, not a rebuild.
(2) Anything the charter does not price separately is charged the ₱100
certification rate, which is the charter's own catch-all.

### The cedula computation (RA 7160, Sec. 156)

For an individual, the community tax is a **basic ₱5.00** plus an additional
**₱1.00 for every ₱1,000 of gross annual income** from the preceding year
(business, profession, or property), the additional portion **capped at
₱5,000**. The computation runs off the declarant's own sworn statement,
and the certificate prints the whole computation on its face. Only persons
**18 or over** may pay it; the system refuses a cedula for a minor.

Examples the demo shows: ₱0 income → ₱5. ₱120,000 → ₱5 + ₱120 = ₱125.
₱10,000,000 → capped at ₱5 + ₱5,000 = ₱5,005.

### The business clearance (RA 7160, Sec. 152)

The charter posts this as *"amount varies depending on the law violated"*.
The clerk assesses the amount and names the law or ordinance violated (the
request form offers the common ones); when nothing was violated, the
standard ₱200 rate is charged. The violated law is printed on the
clearance and on the receipt. Personal exemptions never apply to it.

---

## II. The waivers, and exactly where each stops

| # | Who | What they get | Enforced by |
|---|---|---|---|
| 1 | Senior citizens | Personal certificates **free** | RA 9994 (Expanded Senior Citizens Act of 2010) |
| 2 | Persons with disability | Personal certificates **free** | RA 10754 (An Act Expanding Benefits for PWDs) |
| 3 | Indigent residents | Personal certificates **free** | RA 11291 (Magna Carta of the Poor) + the charter |
| 4 | First-time jobseekers | Barangay certification **and** barangay clearance **free, ONCE** | RA 11261 (First Time Jobseekers Assistance Act) |

**What "personal certificates" means.** The waivers cover the clearance and
the certifications issued to a person about themselves. They do **not** touch:

- the **business clearance** — a regulatory fee on an enterprise, not on a
  person (a senior citizen's sari-sari store pays like anyone else's);
- the **cedula** — a tax, whose basic ₱5 everyone 18 and over owes;
- the **lupon filing fee** and **facility rental** — user and regulatory
  charges, not certificates;
- **Taripa items** — assessed processing fees.

The fee rules apply these in a fixed order (free-for-all documents first,
RA 11261 next, then the variable-fee documents, then the personal
exemptions), and the order is numbered in the source so it cannot be
quietly rearranged.

### RA 11261 in detail

- Requires **Filipino citizenship**, first-time jobseeker status, and at
  least **six months' residency** in the issuing barangay (the system reads
  the residency from the resident record and refuses the document with a
  reason if it falls short).
- May be availed **only once**. The system flags the resident when either
  the First-Time Jobseeker Certificate **or** a barangay clearance issued
  under the waiver is released, and refuses any later claim.
- Requires the requester to execute an **Oath of Undertaking** before the
  Punong Barangay — the system prints it on the same page.
- The barangay must submit a **monthly report to the City/Municipal PESO**
  (IRR, Sec. 7). *Not automated — see the limitations in
  01-requirements.md.*
- Implementing rules: **Joint Memorandum Circular No. 001, s. 2019**.

### A note on the senior/PWD waiver's basis

RA 9994 and RA 10754 enumerate discounts, VAT exemptions and express lanes
for goods and services; neither says "barangay certificates are free" in so
many words. The free issuance is the barangay's **own Citizen's Charter
policy**, adopted in keeping with those laws, and this system implements it
because the charter posts it. The indigent waiver, by contrast, rests
directly on RA 11291, and the indigency certificate itself is free to
everybody under the charter.

---

## III. Where the barangay's authority to charge comes from

- **RA 7160 (Local Government Code of 1991)**
  - **Sec. 152** — barangays may levy reasonable fees or charges for
    services rendered, including regulation and the use of barangay
    property and facilities. This is the authority behind the clearance,
    facility-rental and Taripa fees.
  - **Sec. 156** — the community tax: rates, the ₱5,000 cap, and the
    18-and-over liability. (Secs. 156–164 cover the whole cedula: place and
    time of payment, issuance, presentation, penalties.)
  - **Sec. 389** — the Punong Barangay issues the certifications and
    clearances of the barangay.
  - **Secs. 399–422** — the **Katarungang Pambarangay**, the barangay
    justice system the ₱150 filing fee belongs to. (Note: the KP framework
    itself is meant to be inexpensive and accessible; some barangays charge
    only token fees. This barangay's charter prices the filing at ₱150, and
    the system charges what the charter posts.)
- **The barangay's own revenue ordinance / Taripa.** A barangay may collect
  only what its ordinance allows; the charter and Taripa posted at the hall
  are those ordinances in public form. The system's `App.config` is the
  single place a corrected rate is entered.
- **RA 11032 (Ease of Doing Business and Efficient Government Service
  Delivery Act of 2018)**
  - Prescribes **3 working days** for simple frontline transactions, **7**
    for complex, **20** for highly technical ones. A barangay document is a
    simple transaction; the request queue counts working days in the open
    and flags anything past the 3-day standard.
  - Requires every LGU to post a **Citizen's Charter** — which is where
    this fee schedule itself comes from.
  - **Sec. 11(f)**: barangay clearances related to doing business are
    applied for, issued and collected at the city/municipality, with the
    barangay's share remitted to it — implemented by
    **DILG MC 2019-177**. *(v3 mis-cited this circular as the basis for the
    free Certificate of Indigency; it is not, and v3.1 corrects the
    citation.)*

---

## IV. Other laws the barangay works under, discovered for this version

| Law | What it has to do with us |
|---|---|
| **RA 11291** (Magna Carta of the Poor) | Fee relief for indigent residents; the anchor of waiver #3. |
| **RA 11861** (Expanded Solo Parents Welfare Act) | The solo parent certification references it; the barangay certifies facts it knows first-hand, the LGU social-welfare office evaluates the benefit. Issued free as social-service documentation. |
| **RA 8371** (IPRA) | Context for the IP scholarship certification; recognizes ICC/IP rights. |
| **RA 11310** (4Ps Act) | Context for the 4Ps scholarship certification. |
| **RA 9262** (Anti-VAWC Act) | Barangay Protection Orders are issued **free of charge**. *Outside this system's scope — noted so nobody reads the Taripa as applying to BPOs.* |
| **RA 11032 / ARTA advisories** | Refusing a service without proper grounds is a violation; the rejection path always demands and records a reason. |
| **RA 7160 Sec. 391–393** | The barangay secretary's records (the blotter) are the basis of the blotter-related certification; the certificate says it certifies the ENTRY, not the truth of the report. |

---

## V. What this means in the code

- `Service/FeeSchedule.cs` is the only place a peso amount is decided, and
  every assessment returns both the amount **and its legal basis** as one
  object — the basis is printed on the certificate, the receipt and the
  queue, so no charge ever appears without its reason.
- `BarangayDocumentSystem/App.config` holds every rate, so a corrected
  ordinance figure never needs a rebuild.
- `tests/RuleChecks` asserts the behaviour above against the law: the flat
  rates, the variable computations, where each waiver stops, the RA 11261
  once-only rule on both documents, and the RA 11032 aging.

---

## VI. Review note — v3.1.1 (the fee question the Draft branch raised)

Jonathan F. Del Rosario's `Draft` branch priced the Barangay Clearance at a
flat **₱50** and documented it, honestly, as a *"classroom placeholder;
purpose-based Charter fees pending"*. During the v3.1.1 review he also
supplied an independent re-check of the Citizen's Charter photo, which
**confirms** this document's rates:

- Residency and good-moral certifications at **₱100** — confirmed against
  the posted charter.
- The Barangay Clearance at **₱100** for local employment and **₱200** for
  work abroad — confirmed, and the reason the ₱50 flat placeholder was
  rejected: it undercharges the charter's local rate by half and has no
  scope distinction at all.
- His branch's flat ₱200 business clearance (exemptions "do not apply") was
  likewise superseded: ₱200 is our **standard** rate, but the amount
  **varies with the law violated** and is assessed at the counter.

No peso values changed in v3.1.1 — the charter schedule above stands, and
the review record exists so nobody has to re-litigate the ₱50 question
from memory.

*Sources: the Barangay Citizen's Charter posted at the hall; RA 7160; RA
9994; RA 10754; RA 11032; RA 11261 and its IRR (JMC 001 s. 2019); RA 11291;
RA 11861; RA 8371; RA 11310; DILG MC 2019-177; the City Budget Office
Letter of Review of 27 November 2024 (the Punong Barangay's name); the
barangay's FY 2025 20% Development Fund project list (the purok names).*
