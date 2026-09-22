# Barangay Document System

Windows Forms application for Barangay Magugpo Poblacion, City of Tagum.
This version targets **.NET Framework 4.7.2** and uses one application project.

## Run

1. Open `BarangayDocumentSystem.sln` in Visual Studio 2022. The `.slnx` file
   is also available for newer versions that support that format.
2. Install the **.NET desktop development** workload and the **.NET Framework
   4.7.2 targeting pack** if Visual Studio asks for them.
3. Set `BarangayDocumentSystem` as the startup project, then press **F5**.

If the app opens as an empty white window, check that you pulled the latest
`Draft` branch: the earlier models-and-storage checkpoint still had the empty
starter form. Rebuild the solution after pulling. At runtime, the app starts
on Dashboard with Residents and Document Requests in the sidebar.

The Form Designer shows the main window layout. The three data pages are
created by the runtime constructor; use F5 to see the working dashboard.

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
| `Helpers` | Shared UI layout, messages, and error logging |
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

For a guided demo, read [the walkthrough](docs/Walkthrough.md).
For the structure and class responsibilities, read [the code guide](docs/CodeGuide.md).

## Current limits

No database persistence, login or user roles, photo capture, blotter records,
or refund processing. Printing uses classroom document wording that must be
reviewed before official use. The older project documentation's .NET 8 target
has been replaced by .NET Framework 4.7.2 for this restart.
