# Barangay Document System

Windows Forms application for Barangay Magugpo Poblacion, City of Tagum.
This version targets **.NET Framework 4.7.2** and uses one application project.

## Run

1. Open `BarangayDocumentSystem.sln` in Visual Studio 2022. The `.slnx` file
   is also available for newer versions that support that format.
2. Accept Visual Studio's missing-component prompt from the included `.vsconfig`.
   It lists **.NET desktop development** and the **.NET Framework 4.7.2 targeting
   pack**. You can also import `.vsconfig` using Visual Studio Installer.
3. Set `BarangayDocumentSystem` as the startup project, rebuild, then press **F5**.

Use Windows with the .NET Framework 4.7.2 runtime or a later compatible 4.x
runtime. Building also needs the 4.7.2 targeting pack, even when a newer runtime
is installed. Installing .NET 8/10 alone does not supply that targeting pack.
The project uses C# 7.3 and the classic `.sln` format for VS 2022 compatibility.
This Windows Forms application does not run natively on macOS or Linux.

Visual Studio can detect prerequisites from a solution's `.vsconfig`; see
[Microsoft's installation configuration guide](https://learn.microsoft.com/en-us/visualstudio/install/import-export-installation-configurations).

If Visual Studio says the startup project cannot be launched, open **Project
Properties > Debug** and select **Start project**. The shared project points
to `BarangayDocumentSystem.Program` and uses a Windows executable output.
Local `.vs` and `.csproj.user` settings are not shared through Git. To separate
an IDE setting problem from an application problem, try opening
`bin\Debug\BarangayDocumentSystem.exe` after a successful build. Keep its
`.exe.config` file alongside it. If that works but F5 fails, check the IDE's
startup/debug settings; avoid changing document or fee code to fix the launcher.

If the app opens as an empty white window, check that you pulled the latest
`Draft` branch: the earlier models-and-storage checkpoint still had the empty
starter form. Rebuild the solution after pulling. At runtime, the app starts
on Dashboard with Residents and Document Requests in the sidebar.

The Form Designer shows the main window layout. The three data pages are
created by the runtime constructor; use F5 to see the working dashboard.
To edit those pages, open the corresponding files under `Controls` in the
designer. Forms and pages use ordinary designer declarations and named event
handlers; their design-time constructors do not load services or sample data.

Seven fictional residents and six requests load by default. Set
`LoadSampleData` to `false` in `App.config` to start with empty lists.

**Records are kept in memory and are lost when the application closes.**
This follows the project's current no-database scope.

## What works

- Dashboard with resident counts, requests by status and document, collections,
  free documents released, and residents by purok.
- Resident registration, search, editing, deletion, and classifications.
- Seven document types, with fee assessment and an explanation of the fee.
- Pending → Processing → Ready for Release → Released workflow.
- Rejection with a required reason; payments remain in history if a paid
  request is rejected.
- Official receipt numbers, duplicate-receipt checks, and payment before release.
- First-time jobseeker eligibility checks, an oath in the document, and a
  once-only benefit.
- Document text preview, print preview, and printing through Windows printers
  such as Microsoft Print to PDF.

Released documents keep their finalized text. Changing a resident later does
not change documents already released. Residents with any document requests
cannot be deleted.

## Where the code belongs

| Folder | Purpose |
|---|---|
| `Models` | Resident and request data, enums, and result objects |
| `Interfaces` | Contracts for storage and document templates |
| `Services` | Validation, fees, request workflow, and document generation |
| `Data` | In-memory storage and fictional sample data |
| `Documents` | One template class for each document type |
| `Forms` | Main window and input dialogs |
| `Controls` | Dashboard, Residents, and Requests pages |
| `Helpers` | User messages and error logging |
| `Printing` | Page layout and Windows printing |
| `Configuration` | Loading and checking `App.config` |
| `Tests` | Executable checks for rules, forms, and pagination |

`Program.cs` creates the repository and services, loads optional sample data,
and opens `MainForm`. A button handler calls a service; it does not calculate
fees or modify the repository directly.

## Settings

`App.config` contains the barangay, city, province, signing official, and
sample-data switch. Restart the app after changing these settings. Set the
signing official's name before presenting generated documents.

Fee amounts and exemptions are centralized in `Services/FeeSchedule.cs`.
Residency and good moral certificates use the PHP 100 base fee shown in the
supplied Citizen's Charter photo. Other rates and personal exemptions still
use **classroom assumptions** while the remaining Charter work is pending.
See [fee and document notes](docs/FeePolicy.md).

Unexpected errors are logged under
`%LOCALAPPDATA%\BarangayDocumentSystem\errors.log`.
Expected input errors appear as messages and do not save the invalid record.

## Verify

From a Visual Studio Developer PowerShell:

```powershell
MSBuild Tests\BarangayDocumentSystem.Tests.csproj /p:Configuration=Debug
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe
```

The runner exits with a nonzero code if a check fails. Screenshots are written
under the test build's `Screenshots` folder, which Git ignores. The printer
preview check uses Microsoft Print to PDF without submitting a print job;
it is skipped if that driver is unavailable.

These executable tests check running forms, including real button clicks and
filter events. They do not invoke the Visual Studio designer. After changing
layout code, also open each of the six forms and three controls in the designer,
and check F5 from a fresh checkout. A runtime-only test cannot catch all designer
parsing errors.

For a guided demo, read [the walkthrough](docs/Walkthrough.md).
For the structure and class responsibilities, read [the code guide](docs/CodeGuide.md).

## Current limits

No database persistence, login or user roles, photo capture, blotter records,
or refund processing. Printing uses classroom document wording that must be
reviewed before official use. The older project documentation's .NET 8 target
has been replaced by .NET Framework 4.7.2 for this restart.
