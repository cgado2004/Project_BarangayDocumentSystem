# APPLY.md — surgical edits to existing files

Everything else in this package is NEW files. Only these edits touch
existing code — apply them exactly, in this order. (All on the `Draft`
branch, commit `77485c6` or later.)

---

## 1. `BarangayDocumentSystem.csproj` — register the new files

Inside the existing `<ItemGroup>` that holds the `<Compile Include="...">`
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

In a NEW `<ItemGroup>` (after the existing ones):

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

## 2. `Forms/MainForm.cs` — three surgical edits

**(a)** Add one field (next to the three page fields):

```csharp
        private readonly Services.ReportingService reporting;
```

**(b)** In the 5-argument constructor, keep the reference (add one line
anywhere after the parameter list opens):

```csharp
            this.reporting = reporting;
```

**(c)** In `ShowPage(...)`, replace the single green line:

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

Nothing else in this file changes — every handler stays as-is.

## 3. `Controls/DashboardControl.cs` — two lines so the theme sees fresh figures

**(a)** Inside the class, add the notification event:

```csharp
        internal event Action<Models.BarangayStatistics> StatisticsUpdated;
```

**(b)** At the very end of `RefreshData()` (after the `gridPuroks.DataSource = ...`
line), add:

```csharp
            if (StatisticsUpdated != null) StatisticsUpdated(statistics);
```

`RefreshData()` keeps every original line — the grids stay populated exactly
as before (the theme hides `gridTypes`/`gridPuroks` visually but the wiring,
names and data sources are 100% intact).

## 4. `Program.cs` — one line so fonts resolve before any form is built

In `Main()`, right after `Application.EnableVisualStyles();`:

```csharp
            ModernTheme.Resolve();
```

(`using BarangayDocumentSystem.Helpers;` is already at the top of Program.cs.)

## 5. Copy the brand assets from the arena branch

```powershell
git fetch origin arena/01a0cf05-project-barangaydocumentsystem
git checkout origin/arena/01a0cf05-project-barangaydocumentsystem -- BarangayDocumentSystem/Assets/barangay-logo.png
mkdir BarangayDocumentSystem\Assets\fonts
git checkout origin/arena/01a0cf05-project-barangaydocumentsystem -- BarangayDocumentSystem/Assets/fonts/Inter-Regular.ttf BarangayDocumentSystem/Assets/fonts/Inter-Bold.ttf
```

(Inter ships under the SIL Open Font License — included as
`Assets/fonts/OFL-Inter.txt` on the arena branch; copy it too.)

## 6. Build, run, capture

1. Build (`Ctrl+Shift+B`) — expect 0 errors. If anything fails, STOP and
   send me the Error List; do not push a broken restyle.
2. F5. The window opens at 1360×860 with sample data.
3. Verify all three screens against the reference:
   `docs/dashboard-preview.png` on `arena/01a0cf05-project-barangaydocumentsystem`.
4. Capture the Dashboard at exactly 1360×860 (Win+Shift+S or the
   Snipping Tool — capture the client area) and save it as
   `docs/dashboard-preview.png` on the `Draft` branch (it does not exist
   there yet — create it).
5. Reference it in `Draft`'s `README.md` under Features:

```markdown
![Dashboard](docs/dashboard-preview.png)
```

Do not touch any other image.
