# Documentation index

Current, describing the composite app (Draft UI + Draft2 MySQL):

| File | What it is |
| --- | --- |
| `01-requirements.md` | The system's requirements (Draft-era; its "no database by default" storage story is superseded by the composite's MySQL layer) |
| `02-erd.svg` | ERD — matches `Database/schema.sql` (composite build) |
| `03-uml.svg` | Request state machine — matches `Services/RequestService.cs` |
| `04-project-timeline.md` | The team's project timeline |
| `06-pushing-to-github.md` | Git workflow for the group |
| `07-fee-schedule-and-legal-basis.md` | **The Citizen's Charter fee reference — seeded into `fee_schedule` by `Database/schema.sql`** |

Legacy (describes the retired v3.1 / .NET 8 app; kept for the project
write-up, no longer matches the code):

| File | Why it moved |
| --- | --- |
| `legacy/00-legacy-readme-v3.2.2.md` | The old root README (v3.2.2 group write-up) |
| `legacy/05-database-guide.md` | Describes the old `barangay_magugpo` schema, superseded by `Database/schema.sql` |
| `legacy/08-demo-walkthrough.md` | Demo script for the retired app |
