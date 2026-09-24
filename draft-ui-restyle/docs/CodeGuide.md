# Code guide

## Start with these files

1. `Program.cs`: creates the objects the app needs, then opens the main window.
2. `Forms/MainForm.cs`: switches between the three pages.
3. `Controls/ResidentsControl.cs`: opens resident dialogs.
4. `Forms/ResidentForm.cs`: collects the typed values and asks the service to save.
5. `Services/ResidentService.cs`: validates and saves a resident.
6. `Data/SqlBarangayRepository.cs`: saves and retrieves records in SQL Server.

The same pattern is used for document requests and payments.

## Logical layers

This restart keeps one application project with separate folders:

- Business data and rules: `Models`, `Interfaces`, `Services`, `Documents`.
- Storage: `Data`.
- User interface: `Forms`, `Controls`, `Helpers`, `Printing`.

Models, services, storage, and templates do not reference Windows Forms.
These are logical boundaries in one assembly, not separate projects enforced
by the compiler.

## Why the main classes exist

| Class | Responsibility |
|---|---|
| `Resident` | A person's registry details |
| `DocumentRequest` | Request details, resident snapshot, payment, and status |
| `ResidentValidator` | Required fields, dates, classifications, contact format |
| `ResidentService` | Search, save, and delete operations |
| `FeeSchedule` | Document base fees, their sources, and classroom exemption rules |
| `RequestService` | Filing, status changes, receipts, release, and rejection |
| `DocumentRenderer` | Shared letterhead, footer, and template selection |
| `IDocumentTemplate` | The title and body supplied by a document template |
| `IBarangayRepository` | The storage operations used by the services |
| `SqlBarangayRepository` | Persistent storage using parameterized SQL |
| `SqlDatabase` | First-run schema creation and version check |
| `InMemoryBarangayRepository` | Isolated storage for tests |
| `ReportingService` | Dashboard counts, groups, and collection totals |
| `BarangayStatistics` | Results displayed by the dashboard |
| `TextPaginator` | Fit text onto a printed page and continue on the next page |
| `ModernTheme`, `ModernControls` | The navy theme: palette tokens, Inter/Segoe fonts, rounded cards, hero banner, pills, bars |

To add a document type, add its enum value, template, registration in
`Program.cs`, and fee rule in `FeeSchedule`. The renderer and views do not
need another document-specific condition. Business clearance is the existing
exception because it collects additional business details.

## Saving a resident

```text
Save button
  -> ResidentService.Save
  -> ResidentValidator.Validate
  -> repository.SaveResident
  -> refresh the pages
```

The form handles interaction. The validator rejects invalid data. The
repository saves to SQL Server and returns detached models, so canceling an edit
cannot alter a saved record. A version check rejects stale edits. Dashboard
calculations live in `ReportingService`; the control only displays its results.

## Request and payment rules

- Only Pending can move to Processing.
- Only Processing can move to Ready for Release.
- Only Ready for Release can move to Released.
- Fee-bearing requests need payment and an official receipt before release.
- Paid receipt numbers are unique across the saved database, ignoring letter case.
- Released and Rejected requests cannot change status again.
- Rejection requires a reason. An earlier payment remains recorded.
- A first-time jobseeker needs at least six months of residency and cannot
  already have used the benefit or have another active request for it.
- Eligibility is checked again at release.

## Protecting history

A request copies the resident's details when filed. On release, the generated
document text is stored too. Later name, address, profile, or template changes
cannot rewrite an already released document.

Residents with requests cannot be deleted. This preserves the link to request
and collection history. Release and benefit-use updates run within one database
transaction. SQL foreign keys, unique indexes, and version checks provide
additional protection. See [database setup and tables](Database.md).

## UI layout and errors

Layouts are separated from event handlers using partial classes.
`.Designer.cs` files hold the layout; the matching `.cs` files hold behavior.
`InitializeComponent` uses explicit control construction, property assignments,
container additions, and named event handlers that the Windows Forms designer
can read. Keep service calls, loops, layout factories, and inline event lambdas
out of that method. Grid columns have unique component names within each view;
their `DataPropertyName` values still identify the model properties to display.

The look is applied by `ApplyTheme()` methods in separate `.Theme.cs` partial
class files, called from `OnLoad` — strictly after `InitializeComponent`, so
the designer is never involved and keeps opening. `Helpers/ModernTheme.cs`
holds the navy-and-gold palette tokens, the Inter-from-`Assets\fonts` font
resolution with a Segoe UI fallback, and the rounded-rectangle, shadow, and
gradient helpers; `Helpers/ModernControls.cs` holds the hero banner, purok
chips, and document-type bars. Use the tokens instead of raw `Color.FromArgb`
values in theme code, and keep service calls out of the theme files.

Parameterless constructors create the designable controls. The constructors
that accept services supply runtime data. For example, `MainForm` shows its
shell in the designer; open `DashboardControl` to edit the dashboard layout.
Check both the designer and the running app when changing layouts.

`UiFeedback.Run` catches expected validation and workflow errors and displays
their messages. Unexpected exceptions are logged by `ErrorLogger`.

Names use PascalCase for classes and methods, camelCase for fields and local
variables, and short prefixes such as `txt`, `cmb`, and `btn` for controls.
Each public model, enum, interface, and service has its own matching file.
