# Code guide

## Start with these files

1. `Program.cs`: creates the objects the app needs, then opens the main window.
2. `Forms/MainForm.cs`: switches between the three pages.
3. `Controls/ResidentsControl.cs`: opens resident dialogs.
4. `Forms/ResidentForm.cs`: collects the typed values and asks the service to save.
5. `Services/ResidentService.cs`: validates and saves a resident.
6. `Data/InMemoryBarangayRepository.cs`: stores a copy of the saved record.

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
| `FeeSchedule` | All classroom fee amounts and exemption rules |
| `RequestService` | Filing, status changes, receipts, release, and rejection |
| `DocumentRenderer` | Shared letterhead, footer, and template selection |
| `IDocumentTemplate` | The title and body supplied by a document template |
| `IBarangayRepository` | The storage operations used by the services |
| `InMemoryBarangayRepository` | Lists of records for the current session |
| `TextPaginator` | Fit text onto a printed page and continue on the next page |

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
repository stores a copy, so canceling an edit cannot alter a saved record.

## Request and payment rules

- Only Pending can move to Processing.
- Only Processing can move to Ready for Release.
- Only Ready for Release can move to Released.
- Fee-bearing requests need payment and an official receipt before release.
- Receipt numbers are unique within the session, ignoring letter case.
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
and collection history. A future database implementation should perform
release and benefit-use updates within a database transaction.

## UI layout and errors

Layouts are separated from event handlers using partial classes.
`.Designer.cs` files hold the layout; the matching `.cs` files hold behavior.
Most dialogs use the simple factories in `UiLayout` to keep button and grid
styles consistent. These layouts are currently maintained in code; review
the layout file when changing controls.

`UiFeedback.Run` catches expected validation and workflow errors and displays
their messages. Unexpected exceptions are logged by `ErrorLogger`.

Names use PascalCase for classes and methods, camelCase for fields and local
variables, and short prefixes such as `txt`, `cmb`, and `btn` for controls.
Each public model, enum, interface, and service has its own matching file.
