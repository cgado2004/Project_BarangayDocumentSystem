# System Design — Barangay Resident & Document Request Management System v3.2

**Barangay Magugpo Poblacion, City of Tagum, Davao del Norte**  
**Branches:** `master` (updated with fees & documents) + `leader_draft` (integration baseline)  
**Status:** 0 build errors, `scripts/check_structure.py` PASS, 23/23 RuleChecks documented

---

## 1. Problem

The barangay maintains a resident registry and issues documents — clearances, certificates of residency/indigency/low income, business clearances, barangay IDs, assistance & scholarship certifications, blotter, and the four Citizen's Charter financial services (cedula, lupon filing, facility rental, Taripa).

Done on paper this is slow, fees get applied inconsistently, and the statutory exemptions residents are legally entitled to are easy to miss. Business clearances posted as “amount varies depending on law violated” were charged flat, cedula computation under RA 7160 Sec.156 was manual, and RA 11261 once-only benefit had no system guard.

This system computerises the registry, the request workflow, and — most importantly — **the fee rules**, so exemptions are applied automatically rather than depending on whether the clerk on duty remembers them. All amounts live in `App.config` so a new revenue ordinance is one file edit.

---

## 2. Classes — logically arranged

### Layering
```
Views / Forms / CustomControls            what the clerk sees (Draft design + leader palette)
        │  depend only on
Interfaces  (IBarangayRepository, IDocumentTemplate, RepositoryException)
        │  implemented by                          ▲ used by
Database   (RepositoryBase → InMemory | MySql)    BusinessRules (FeeSchedule, templates, renderer)
        │  both work on                            │
Models     (Resident, DocumentRequest, RequestInput, enums, BarangayProfile)
AppSettings + DatabaseSettings + DatabaseInitializer   composition / bootstrap
```

Dependency arrow always inward. No screen names `MySqlBarangayRepository`; `Program.cs` is the only composition root.

### Class table (v3.2)

| Class | Layer | Responsibility | Origin |
|---|---|---|---|
| `Resident` | Domain | Registry record, `GetAge()`, `GetMonthsOfResidency()`, `HasClassification()`, `MiddleInitial()` fix for “.” | Fdraft base, leader fix |
| `DocumentRequest` | Domain | One request, guarded transitions, `Fee`, `FeeBasis`, `Input`, `AvailedUnderJobseekerAct`, `WorkingDaysInQueue()`, `Rehydrate()` for DB load | Draft modelled, Fdraft placed, leader rewrote v3.1 |
| `RequestInput` | Domain | Carries variable-fee context: `Scope` (Local/Abroad), `Amount`, `Hours`, `GrossAnnualIncome`, `Detail`, `ApplyJobseekerWaiver` | leader v3.1 |
| `FeeAssessment` | BusinessRules | Result of pricing: `BaseFee`, `FinalFee`, `Basis`, `IsBlocked`+`BlockReason`, `MarksJobseekerAvailment` | leader |
| `FeeSchedule` | BusinessRules | **All** fee rules, real Citizen's Charter rates, ordered assessment, `IsPersonalCertificate()`, `HasVariableFee()`, `NameOf()` | leader |
| `DisplayFormat` | BusinessRules | Single peso/date/hours formatting → no duplication | leader |
| `DocumentTemplateBase` + `GenericCertificationTemplate` | BusinessRules | Shared skeleton: `Title`, `SubtitleFor()`, `BodyLines()`, `RequiresOath`, phrase helpers `FormalName()`, `ResidentIntroduction()` | leader |
| 7 template files grouping 24 types | BusinessRules | `BarangayClearanceTemplate`, `ResidencyTemplates`, `IndigencyAndJobseekerTemplates`, `CharacterAndAssistanceTemplates`, `EmploymentAndBlotterTemplates`, `MoneyDocumentTemplates` (cedula, lupon, facility, tarifa) | leader |
| `DocumentRenderer` | BusinessRules | GDI+ layout: letterhead, rule, title, wrapped body (real font metrics), oath, signature block, reference block pinned bottom; `RenderText()` 64-char for RuleChecks | Draft had service, Fdraft shorter, leader v3.1 |
| `IBarangayRepository` | Interface | Storage contract: `Residents`, `Requests`, `StorageDescription`, `FindResident`, `SearchResidents`, `ResidentsOfPurok`, `CreateRequest(..., RequestInput)`, `GetRequestsByStatus`, `SaveRequest`, `GetStatistics()`, `Reload()` | Fdraft Reload rule + leader contract merged |
| `RepositoryBase` | Database | Template Method: shared working set, `SearchResidents` (name/sortable/purok/contact/occupation), `GetStatistics()`, `File()` (prices + TrimToSeconds + block check), `Add/Update/RemoveResident`, `CreateRequest`, `SaveRequest`; abstract hooks `InsertResident`, `UpdateResidentRow`, `DeleteResidentRow`, `InsertRequest`, `UpdateRequestRow` | leader v3.2 DRY fix |
| `MySqlBarangayRepository` | Database | Frent's persistence: parameterized SQL, per-op connection (pooled), `LAST_INSERT_ID()`, transaction in `SaveRequest` (request + jobseeker flag), `ParseEnum<T>` name-based | Fdraft, leader extended 7 cols |
| `InMemoryBarangayRepository` | Database | Demo + RuleChecks: IDs from counter, no-op hooks | leader |
| `SampleData` | Database | Single source of 7 residents (Juan full-price, Maria senior, Jose 14mo RA11261 pass, Ana solo-parent business, Pedro indigent, Liza PWD+Student hyphenated, Carlo Peña 2mo fail + DITO), 13 requests covering flat, variable, waiver, aging demo | merged seeder + in-memory seed |
| `DatabaseInitializer` | Database | Creates DB `CREATE DATABASE IF NOT EXISTS utf8mb4`, runs embedded `schema.sql` (split on `;` ignoring `--`), `EnsureColumns` adds missing v3.1 cols + widens `fee_basis` 255→500 via `information_schema` | Fdraft + leader upgrade |
| `DatabaseSettings` + `AppSettings` | Config | `AppSettings` reads `App.config` via XDocument (no ConfigurationManager), `Money()`, `Count()`, `Flag()`, `Text()` with fallbacks; `DatabaseSettings` precedence env `BARANGAY_DB_CONNECTION` > config > XAMPP default `root`/blank `CharSet=utf8mb4` | Fdraft order, leader single reader |
| `BarangayProfile` | Models | Barangay name/city/province/punong barangay/office hours from App.config, used by all templates | leader |
| `MainShell` + `MainShell.Designer.cs` + `NavigationSidebar` + `SummaryCard` | UI | Flat sidebar with seal, page title, six cards, status strip, DPI aware | Draft design, leader palette/logo |
| `ViewBase` + 3 views | UI | `DashboardView`, `ResidentsView`, `RequestsView`; `ViewBase.Persist()` single “save failed” handling → Dialog + Reload | Draft + leader |
| `ResidentForm`, `RequestForm`, `PaymentForm`, `DocumentPreviewForm`, `Prompt` | UI | Dialogs, `RequestInput` fields (scope combo, amount, hours, income, detail, jobseeker checkbox) | leader |
| `AppTheme`, `UiFactory`, `InputValidator`, `Dialog`, `CueTextBox` | UIHelpers | Theme (serif family Georgia→Times→Cambria fallback), factory (PrimaryButton, StyleGrid), validation (RequiredText, NamePart, PhoneNumber 09XXXXXXXXX, Purpose no brackets, ReceiptNumber, Money, AssessedAmount >0, Hours, dates), dialog wrappers | Draft + leader |

> **v2 note preserved.** v1 had 310-line `DocumentPrinter` switch + concrete `BarangayRepository` in form. Became `DocumentRenderer` + templates + interface. See `04-refactor-notes.md`. v3.2 further extracted `RepositoryBase` to remove duplication between MySQL and in-memory stores.

### Relationship

```
Resident  1 ──────── 0..*  DocumentRequest
```

Plain **association**. Resident exists independently; request is historical record. Not composition — released clearance remains fact even if address changes. `Resident.AddRequest()` internal, `DocumentRequest` holds `Resident` non-null.

---

## 3. Why the fee logic is separate — updated with real charter

`FeeSchedule` contains **every** rule about money. No view computes a fee — each calls `Assess(resident, type, input)` and displays what comes back, including **basis sentence** for receipt.

- **Testable without UI:** `tests/RuleChecks/Program.cs` runs same logic on in-memory store, no MySQL.
- **One place to change:** When Magugpo Poblacion passes new ordinance, edit `App.config` (numbers) or `FeeSchedule.cs` (law) — not 20 forms.
- **Auditable:** Every assessment returns *why*, not just *how much*.
- **Config-driven:** `Program.cs` reads `App.config` via `AppSettings.Money()` with InvariantCulture (prevents comma decimal locale bug), falls back to built-in defaults.

**Ordered assessment (critical, numbered in source):**
1. Free-for-all: `CertificateOfIndigency`, `CertificateOfLowIncome`, assistance/social-service (Medical, Financial, Burial, IP, 4Ps, SoloParent, GAD, CSO, Blotter) — free for everyone
2. RA 11261 first-time jobseeker: can **BLOCK** request (second availment or <6mo residency) or waive with `MarksJobseekerAvailment`
3. Variable-fee docs **before** personal exemptions (business, cedula, lupon, facility, tarifa) — regulatory/tax, not personal certs, so senior still pays
4. Personal exemptions: Senior (RA 9994), PWD (RA 10754), Indigent (RA 11291)
5. Ordinary rate

`IsPersonalCertificate()` and `HasVariableFee()` helpers make intent explicit.

### Real rates (Citizen's Charter, from `docs/07`)

| Document | Rate | Basis |
|---|---|---|
| Barangay Clearance Local | ₱100 | Charter |
| Barangay Clearance Abroad | ₱200 | Charter — scope field |
| Certification (residency, good moral, other) | ₱100 | Charter catch-all |
| Indigency, Low Income, Assistance | FREE | Charter + RA 11291 |
| Business Clearance | VARIES, standard ₱200 | RA 7160 Sec.152, clerk assesses + names violated law |
| Cedula | VARIES: ₱5 base + ₱1/₱1k income, cap ₱5k additional | RA 7160 Sec.156, e.g. ₱120k=₱125, ₱10M=₱5,005, minor BLOCKED |
| Lupon Filing | ₱150 flat | Charter + RA 7160 Sec.399-422 |
| Facility Rental | ₱200/hour, ceiling billing | Charter, 2.5h→3h=₱600, no hours BLOCKED |
| Taripa | Assessed | Charter + RA 7160 Sec.152, requires detail + amount, otherwise BLOCKED |

---

## 4. Statutory exemptions — expanded

### RA 11261 — First Time Jobseekers Assistance Act (covers clearance AND certificate)

| Condition | Enforcement |
|---|---|
| At least 6 months residency | `GetMonthsOfResidency()` vs `DateOfResidency` months calc with day adjust |
| Availed once only | `HasAvailedFirstTimeJobseeker` set on `Release()` if `DocumentType==FirstTimeJobseekerCertificate` OR `AvailedUnderJobseekerAct`; checked before filing → `IsBlocked` with `BlockReason` |
| Filipino citizen | Assumed for registered residents |
| Waiver on clearance | `RequestInput.ApplyJobseekerWaiver` checkbox → `Assess()` returns free + marks availment |

Check runs twice: UI greys button with explanation, and `RepositoryBase.File()` refuses with `InvalidOperationException` — belt and braces. Printed certificate includes **Oath of Undertaking** (NBI/PSA/BIR requirement).

### Others

| Basis | Effect | Where stops |
|---|---|---|
| RA 9994 Senior | Personal certs free | Does NOT touch business, cedula, lupon, facility, tarifa |
| RA 10754 PWD | Personal certs free | Same |
| RA 11291 Indigent + Charter | Personal certs free, indigency/low income free for all | Same |
| RA 7160 Sec.152 | Business clearance varies, Taripa assessed | Personal exemptions never apply |
| RA 7160 Sec.156 | Cedula tax, minor BLOCKED, cap | Personal exemptions never apply |
| RA 11032 | 3 working days simple transaction, `WorkingDaysBetween()` excludes weekends, `IsBeyondRA11032Standard()` highlights aging; no holiday table → conservative upper bound | — |
| DILG MC 2019-177 | Indigency >₱50 excessive → free | — |
| RA 11032 + OR rule | Payment requires traceable OR number, `RecordPayment()` needs receipt | — |

### Deliberate exclusions preserved

Business clearance never waived by personal status — regulatory fee on enterprise. Checked **before** personal exemptions in `Assess()` — order matters, documented in source comments.

---

## 5. Documents — 24 types, logically arranged

From `Models/Enums.cs` (order matters — DB stores NAME, but kept stable):

- **Core 7:** BarangayClearance, CertificateOfResidency, CertificateOfIndigency, BarangayBusinessClearance, BarangayID, FirstTimeJobseekerCertificate, CertificateOfGoodMoralCharacter
- **Tarpaulin rest:** CertificateOfLowIncome, SoloParentCertification, MedicalAssistanceCertification, FinancialAssistanceCertification, BurialAssistanceCertification, IpScholarshipCertification, FourPsScholarshipCertification, EmploymentCertification, AcceptanceCertificate, GadRelatedDocumentation, BlotterRelatedIncident, CsoDocumentation, OtherCertification
- **v3.1 Charter additions:** CommunityTaxCertificate (cedula with computation printed), LuponCaseFiling, BarangayFacilityRental, OtherTarifaProcessingFee

Templates grouped by concern:
- `BarangayClearanceTemplate` — scope subtitle “For Local/Abroad Employment”
- `ResidencyTemplates` — residency + generic
- `IndigencyAndJobseekerTemplates` — indigency/low income, jobseeker with oath
- `CharacterAndAssistanceTemplates` — good moral, solo parent, medical/financial/burial, IP/4Ps
- `EmploymentAndBlotterTemplates` — employment, acceptance, blotter
- `MoneyDocumentTemplates` — cedula (prints income + breakdown), lupon, facility (hours), tarifa
- `DocumentTemplateBase` — `FormalName()` uppercase, `ResidentIntroduction()` “41 years of age, bona fide resident of Purok…”, `Noun` override

Renderer separation:
- `Draw(Graphics, bounds, request)` — measures before drawing, cursor only forward, DPI independent, letterhead (Republic, Province, City, Barangay uppercase bold, Office of Punong Barangay), rule, title uppercase, subtitle italic, body wrapped word-by-word preserving indent, oath with rule, signature block (line + Punong Barangay name bold + title), reference block pinned above bottom edge with issued date, reference, fee+basis, OR line, status.
- `CreatePrintDocument()` — Letter, margins 75/60, preview and print share it.
- `RenderText()` — 64-char plain text, centre helpers, same wrapping, used by RuleChecks to prove letterhead and no `[SET` placeholders.

Fallback: `GenericCertificationTemplate` for any unlisted type → never blank page.

---

## 6. Request workflow — with RequestInput

```
Pending ──► Processing ──► ReadyForRelease ──► Released
   │             │                │
   └─────────────┴────────────────┴──────────► Rejected
```

Rules enforced in `DocumentRequest`, not UI (same as v2):

- Only pending → processing
- Only processing → ready
- Only ready → released
- **Unpaid fee cannot be released** ← real control, throws with peso amount
- Released cannot be rejected
- Rejection requires reason
- `RecordPayment()` needs OR number, checks already paid, fee>0
- `GetReferenceNumber()` → `BMP-yyyy-dddd` built from year+id, never stored

**New in v3.1/v3.2:**
- `RequestInput` carried through `CreateRequest` → persisted in 7 columns (`scope`, `assessed_amount`, `hours`, `gross_annual_income`, `detail`, `apply_jobseeker_waiver`, `availed_under_jobseeker_act`) → reloaded via `ReadRequest` → printed same computation it was filed with (fee frozen, schedule may change next year)
- `File(resident, type, purpose, input, filedOn)` internal — `filedOn` allows backdated request for RA11032 aging demo (6 days ago), trimmed to seconds to match MySQL DATETIME precision → queue order stable after reload
- `ApplyAssessment()` writes fee+basis+availed flag atomically, called only by repository

---

## 7. Validation — expanded

| Field | Rule | Where |
|---|---|---|
| First/last name | Required, 60 chars, at least one letter, only letters/space/hyphen/dot/apostrophe, Unicode letters (Peña) | `InputValidator.NamePart()` |
| Middle name | Optional, same chars, `MiddleInitial()` finds first letter → “ P.”, punctuation never reaches doc (bug “Juan .. Dela Cruz” fixed) | Resident |
| Date of birth | Not future, not >130y, plausible | InputValidator |
| Date of residency | Not future, not before birth, used for months calc | InputValidator + Resident |
| Purok | Required, must be one of real puroks list `Puroks.All` (14 real names from 20% Development Fund FY2025), case-insensitive check | Enums + Validator |
| Contact | Optional, Philippine mobile 09XXXXXXXXX or landline with area code, strips non-digits, handles +63 | InputValidator |
| Senior classification | Warns if under 60, flag bit 1 | Validator |
| Purpose | Required, 160 chars, printed on cert, no `[` `]` placeholders | InputValidator |
| O.R. number | Required when paid, 3-40 chars, letters/digits/dash/space/slash only | InputValidator |
| Assessed amount | >0 (0 is mistake, free by rule not by typing 0), max 100k | InputValidator |
| Hours | >0, max 720 | InputValidator |
| Gross income | >=0, used for cedula | FeeSchedule |
| Detail | Required for business (law violated) and tarifa (Taripa item) | FeeSchedule blocks if missing |

Two layers: `KeyPress` filter + submit `TryParse` — paste bypasses filter.

---

## 8. MySQL — done, upgraded, logically arranged

**Storage is `Database/MySqlBarangayRepository.cs`, selected in `Program.cs`.** See `docs/05-database-guide.md` and `docs/08-integration-notes.md`.

- **Setup:** Start MySQL (XAMPP), F5. No manual SQL. `DatabaseInitializer.EnsureCreated()` creates DB `barangay_db` utf8mb4 if missing, runs embedded `schema.sql` (one copy, embedded resource), upgrades old DB via `EnsureColumns` (checks `information_schema.COLUMNS`, `ALTER TABLE ADD COLUMN` missing, `MODIFY fee_basis VARCHAR(500)` if 255).
- **Schema:** `residents` (resident_id AUTO_INCREMENT, names, dob, gender, civil_status, purok, address, contact, occupation, date_of_residency, is_registered_voter, classification INT flags 1 Senior/2 PWD/4 Indigent/8 Student/16 SoloParent, has_availed_jobseeker), `document_requests` (request_id, resident_id FK CASCADE, document_type VARCHAR(50) NAME not number, purpose, date_requested DATETIME, date_released, status NAME, fee DECIMAL(10,2), fee_basis VARCHAR(500) frozen, is_paid, official_receipt_no, remarks, scope VARCHAR(10) default Local, assessed_amount, hours, gross_annual_income, detail, apply_jobseeker_waiver, availed_under_jobseeker_act).
- **Connector:** `MySql.Data 8.4.0` PackageReference, `MySqlConnectionStringBuilder` for description “MySQL — localhost/barangay_db”, `Run<T>` opens per op, pooled, `Describe()` maps 1042/2002/2003/2013 → “start MySQL”, 1044/1045 → “check user/pass”, 1049 → “DB not exist”.
- **Queries:** `ResidentColumns` const, `SqlSelectResidents` ORDER BY id, `SqlSelectRequests` ORDER BY id, `BindResident` shared by INSERT/UPDATE, `LastInsertId()` SELECT LAST_INSERT_ID() on same connection.
- **Transaction:** `SaveRequest` → `BEGIN`, UPDATE document_requests (purpose,status,date_released,is_paid,or,remarks) + UPDATE residents has_availed_jobseeker, COMMIT — both or neither, prevents crash releasing cert but forgetting once-only flag.
- **Reload:** Discards memory, re-reads both tables, `ReplaceWorkingSet` only after both succeed → failed reload leaves old data, not empty screen. `ViewBase.Persist()` catches `RepositoryException`, Dialog, Reload.
- **No stored procedures:** Parameterized SQL prevents injection (O'Brien safe). Could add `sp_GetStatistics` but keeps rules in C# for testability.
- **Config:** `DatabaseSettings.Load()` env var `BARANGAY_DB_CONNECTION` > App.config `BarangayDb` > default XAMPP root blank, `CharSet=utf8mb4` for Peña. `SeedSampleData` flag — only when empty.

---

## 9. Known limitations — updated

- **Single user:** Cached in memory, two copies don't see each other's changes until reload (Fdraft limit, accepted).
- **No authentication/roles:** Clerk vs punong barangay not distinguished.
- **No blotter verification:** Clearance asserts “no pending case” rather than checking records.
- **Fee amounts configurable but charter-based:** Real rates now, but ordinance may change → edit `App.config`.
- **No photo/biometric for Barangay ID.**
- **RA11032 working days excludes weekends only, not PH holidays** — needs holiday table for precise count, currently conservative upper bound.
- **No stored procedures** — could be added if DB admin requires.
- **One DB per app:** No multi-tenancy.

---

## 10. Verification — v3.2

- `python scripts/check_structure.py` → PASS: 48 source files, Framework v4.8, layout, XML parse, assets (logo), embedded schema, no committed password (blank), schema/repository column agreement, attribution headers (PART/ORIGIN/EDITS/VOICE), compatibility scan (no `System.Configuration`, no generic `Enum.Parse<T>`, no `Math.Clamp`, etc.)
- `git diff --check` → whitespace clean.
- `tests/RuleChecks` → 23 checks: 7 residents seeded, real puroks, Peña ñ preserved, flat rates (100/200), variable fees (business 500, cedula 125/5/5005 cap, minor blocked, lupon 150, facility 2.5h=600, tarifa blocked), exemptions stop at regulatory, RA11261 14mo pass 2mo blocked + waiver on clearance + once-only block after release, workflow unpaid refused, reference format, RA11032 aging 4 weekdays, weekend not counted, 6 days aging flagged, all 24 types have name+template+render on real letterhead with Punong Barangay, jobseeker oath, cedula computation prints.
- Build: Both projects compile 0 errors on Windows (MSBuild). Linux editing env cannot run MSBuild/NuGet, so UI behavior, printing, first MySQL run not verified here — see `docs/08` §6 checklist.

---

## 11. Logical arrangement — folder map

```
BarangayDocumentSystem.sln
BarangayDocumentSystem/
  Program.cs                 composition root, AppSettings → FeeSchedule → DatabaseSettings → Initializer → Repository → MainShell
  AppSettings.cs             single reader of App.config (XDocument)
  MainShell.cs/.Designer.cs/.resx  navigation + status strip
  Assets/barangay-logo.png
  BusinessRules/
    FeeSchedule.cs           real charter rates, ordered assessment, blocking, variable fees
    DisplayFormat.cs         Peso(), LongDate(), Hours(), PesoOrFree()
    DocumentRenderer.cs      GDI+ + plain text
    DocumentTemplates/       7 files grouping 24 types, DocumentTemplateBase
  Database/
    RepositoryBase.cs        Template Method, shared queries/stats/File()
    MySqlBarangayRepository.cs  parameterized SQL, transaction, Reload
    InMemoryBarangayRepository.cs  counter IDs
    SampleData.cs            single source seed
    DatabaseInitializer.cs   EnsureCreated + EnsureColumns upgrade
    DatabaseSettings.cs      env var > config > default
    schema.sql               embedded, one schema
  Models/
    Resident.cs              age, months residency, flags, middle initial fix
    DocumentRequest.cs       guarded transitions, RequestInput, Rehydrate, RA11032 aging
    Enums.cs                 DocumentType 24, ClearanceScope Local/Abroad, RequestStatus, CivilStatus, Gender, ResidentClassification Flags, Puroks real list
    BarangayProfile.cs       current barangay details from config
  Interfaces/
    IBarangayRepository.cs   storage contract + ResidentDetails + BarangayStatistics
    IDocumentTemplate.cs     Title, SubtitleFor, BodyLines, RequiresOath, OathLines
    RepositoryException.cs   clerk-readable
  Forms/                     dialogs (RequestForm with scope/amount/hours/income/detail/waiver)
  Views/                     Dashboard (6 SummaryCards), Residents, Requests, ViewBase.Persist
  CustomControls/            NavigationSidebar, SummaryCard
  UIHelpers/                 AppTheme (serif fallback Georgia/Times/Cambria), UiFactory, InputValidator, Dialog, CueTextBox, CompilerShims
  App.config                 Storage=MySQL, SeedSampleData, Barangay details, 11 fee keys, 2 rule keys, BarangayDb connection
  app.manifest               DPI PerMonitorV2
docs/
  01-system-design.md        this file (updated, preserved per task)
  01-requirements.md         Docu v7 requirements
  02-erd.svg / 03-uml.svg    redrawn from Docu v7
  04-project-timeline.md     weekly log, status update Sept 28
  04-refactor-notes.md       preserved DRY/SOLID audit (v2)
  05-database-guide.md       setup: start MySQL, F5, no manual SQL
  06-pushing-to-github.md    push guide
  07-fee-schedule-and-legal-basis.md  charter rates + RA 9994/10754/11291/11261/7160/11032
  08-integration-notes.md    how drafts merged, DRY table, SOLID one-liners, bugs fixed
  09-object-model.md         class diagram, ERD, file-to-release, SOLID/DRY map
tests/RuleChecks/            regression harness, in-memory, no MySQL needed
scripts/check_structure.py   structural CI
push.sh
```

All source files carry `PART / ORIGIN / EDITS / VOICE` header attributing branch/teammate.

---

## 12. What changed for master update (fees + documents)

- **Fees:** `FeeSchedule.cs` placeholder 50→100/200 real charter, `DisplayFormat.cs` added, `Enums.cs` 7→24 types + `ClearanceScope` + `Puroks` real list, `DocumentRequest.cs` + `RequestInput`, `schema.sql` +7 cols + fee_basis 500, `MySqlBarangayRepository` + `RepositoryBase` + `InMemory`, `AppSettings.cs` + `BarangayProfile.cs`, `App.config` 11 fee keys, `Program.cs` composition reads config.
- **Documents:** `DocumentRenderer` 494 lines with wrapping + reference block, `DocumentTemplateBase` + 6 grouped template files covering 24 types + generic fallback, `IDocumentTemplate` updated, forms support scope/amount/hours/income/detail/waiver, `MainShell` DPI aware.
- **Preserved:** `docs/01-system-design.md` (this updated version) and `docs/04-refactor-notes.md` (original kept, not deleted).
- **Logically arranged:** Single project, folder per concern, no duplicate schemas/seeds/config readers, embedded schema, `check_structure.py` PASS.

