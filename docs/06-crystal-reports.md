# Crystal Reports in this system

Two ways to print a report, and the barangay can use either:

1. **Plain-paper reporter (always available).** The program draws the report
   itself with Windows printing — letterhead, seal, columns, grouping and
   totals. Nothing has to be installed. This is what the *Print* button uses
   when Crystal Reports is not on the computer.
2. **Crystal Reports (when it is installed).** The same report, handed to a
   `.rpt` layout file so the barangay can change the look without me changing
   the code.

## Why Crystal is not a hard reference

The solution builds and runs on a computer with no Crystal Reports installed.
That is on purpose:

* Visual Studio projects that reference `CrystalDecisions.*` fail at build
  time with "type or namespace could not be found" on any machine without the
  Crystal runtime — and most of the machines at the barangay hall, including
  the laptop used for the demo, do not have it.
* So `Services/Reports/CrystalReportGateway.cs` finds the Crystal assemblies
  **at run time** (they are registered in the Global Assembly Cache by the
  Crystal installer) and only then creates the viewer. No `using
  CrystalDecisions...` statement appears anywhere in the project.

If Crystal is installed, the *Print with Crystal Reports* button lights up. If
it is not, the button stays disabled and says why, in one sentence, next to it.

## Enabling it on a computer

1. Install **SAP Crystal Reports runtime for .NET Framework (64-bit, SP 3x)**
   or the full Crystal Reports developer edition. Either registers
   `CrystalDecisions.CrystalReports.Engine.dll`, `CrystalDecisions.Shared.dll`
   and `CrystalDecisions.Windows.Forms.dll` in the GAC.
2. Either keep `Reports.UseCrystalReports` as `true` in `App.config`, or set
   it to `false` to force the plain reporter even when Crystal is present.
3. Put the `.rpt` files in the folder named by `Reports.CrystalFolder`
   (`Reports\CrystalReports` by default), next to the `.exe`. The build copies
   anything in that folder to the output directory, so the folder next to the
   program is the one that matters.

## The layout files and the data they receive

Each report key below matches a constant in
`Services/Reports/ReportDefinitions.cs`. A layout gets a single `DataTable`
with exactly the columns the report service builds — bind a field in the
`.rpt` by its column name.

| Key | Report | Columns |
|---|---|---|
| `daily` | Per-day transactions | `txn_date`, `filed`, `released`, `rejected`, `waiting`, `free_issued`, `collected` |
| `register` | Document register | `reference_number`, `date_requested`, `resident`, `document`, `purpose`, `status`, `fee`, `is_paid`, `official_receipt_no` |
| `collections` | Collections (OR) | `or_number`, `series_code`, `control_number`, `or_date`, `payer`, `amount`, `method`, `collected_by`, `is_void` |
| `census` | Census summary | `item`, `value`, `note` |
| `age` | Population by age | `age_bracket`, `male`, `female`, `total` |
| `purok` | Population by purok | `purok`, `residents`, `households`, `male`, `female` |
| `residents` | Residents masterlist | `last_name`, `first_name`, `middle_name`, `suffix`, `purok`, `age`, `gender`, `civil_status`, `residency_status`, `record_state` |
| `households` | Households and dependents | `purok`, `head`, `household_size`, `dependents`, `senior`, `pwd`, `indigent`, `solo_parent`, `four_ps` |
| `business` | Business clearances | `reference_number`, `date_requested`, `business_name`, `business_nature`, `business_purok`, `business_ownership`, `business_employees`, `status`, `fee`, `is_paid`, `official_receipt_no` |
| `activity` | Activity log | `occurred_on`, `username`, `role`, `module`, `action`, `target_reference`, `details`, `machine_name` |
| `fees` | Fee schedule | `document`, `amount`, `basis`, `note` |

Parameters the user picks on the Reports screen (date range, purok) are
applied **before** the table reaches Crystal, so the layout itself does not
need parameter fields — though you may add them for the heading.

## The three states the Reports screen explains

The screen never just fails; it tells the clerk which of these is true:

* **Crystal is switched off** in `App.config` → the button is disabled and the
  note says so.
* **Crystal is not installed** on this computer → the button is disabled and
  the note says the plain reporter will be used.
* **The `.rpt` for this report is missing** from the Crystal folder → the
  button is disabled and the note names the file it expects.

## Building a layout the barangay can keep

1. Open Crystal Reports, *New → Blank Report*, and connect it to
   *ADO.NET (XML)* — do not connect it to the live MySQL database. The
   program passes it a ready-made table; a layout with its own connection
   will show yesterday's data or nothing at all.
2. Add a `DataTable` data source with the column names from the table above.
   Adding extra fields is fine; missing ones print blank.
3. Design the page like the plain reporter: the seal top-left, the barangay
   name and address line at the top, the report title, then the columns, the
   totals row and the footer from `Reports.Footer`.
4. Save as `<Report>.rpt` into `Reports\CrystalReports` (names such as
   `PerDayTransactions.rpt`, `Collections.rpt`, `CensusSummary.rpt`).
5. Run the program, open **Reports**, choose the report, press
   *Print with Crystal Reports*. The viewer opens inside the program with the
   layout loaded and the current rows already in it.

## What I would tell the barangay

Use the plain reporter day to day — it always works and it prints the same
figures. Keep the Crystal layouts for the reports that go to the Municipal
Hall, where the look matters more than the labels on the buttons.
