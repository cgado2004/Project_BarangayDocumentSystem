# APPLY.md — restructuring `master` to the professor's folder protocol

The reference is the HRIS project's Solution Explorer (and the same
protocol Frent already applied to `leader_draft`):

```
project root:  DBContext/  Helper/  Interfaces/  Models/  Service/
               forms + App.config + packages.config* + Program.cs
```

\* `packages.config` is the legacy .NET Framework mechanism. This tree is
SDK-style .NET 8, whose equivalent — `PackageReference` inside the
`.csproj` — is used instead (see the csproj comments).

## Why master and not our branch

The arena/`leader_draft` line **already** follows this protocol (Frent's
restructure). `master` did not: it held the old three-project layout.

## Mapping applied (every file byte-for-byte; ZERO code edits)

| Old (`master`) | New |
|---|---|
| `src/BarangayDocumentSystem.Domain/Abstractions/IBarangayRepository.cs` | `BarangayDocumentSystem/Interfaces/` |
| `src/...Domain/Abstractions/IDocumentTemplate.cs` | `BarangayDocumentSystem/Interfaces/` |
| `src/...Domain/Entities/{DocumentRequest,Enums,Resident}.cs` | `BarangayDocumentSystem/Models/` |
| `src/...Domain/Services/{DocumentRenderer,FeeSchedule}.cs` | `BarangayDocumentSystem/Service/` |
| `src/...Domain/Templates/*.cs` (7) | `BarangayDocumentSystem/Service/Templates/` |
| `src/...Infrastructure/{InMemory,MySql}BarangayRepository.cs` | `BarangayDocumentSystem/DBContext/` |
| `src/...Infrastructure/{DatabaseInitializer,DatabaseSettings}.cs` | `BarangayDocumentSystem/DBContext/` |
| `src/...Infrastructure/schema.sql` | `db/schema.sql` (repo root, the protocol's db home) |
| `src/...UI/Theme/AppTheme.cs` | `BarangayDocumentSystem/Helper/` |
| `src/...UI/Common/{Dialog,InputValidator,UiFactory}.cs` | `BarangayDocumentSystem/Helper/` |
| `src/...UI/Common/NavigationSidebar.cs` | `BarangayDocumentSystem/` (root, like the protocol's root controls) |
| `src/...UI/Views/{DashboardView,RequestsView,ResidentsView,ViewBase}.cs` | `BarangayDocumentSystem/` root |
| `src/...UI/MainShell.cs` | `BarangayDocumentSystem/` root |
| `src/...UI/Forms/*.cs` (+ `.Designer.cs`, `Prompt.cs`) | `BarangayDocumentSystem/` root (forms at root, per the picture) |
| `src/...UI/Program.cs` | `BarangayDocumentSystem/` root |
| `docs/`, `README.md`, `.gitignore` | unchanged at root |

**Dropped deliberately:** the three old `.csproj` files and the old `.sln`
(they describe the three-project layout and cannot survive the merge).

**Authored new:** `BarangayDocumentSystem/BarangayDocumentSystem.csproj`
(single merged project), `BarangayDocumentSystem.slnx`,
`BarangayDocumentSystem/App.config` (the `BarangayDb` connection
template `DatabaseSettings` reads; master had no App.config at all).

## Two things the merge changes on purpose

1. **Namespaces are NOT normalized.** Files keep
   `BarangayDocumentSystem.Domain.*` / `.Database` / `.UI` namespaces
   inside the single project. That is compile-safe (usings resolve the
   same way inside one assembly) and keeps this a *pure* restructure —
   zero code edits. Normalizing namespaces to match folders is a
   recommended follow-up commit, done with an IDE rename.
2. **The two missing package references are added** in the csproj
   (`MySql.Data` 8.4.0, `System.Configuration.ConfigurationManager`
   8.0.0). Without them the uploaded master could not compile at all —
   see the csproj comments.

## How to land it

1. On a machine with push rights: `git checkout -b master-restructured origin/master`
2. Delete `src/`, copy this folder's contents over the repo root.
3. `dotnet build BarangayDocumentSystem/BarangayDocumentSystem.csproj`
   — expect success (NuGet restores the two packages).
4. Run once (in-memory default; the MySQL switch in `Program.cs` stays
   commented until XAMPP is up, exactly as uploaded).
5. Commit, push the branch, open a PR **into `master`** and merge.

## Honest limitations

- Authored in the same no-Windows sandbox as everything else: file
  contents are untouched originals, but nothing here has been compiled.
  The two package pins are the standard current versions; if NuGet
  complains, adjust the version, not the code.
- `Prompt.cs` still exists on master and is kept (a restructure does not
  judge). On the arena line it was removed as orphaned; audit separately.
