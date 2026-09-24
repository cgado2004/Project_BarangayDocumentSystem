# Barangay Document System

A Windows Forms app for Barangay Magugpo Poblacion, City of Tagum.
Built with .NET Framework 4.7.2 and SQL Server Express LocalDB.

## Run

1. Open `BarangayDocumentSystem.sln` in Visual Studio 2022 or newer.
2. Install the components listed in `.vsconfig`: .NET desktop development,
   the .NET Framework 4.7.2 targeting pack, and SQL Server Express LocalDB.
3. Set `BarangayDocumentSystem` as the startup project, build, and press F5.

The app creates its database on first launch. Saved records remain after
closing the app or pulling new code. Each Windows account has its own local
database; classmates' laptops do not share records.

`App.config` contains the barangay details, database connection, and sample
switch. Set `LoadSampleData=false` before the first run for an empty database.
Otherwise, fictional samples load once. Changing this setting later does not
reset records.

## Features

- Resident registration, editing, and search.
- Document requests, fees, receipts, and release tracking.
- Dashboard totals, document preview, and printing.
- Saved request history and protection against duplicate receipts.
- Navy theme: white sidebar with the barangay seal, hero banner, six stat
  cards, purok chips, and document-type bars.

![Dashboard](docs/dashboard-preview.png)

The dashboard above is captured from the running app at 1360×860 with
sample data.

## Checks

Run these from a Visual Studio Developer PowerShell:

```powershell
MSBuild Tests\BarangayDocumentSystem.Tests.csproj /p:Configuration=Debug
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe
.\Tests\bin\Debug\BarangayDocumentSystem.Tests.exe --sql
```

The SQL checks use separate temporary databases. They do not change the app's
records. Runtime checks do not replace testing the Visual Studio designers.

## Help

- [Database setup](docs/Database.md)
- [Code guide](docs/CodeGuide.md)
- [Demo walkthrough](docs/Walkthrough.md)
- [Fee notes](docs/FeePolicy.md)

For F5 issues, check **Project Properties > Debug > Start project**. For database
issues, confirm LocalDB is installed and check
`%LOCALAPPDATA%\BarangayDocumentSystem\errors.log`.
