# Barangay Document System

A Windows Forms project for managing barangay residents and document requests.
This starter currently opens an empty main window. Resident records, requests,
fees, and printing will be added in small steps.

## Run the project

Open `BarangayDocumentSystem.slnx` in Visual Studio with the .NET desktop
development workload and .NET Framework 4.7.2 targeting pack installed.
Press F5 to run.

## Current layout

```text
BarangayDocumentSystem/
|-- Forms/
|   |-- MainForm.cs           Window events and behavior
|   `-- MainForm.Designer.cs  Layout managed by the Windows Forms designer
|-- Properties/              Assembly information, resources, and settings
|-- App.config               Runtime configuration
`-- Program.cs               Application startup
```

Keep class names and filenames consistent, and match namespaces to folders.
Use descriptive names such as `MainForm` and `SaveResident`, with camelCase
for local variables and parameters. Add folders when they have a purpose.

Forms will handle user interaction. Business rules and data access will live
in separate classes as those features are added. Validate input before saving
and show clear messages for errors the user can correct. Keep comments short
and use them where the reason for the code would otherwise be unclear.
