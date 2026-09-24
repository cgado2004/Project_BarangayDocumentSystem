# Draft-branch UI restyle package — navy design, C# 7.3 / .NET Framework 4.7.2

For **Jonathan** to apply on the `Draft` branch. Restyles the WinForms UI to
match the arena branch's navy design (`docs/dashboard-preview.png` there).
**Look and layout only** — services, SQL, validation, handlers and data
wiring are untouched.

## What's in this package

| File | Kind | Purpose |
|---|---|---|
| `Helpers/ModernTheme.cs` | NEW | design tokens, bundled-font resolution + per-role cache, drawing helpers, `StyleGrid` / `StylePrimary` / `StyleSecondary` / `StyleStatCard` / `StyleSidebar` |
| `Helpers/ModernControls.cs` | NEW | `HeroBanner`, `PillChip`, `NavyBar` (all painted, C# 7.3) |
| `Controls/DashboardControl.Theme.cs` | NEW (partial) | hero banner, six accent cards, purok pills, document bars, styled status table |
| `Controls/ResidentsControl.Theme.cs` | NEW (partial) | grid + buttons + labels theme |
| `Controls/RequestsControl.Theme.cs` | NEW (partial) | grid + buttons + details card theme |
| `Forms/MainForm.Theme.cs` | NEW (partial) | shell theme, 1360×860 default, red live-figures footer |
| `APPLY.md` | guide | the ONLY edits to existing files (csproj, MainForm ×3, DashboardControl ×2, Program ×1, assets) |

## How it stays designer-safe and behavior-safe

- Theme partials hook `OnLoad`, which runs **after** `InitializeComponent`
  and after the constructors — the designer never sees custom logic.
- No `Designer.cs` is modified. No control is renamed, re-parented or
  detached. `ReportingService.GetStatistics` wiring is intact: the original
  `RefreshData` lines all stay, and the new visuals subscribe to a
  notification raised at the end of it.
- Numbers come only from `ReportingService` — nothing invented. The one
  substitution is documented in `DashboardControl.Theme.cs`: the spec's
  "Released" tile has no matching statistic on this branch, so the sixth
  card keeps its real meaning (total document requests) in the emerald slot.
- No "PerMonitorV2 / DPI" text anywhere; the footer is the red live-figures
  line the spec asks for.

## Honest limitations (read before applying)

1. **This package has never been compiled.** The authoring sandbox has no
   Windows and no way to build a classic .NET Framework 4.7.2 csproj — the
   C# 7.3 constraints were enforced by hand (block namespaces, explicit
   usings, no C# 8 constructs). Expect the possibility of small fix-ups at
   build time; if the Error List shows anything, stop and report it.
2. **The preview capture must be taken on Windows** per the task ("when it
   builds, runs and matches… capture") — that step belongs to whoever runs
   it, per `APPLY.md` §6. Nothing here fakes a screenshot.
3. The hero's Punong Barangay line uses the design's official text
   ("HON. EUGENIA SOLIS HINGPIT, MD"). If `Draft` should read it from
   `AppSettings` instead, that is a one-line change in
   `Helpers/ModernControls.cs` (`HeroBanner.OnPaint`).

## After applying

Copy this folder's contents into the `Draft` working tree following
`APPLY.md`, build, run, capture, commit on `Draft`, push `Draft`.
Suggested commit message:

```
Restyle UI to the navy design system (theme partials, no behavior changes)
```
