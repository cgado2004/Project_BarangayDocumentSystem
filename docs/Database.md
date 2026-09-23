# Database setup

The app uses **Microsoft SQL Server Express LocalDB**, through the .NET
Framework's built-in `System.Data.SqlClient`. No additional NuGet package,
web server, or SQL password is needed for the default setup.

## First run on each laptop

1. In Visual Studio Installer, modify the installation and select **SQL Server
   Express LocalDB** under Individual components. It is also listed in this
   project's `.vsconfig`. Installing this prerequisite may require administrator
   access. SQL Server 2022 LocalDB was used for the integration checks.
2. Open `BarangayDocumentSystem.sln`, build, and run. The app connects as the
   current Windows user to `(LocalDB)\MSSQLLocalDB` and creates a database named
   `BarangayDocumentSystem` if it does not exist.
3. The schema and optional fictional sample records are installed automatically.
   No manual import or absolute file path is needed.

The default connection is in `App.config`, under `BarangayDatabase`. Each
Windows account has its own LocalDB instance and records. This is local
storage, not a database shared between classmates' laptops. Pulling source
code does not copy another person's records.

SQL Server Management Studio or Visual Studio's SQL Server Object Explorer
can connect to the same instance to inspect tables. Neither is required to
run the application. See [Microsoft's LocalDB documentation](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb).

## First-run samples and existing data

`LoadSampleData=true` adds the seven fictional residents and six requests only
when the database is initialized for the first time and has no records.
Set it to `false` **before the first run** for an empty database.
Changing the setting later does not erase records or reload samples.
Sample loading is transactional: an interrupted load leaves no partial sample
records and can be retried on startup.

Rebuilding, pulling, closing the app, and opening it again preserve saved
records. Checkouts using the same database name and Windows account use the
same records. For a separate demonstration database, change `Initial Catalog`
to another application name, such as `BarangayDocumentSystemDemo`.

## Tables and responsibilities

| Table | Purpose |
|---|---|
| `Residents` | Current resident details, classifications, and jobseeker-benefit use |
| `DocumentRequests` | Request details, status, fee, receipt, dates, and finalized document text |
| `RequestResidentSnapshots` | The resident details captured when each request was filed |
| `AppState` | Schema version and whether first-run sample initialization is complete |

One resident can have many requests. Each request has one resident snapshot.
Historical snapshots intentionally retain old values when the resident changes.
Request and snapshot insertion happen together in a transaction. Foreign keys
prevent deletion of a resident who has request history.

`SqlBarangayRepository` implements the same `IBarangayRepository` interface as
the in-memory test implementation. SQL parameters carry user input; names and
addresses are never inserted into SQL command text. Mapping classes translate
between table columns and the existing models.

Services still check the business rules. SQL adds unique indexes for paid
receipt numbers and active/released jobseeker requests, plus checks for valid
payments and releases. Version numbers reject stale edits instead of silently
overwriting newer records. Releasing a jobseeker certificate and recording
benefit use commit together or roll back together.

Schema version 1 is embedded in the application. Startup creates a missing
schema and verifies an existing version; it does not drop or recreate tables.
Future schema changes need an explicit migration.

## Verification and troubleshooting

Run the normal checks and then the database checks:

```powershell
MSBuild Tests\BarangayDocumentSystem.Tests.csproj /p:Configuration=Debug
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe --sql
```

`--sql` runs the workflows against real LocalDB databases with unique
`BarangayTests_...` names. It also checks reconnection, rollback, duplicate
receipts, stale writes, and once-only samples. The runner removes only the test
databases it creates; it never uses the configured application database.

If startup fails, check that LocalDB is installed and that `App.config` names
the correct instance. `SqlLocalDB info` lists local instances. The application
logs the underlying error to `%LOCALAPPDATA%\BarangayDocumentSystem\errors.log`.
It does not silently switch to temporary in-memory storage.

Database files live outside the source checkout and are excluded from Git.
Use SQL Server backup/restore to transfer or protect actual records; a Git
commit is not a database backup. Shared network deployment, database user
permissions, and scheduled backups remain separate work.
