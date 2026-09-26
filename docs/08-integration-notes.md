# Branch integration baseline

## Precedence

| Concern | Reference | Integration decision |
|---|---|---|
| Runtime | Team requirement; Draft2 | Classic WinForms project targeting .NET Framework 4.8 |
| Folders | Draft2 | BusinessRules/DocumentTemplates, CustomControls, Database, Forms, Interfaces, Models, UIHelpers, Views |
| UI | Draft | Flat navigation, summary cards and table breakdowns; leader palette and seal retained |
| Fees | leader_draft docs/07 | Keep existing rules and reference document; no new legal assumptions |
| Persistence and final models | Forthcoming ERD/UML/documents | Retain demo repository; do not import conflicting SQL Server/MySQL schemas |

Reference branches were inspected, not merged wholesale. In particular, Draft2's
tracked bin/obj outputs and Draft's competing model/repository classes were not
imported. There is one application project, one repository contract, one fee
schedule and one model set.

## Changes

- Replaced the .NET 8 SDK project and `.slnx` with a classic Framework 4.8
  `.csproj` and `.sln`. Removed the obsolete SDK pin and inert packages.config.
- Replaced incompatible startup, enum, string, clamp, null-check, dictionary and
  index-from-end APIs. Added a Framework cue-banner TextBox with handle-recreation
  support and the compiler marker for immutable records.
- Made source imports explicit and updated moved namespaces and designer references.
- Kept Framework DPI opt-in in App.config; removed manifest DPI overrides.
- Simplified dashboard and navigation toward Draft, retained shared grid styling,
  and removed duplicated resident/request page headings. Logo ownership now
  disposes replaced images and avoids keeping an image's source stream alive.
- Shell disposes cached views, including those no longer attached to the content panel.
- Fixed an existing community-tax template constructor mismatch and replaced
  duplicated base/rate amounts with the injected FeeSchedule values.
- Deleted docs/dashboard-preview.png. Removed obsolete README claims about a
  successful build; migrated the existing regression harness to Framework 4.8.

## Pending supplied documents

The existing docs/02-erd.svg, docs/03-uml.svg and the SQL scripts in
BarangayDocumentSystem/Database/ are left intact as reference artifacts.
They are not treated as the newly approved designs.
When the team supplies its documents, reconcile entity fields, relationships,
cardinalities, validation, request states, retention/deletion behavior, storage
provider and fee evidence before implementing persistence or replacing diagrams.

## Verification

Structural checks and whitespace validation pass in this environment. Neither
MSBuild nor a .NET runtime is installed, and SDK downloads failed. No successful
compile or Windows UI run is claimed. Follow the README's Windows build and
regression commands, then smoke-test the designer, DPI behavior and printing.
