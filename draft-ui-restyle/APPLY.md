# APPLY.md — applying the navy restyle to Draft

Everything in this package is either a NEW file or an exact edit listed
below. Apply on the `Draft` branch (tip `77485c6` or later), in this order.
**If any step fails, STOP and send the Error List — do not push a broken
restyle.**

The design being applied: canvas `#F8FAFC`, ink `#0F172A`, muted `#64748B`,
primary navy `#1E3A8A`, hero navy `#1B365D`, gold `#F2B11B`, white rounded
cards (radius 16) with soft shadows; Inter from the bundled fonts with a
Segoe UI fallback, cached per role. Only the look changes — services, SQL,
validation, handlers and control names are untouched.

---

## 1. Copy the package into the branch

From the arena branch checkout that carries `draft-ui-restyle/`:

```powershell
git checkout origin/arena/01a0cf05-project-barangaydocumentsystem -- draft-ui-restyle
```

Files land as follows:

| Package file | Becomes on Draft |
|---|---|
| `Helpers/ModernTheme.cs`, `Helpers/ModernControls.cs` | `Helpers\` (new) |
| `Controls/DashboardControl.Theme.cs`, `Controls/ResidentsControl.Theme.cs`, `Controls/RequestsControl.Theme.cs` | `Controls\` (new) |
| `Forms/MainForm.Theme.cs` | `Forms\` (new) |
| `Tests/ThemeChecks.cs` | `Tests\` (new) |
| `README.md` | repo root `README.md` (replaces; adds the theme bullet + screenshot) |
| `docs/CodeGuide.md`, `docs/Walkthrough.md` | `docs\` (replace; UI sections updated) |
| `docs/02-erd.svg`, `docs/03-uml.svg` | `docs\` (new — Draft's ERD and UML, generated from `Data/Schema.sql` and the real classes) |
| `docs/_gen_diagrams.py` | `docs\` (the generator; stdlib-only Python, optional to keep) |
| `APPLY.md` | this file — do not commit it to Draft; delete after applying |

## 2. `BarangayDocumentSystem.csproj` — register the new files

Inside the existing `<ItemGroup>` holding the `<Compile Include="...">`
entries, add:

```xml
    <Compile Include="Helpers\ModernTheme.cs" />
    <Compile Include="Helpers\ModernControls.cs" />
    <Compile Include="Controls\DashboardControl.Theme.cs">
      <SubType>UserControl</SubType>
    </Compile>
    <Compile Include="Controls\ResidentsControl.Theme.cs">
      <SubType>UserControl</SubType>
    </Compile>
    <Compile Include="Controls\RequestsControl.Theme.cs">
      <SubType>UserControl</SubType>
    </Compile>
    <Compile Include="Forms\MainForm.Theme.cs">
      <SubType>Form</SubType>
    </Compile>
```

In a NEW `<ItemGroup>` after the existing ones:

```xml
  <ItemGroup>
    <Content Include="Assets\barangay-logo.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="Assets\fonts\Inter-Regular.ttf">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="Assets\fonts\Inter-Bold.ttf">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```

## 3. `Tests/BarangayDocumentSystem.Tests.csproj` — one entry

Next to the other `<Compile Include>` entries:

```xml
    <Compile Include="ThemeChecks.cs" />
```

## 4. `Forms/MainForm.cs` — three surgical edits

**(a)** Add one field next to the three page fields:

```csharp
        private readonly Services.ReportingService reporting;
```

**(b)** In the 5-argument constructor, keep the reference (one line):

```csharp
            this.reporting = reporting;
```

**(c)** In `ShowPage(...)`, replace the two green lines:

```csharp
                foreach (var button in new[] { btnDashboard, btnResidents, btnRequests })
                    button.BackColor = button == selectedButton ? Color.FromArgb(57, 120, 100) : Color.FromArgb(31, 74, 64);
```

with:

```csharp
                foreach (var button in new[] { btnDashboard, btnResidents, btnRequests })
                {
                    bool active = button == selectedButton;
                    button.BackColor = active ? Helpers.ModernTheme.PrimaryNavy : Color.White;
                    button.ForeColor = active ? Color.White : Helpers.ModernTheme.Muted;
                    button.Invalidate();
                }
                RefreshFooter();
```

Nothing else in the file changes — every handler stays as-is.

## 5. `Controls/DashboardControl.cs` — two lines so the theme sees live figures

**(a)** Inside the class, add the notification event:

```csharp
        internal event Action<Models.BarangayStatistics> StatisticsUpdated;
```

**(b)** At the very end of `RefreshData()` (after the `gridPuroks.DataSource = ...`
line):

```csharp
            if (StatisticsUpdated != null) StatisticsUpdated(statistics);
```

`RefreshData()` keeps every original line — the grids stay populated exactly
as before; the theme only *hides* `gridTypes`/`gridPuroks` visually and draws
the chips/bars from the same statistics.

## 6. `Program.cs` — one line so fonts resolve before any form is built

In `Main()`, right after `Application.EnableVisualStyles();`:

```csharp
            ModernTheme.Resolve();
```

(`using BarangayDocumentSystem.Helpers;` is already at the top.)

## 7. `Tests/Program.cs` — one line registering the theme checks

After the `UiSmoke` line in `Main`:

```csharp
            Run("Navy theme tokens, fonts, and drawing helpers", ThemeTokens.Run);
```

## 8. Copy the brand assets from the arena branch

```powershell
git fetch origin arena/01a0cf05-project-barangaydocumentsystem
git checkout origin/arena/01a0cf05-project-barangaydocumentsystem -- BarangayDocumentSystem/Assets/barangay-logo.png
mkdir BarangayDocumentSystem\Assets\fonts
git checkout origin/arena/01a0cf05-project-barangaydocumentsystem -- BarangayDocumentSystem/Assets/fonts/Inter-Regular.ttf BarangayDocumentSystem/Assets/fonts/Inter-Bold.ttf BarangayDocumentSystem/Assets/fonts/OFL-Inter.txt
```

(Inter ships under the SIL Open Font License — `OFL-Inter.txt` rides along.)

## 9. Build, run the tests

```powershell
MSBuild BarangayDocumentSystem.sln /p:Configuration=Debug
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe --sql
```

Expect every line PASS (the SQL ones too, with LocalDB running). The new
theme checks verify the palette tokens, font caching, and the drawing
helpers; nothing may still reference the old green palette.

## 10. Run, verify, capture (the one thing this package cannot do)

1. F5. The window opens at 1360×860: white sidebar with the seal and gold
   BARANGAY overline, navy hero banner, six stat cards — RESIDENTS / PENDING
   / READY / RELEASED / COLLECTED (₱, live) / ISSUED FREE — purok chips,
   navy document-type bars, red live-figures footer. No DPI text anywhere.
2. Check Residents and Document requests: navy table headers, light-blue
   selected rows, navy primary / outlined secondary buttons, details card.
3. Verify all three screens against the reference:
   `docs/dashboard-preview.png` on `arena/01a0cf05-project-barangaydocumentsystem`.
4. Capture the Dashboard at exactly **1360×860** (Snipping Tool, client
   area) and save it as `docs/dashboard-preview.png` **on the Draft branch**.
   The README already references it.

## Honest limitation

Steps 9–10 ran nowhere: this package was authored in a Linux sandbox with no
Windows, no .NET Framework 4.7.2, and no LocalDB. The code is a verified
C# 7.3 port anchored against Draft's exact sources, and the diagrams were
rendered and visually checked here — but the build, the test run, and the
screenshot happen on your machine. If anything fails, stop and report.
