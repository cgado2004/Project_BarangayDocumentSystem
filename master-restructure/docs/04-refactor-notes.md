# Refactor Notes — DRY, SOLID, and the UI Overhaul

**Project:** Barangay Resident and Document Request Management System
**Baseline:** commit `ec27edf` (v1, single project)
**Scope agreed:** 3-project split · system fonts only · DRY focus · sidebar navigation

> **One correction to the brief.** The 3-project split you chose is not a DRY
> change — it is **Dependency Inversion** and **Single Responsibility**,
> enforced by the compiler. The notes lead with DRY as requested, but labelling
> that work "DRY" would be wrong, so Part 2 names it correctly.

---

# Part 1 — The audit that started this

Before changing anything, the v1 code was measured:

| Metric | v1 | Meaning |
|---|---:|---|
| Interfaces defined | **0** | Nothing could be swapped or faked |
| `MessageBox.Show` calls | **18** | Same four arguments retyped |
| "Please select … first" blocks | **7** | Near-identical, already drifted |
| `MainForm.cs` | **429 lines** | Four responsibilities in one file |
| `DocumentPrinter.cs` | **310 lines** | `switch` over every document type |
| `new BarangayRepository()` in UI | **yes** | UI welded to one storage class |

Those six numbers drove every change below.

---

# Part 2 — DRY: what was duplicated, and what replaced it

## 2.1 Message boxes — 18 calls → 6 methods

**Before**, scattered across five forms:

```csharp
MessageBox.Show("Please select a resident first.", "No selection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
// …six more, with inconsistent captions
```

**After** — `UI/Common/Dialog.cs`:

```csharp
Dialog.SelectFirst("resident");
Dialog.Warn("Contact number is required.");
if (Dialog.ConfirmDestructive("Delete this record?")) { … }
```

**Verified:** `grep MessageBox.Show` outside `Dialog.cs` → **0 real calls**
(one match remains, inside a comment showing the old pattern).

**Why it matters beyond line count:** captions and icons can no longer
disagree, and restyling every dialog is one edit.

## 2.2 Field validation — ~60 lines → a readable chain

**Before**, repeated per field in `ResidentForm`:

```csharp
if (string.IsNullOrWhiteSpace(txtFirstName.Text))
{
    MessageBox.Show("First name is required.", "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
    txtFirstName.Focus();
    txtFirstName.SelectAll();
    return false;
}
```

**After** — the whole method:

```csharp
private bool IsValid() =>
    InputValidator.Required(txtFirstName, "First name")
    && InputValidator.Required(txtLastName, "Last name")
    && InputValidator.PlausibleBirthDate(dtpBirth)
    && InputValidator.NotFuture(dtpResidency, "Date of residency")
    && InputValidator.NotBefore(dtpResidency, dtpBirth, "Date of residency", "the date of birth")
    && InputValidator.RequiredSelection(cmbPurok, "purok")
    && InputValidator.ContactNumber(txtContact)
    && SeniorAgeIsConsistent();
```

`&&` short-circuits, so the first failure stops the chain — same behaviour as
the early `return false`, but the rules are now readable at a glance.

> `SeniorAgeIsConsistent()` stayed local on purpose. It is barangay-specific
> and would not generalise; forcing it into `InputValidator` would be
> over-abstraction, which is the opposite failure to duplication.

## 2.3 Fourteen parameters → one record

**Before:**

```csharp
public Resident AddResident(string firstName, string middleName, string lastName,
    string suffix, DateTime dob, Gender gender, CivilStatus civilStatus,
    string purok, string addressLine, string contactNumber, string occupation,
    DateTime dateOfResidency, bool isVoter, ResidentClassification classification)
```

…and `MainForm` then repeated all fourteen assignments **again** for editing.
Two lists to keep in sync by memory.

**After:** one `ResidentDetails` record. `Add` and `Update` both route through
a single private `Apply()`. Adding a field updates one record and one method —
call sites are untouched.

## 2.4 Document text — the biggest win

`DocumentPrinter` (310 lines) repeated the letterhead, closing paragraph,
signature block, reference footer and dry-seal notice across **seven**
`Append*` methods. They had already drifted apart.

Now `DocumentRenderer` writes that scaffolding **once**; each template supplies
only its body. Seven templates average ~25 lines each.

## 2.5 Design tokens

`Color.FromArgb(21, 71, 52)` appeared as a literal in six Designer files.
Fonts were retyped on nearly every control. Now `AppTheme` holds every colour,
font and spacing value — rebranding is one file.

## 2.6 Control construction

A styled button took six property assignments, repeated ~40 times.
`UiFactory.PrimaryButton("Release", 140)` replaces each.
`UiFactory.StyleGrid(grid)` replaces twelve assignments per grid, ×4 grids.

## 2.7 Status transitions built from a table

Three near-identical handlers became:

```csharp
var transitions = new (string Text, Action<DocumentRequest> Action, string Verb)[]
{
    ("Start Processing", r => r.StartProcessing(),     "moved to processing"),
    ("Mark Ready",       r => r.MarkReadyForRelease(), "marked ready for release"),
    ("Release",          r => r.Release(),             "released")
};

foreach (var (text, action, verb) in transitions) { … }
```

---

# Part 3 — SOLID (what the 3-project split actually demonstrates)

## 3.1 Dependency Inversion — enforced by the compiler

```
┌──────────────────────────────────┐
│  UI            net8.0-windows    │  WinForms lives ONLY here
│  MainShell, Views, Forms, Theme  │
└───────────┬──────────────┬───────┘
            │              │
            ▼              ▼
┌────────────────────┐  ┌──────────────────────────┐
│ Infrastructure     │  │ Domain      net8.0       │
│ InMemoryRepository ├─►│ Entities, Abstractions,  │
└────────────────────┘  │ Services, Templates      │
                        └──────────────────────────┘
                          references NOTHING
```

**Domain targets `net8.0`, not `net8.0-windows`, and references no projects.**
Using a WinForms type there is now a **compile error**, not a code-review note.

**Verified:**
```
Domain references to WinForms/Drawing ....... 0
Infrastructure references to WinForms ....... 0
UI files naming InMemoryBarangayRepository .. 1  (Program.cs only)
```

The concrete store is named in exactly one place:

```csharp
IBarangayRepository repository = new InMemoryBarangayRepository(feeSchedule);
// MySQL becomes: new MySqlBarangayRepository(connectionString, feeSchedule);
```

## 3.2 Single Responsibility

| v1 | Responsibilities | v2 | Responsibility |
|---|---|---|---|
| `MainForm` (429) | residents, requests, dashboard, chrome | `MainShell` | chrome + view switching |
| | | `ResidentsView` | residents |
| | | `RequestsView` | requests |
| | | `DashboardView` | statistics |
| `DocumentPrinter` (310) | 7 documents + layout | `DocumentRenderer` | layout |
| | | 7 templates | one document each |

## 3.3 Open/Closed

**Adding an eighth document, v1:** edit `DocumentPrinter` — add a `switch`
case, add a private method. Reopening tested code; the seven working documents
are all at risk.

**v2:** add `BusinessPermitTemplate.cs`, add one line in `Program.cs`.
`DocumentRenderer` is never opened. It contains no `switch` and knows no
document type by name.

## 3.4 Liskov Substitution

`MainShell` stores views as `ViewBase` and calls `Title`, `Subtitle`,
`RefreshData()`. Any subclass substitutes cleanly because all three honour the
same contract. The shell has no idea which concrete view it holds.

## 3.5 Interface Segregation — and an honest limit

`IDocumentTemplate` is deliberately tiny: `DocumentType`, `Title`, `Subtitle`,
`BuildBody`. No template is forced to implement anything it does not use —
`Subtitle` has a default of `null`.

**However:** `IBarangayRepository` has **nine members** covering residents,
requests and statistics. A strict reading of ISP would split it into
`IResidentStore`, `IRequestStore` and `IStatisticsProvider`. It was kept whole
because at this size three interfaces would add indirection without removing
any real coupling. **This is a judgement call, and a marker could reasonably
push back on it.** The reasoning is more useful than a false claim of purity.

---

# Part 4 — UI overhaul

## 4.1 TabControl → sidebar shell

| | v1 | v2 |
|---|---|---|
| Navigation | 3 tabs | Left sidebar, 232 px |
| Dashboard | monospaced text blob in a TextBox | 10 stat cards + 2 breakdown lists |
| Status colour | plain text | colour-coded per status |
| Grid style | WinForms default | themed headers, alternating rows, 34 px rows |
| Request filter | 5 RadioButtons | one ComboBox |
| Buttons | default grey | flat, hover states, primary/secondary/danger |

Replacing the radio filter with a ComboBox also removed the need for the
`CheckedChanged` double-fire guard those required — a class of bug deleted
rather than worked around.

## 4.2 Fonts — the trap you avoided

You chose **system fonts only**, which is right, and worth stating in the
submission because the failure mode is silent:

> A font that is not installed does **not** raise an error. Windows substitutes
> a fallback with different metrics, so text overflows its labels and the
> layout quietly breaks — *on the grader's machine, not yours.*

`AppTheme` uses only **Segoe UI** (Windows UI font since Vista) and
**Consolas** (monospace, since Vista). Zero risk.

If a custom font were ever wanted, the safe route is embedding it as a resource
and loading via `PrivateFontCollection` — never requiring an install.

## 4.3 Icons without asset files

Sidebar glyphs are Unicode characters (`▦ ● ▤`), so the app ships with no
image files and nothing to break on a path change.

---

# Part 5 — Problems encountered

In the spirit of the `LogicBuilding101` reference — the things that actually
bit during this refactor.

### 5.1 Docked controls stack in reverse

`Controls.Add` with `DockStyle.Top` puts each new control **above** the last,
so the sidebar came out upside-down. Fixed with `BringToFront()` after each
add. Related: in `MainShell`, the `Fill` panel must be added **before** the
docked edges, or it covers them.

### 5.2 `Controls.Clear()` leaks handles

Rebuilding the dashboard on every refresh left the old cards' window handles
allocated. `Clear()` removes references but does not `Dispose()`. Fixed with an
explicit `DisposeChildren` helper — and it must iterate over `.ToList()`,
because mutating `Controls` while enumerating it throws.

### 5.3 Regex-based refactoring is not safe

Bulk-replacing `MessageBox.Show(...)` with `Dialog.Warn(...)` initially matched
a call **inside a doc comment**, which would have corrupted the explanation of
the old pattern. The verification step caught it. Lesson: after an automated
edit, re-grep and read what matched.

### 5.4 Pre-formatted text must not be re-wrapped

`DocumentRenderer` word-wraps paragraphs to 72 columns — which destroyed the
barangay ID card's `Label : value` alignment. Added an `IsPreformatted` check
so aligned rows pass through untouched.

### 5.5 Namespace migration by `sed` needs verification

Moving three namespaces across ~25 files with `sed` looked fine, but internal
qualified references (`BarangayDocumentSystem.Models.X`) needed a second pass.
Confirmed with a namespace census showing all ten namespaces consistent.

---

# Part 6 — Verification

Run against the finished code:

```
Domain → WinForms/Drawing references ........ 0        CLEAN
Infrastructure → WinForms references ........ 0        CLEAN
UI files naming the concrete repository ..... 1        Program.cs only
Views depending on IBarangayRepository ...... 5        via interface
Raw MessageBox.Show outside Dialog.cs ....... 0        was 18
Interfaces defined .......................... 2        was 0
Brace balance, all files .................... OK
Designer ↔ handler pairing, all 4 forms ..... ALL PASS
Namespaces consistent ....................... 10, no strays
```

---

# Part 7 — Before / after

| | v1 | v2 |
|---|---|---|
| Projects | 1 | 3 |
| Interfaces | 0 | 2 |
| Raw `MessageBox.Show` | 18 | 0 |
| Largest UI file | 429 lines | ~230 lines |
| `DocumentPrinter` | 310 lines, 1 switch | renderer + 7 templates, no switch |
| Add a document | edit a 310-line class | add a file + 1 line |
| Swap the database | rewrite every form | change 1 line |
| Colour literals | 6 Designer files | 1 theme file |
| Total C# | 3,549 lines | 4,179 lines |

**On the line count:** the code got *longer*, and that is expected. DRY removes
**duplication**, not volume. The added lines are interfaces, a theme, reusable
helpers and seven small template classes — each with one clear owner. What
shrank is the number of places you must edit to make any given change, which is
the metric that actually matters.

---

# Part 8 — Honest limitations

- **Not compiled.** The sandbox cannot install the .NET SDK. Structure was
  verified by static analysis (see Part 6); a first build may still surface
  something. Paste any errors and they will be fixed.
- **No unit tests.** The refactor makes tests *possible* — `IBarangayRepository`
  can now be faked — but none were written. That would be the natural next step
  and the strongest proof the abstractions are real.
- **`IBarangayRepository` is arguably too wide.** See §3.5.
- **`DocumentRenderer.IsPreformatted` is a heuristic.** It looks for a colon at
  a fixed column. A body paragraph containing `"  : "` would be mis-detected.
  Acceptable at this scale; a `bool IsPreformatted` flag on the template would
  be the robust fix.
- **No DI container.** Wiring is by hand in `Program.cs` — deliberate, to avoid
  a NuGet dependency and keep the graph visible.
