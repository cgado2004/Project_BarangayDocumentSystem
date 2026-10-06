// ---------------------------------------------------------------------------
//  CrystalReportGateway.cs - talking to Crystal Reports without requiring it.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using BarangayDocumentSystem.Config;

namespace BarangayDocumentSystem.Services.Reports
{
    /// <summary>
    /// The bridge to SAP Crystal Reports.
    ///
    /// This needs explaining, because it looks odd at first: the system does not
    /// add a compile-time reference to the Crystal Reports assemblies. Instead
    /// it looks for them at run time by name and calls them through reflection.
    ///
    /// Why? Because of the criterion that this project has to build on any
    /// Visual Studio 2022 or newer, on any machine. A hard reference to
    /// CrystalDecisions.CrystalReports.Engine would mean the solution does not
    /// compile at all until somebody installs the SAP Crystal Reports runtime -
    /// and if a teammate opens the project the night before the defence on a
    /// machine without it, the whole solution shows errors.
    ///
    /// With this bridge:
    ///
    ///   * the solution ALWAYS compiles, on a clean machine;
    ///   * when the runtime IS installed and the .rpt file exists, the report
    ///     opens in the real CrystalReportViewer - the FR-17 requirement,
    ///     literally satisfied;
    ///   * when it is not, the report opens in the built-in viewer, which shows
    ///     the same rows from the same query.
    ///
    /// The assemblies it looks for are the three that ship with the developer
    /// version of Crystal Reports for Visual Studio:
    ///     CrystalDecisions.CrystalReports.Engine
    ///     CrystalDecisions.Shared
    ///     CrystalDecisions.Windows.Forms
    ///
    /// See docs/05-crystal-reports.md for how to install the runtime and draw
    /// the .rpt files (the field lists are written out there).
    /// </summary>
    public static class CrystalReportGateway
    {
        private const string EngineAssembly = "CrystalDecisions.CrystalReports.Engine";
        private const string SharedAssembly = "CrystalDecisions.Shared";
        private const string ViewerAssembly = "CrystalDecisions.Windows.Forms";

        private const string ReportDocumentType = "CrystalDecisions.CrystalReports.Engine.ReportDocument";
        private const string ViewerType = "CrystalDecisions.Windows.Forms.CrystalReportViewer";
        private const string ParameterFieldsType = "CrystalDecisions.Shared.ParameterFields";
        private const string ParameterFieldType = "CrystalDecisions.Shared.ParameterField";
        private const string ParameterValuesType = "CrystalDecisions.Shared.ParameterValues";
        private const string ParameterDiscreteValueType = "CrystalDecisions.Shared.ParameterDiscreteValue";

        private static bool? _installed;

        // ==================================================================
        //  Is Crystal there?
        // ==================================================================

        /// <summary>
        /// True when the Crystal runtime can be loaded on this machine.
        ///
        /// I remember the answer after the first look, because the reports
        /// screen asks this every time it redraws and loading assemblies is not
        /// free. The switch in App.config can also turn Crystal off on purpose,
        /// which is useful when a machine has an old runtime that misbehaves.
        /// </summary>
        public static bool IsRuntimeInstalled()
        {
            if (!AppConfig.UseCrystalReports) return false;
            if (_installed.HasValue) return _installed.Value;

            try
            {
                Assembly.Load(EngineAssembly);
                Assembly.Load(SharedAssembly);
                Assembly.Load(ViewerAssembly);
                _installed = true;
            }
            catch (Exception error)
            {
                // Not an error condition - most development machines do not have
                // it, and the built-in viewer is a complete fallback.
                AppLog.Info("SAP Crystal Reports is not installed on this machine, so reports will use "
                          + "the built-in viewer. (" + error.GetType().Name + ")");
                _installed = false;
            }

            return _installed.Value;
        }

        /// <summary>Forces the check again - used after somebody installs the
        /// runtime while the program is open, and by the settings screen.</summary>
        public static void ResetCache()
        {
            _installed = null;
        }

        /// <summary>The full path of a template, or empty when it is missing.</summary>
        public static string TemplatePath(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName)) return string.Empty;

            string folder = AppConfig.CrystalReportsPath;
            string path = Path.Combine(folder, templateName);

            if (File.Exists(path)) return path;

            // I also look next to the program itself, because that is where a
            // clerk would drop a file they were e-mailed.
            string beside = Path.Combine(AppConfig.ApplicationFolder, templateName);
            return File.Exists(beside) ? beside : string.Empty;
        }

        public static bool TemplateExists(string templateName)
        {
            return !string.IsNullOrEmpty(TemplatePath(templateName));
        }

        /// <summary>How many of the templates are on disk, for the reports
        /// screen's "is everything in place" line.</summary>
        public static int CountTemplatesPresent(IEnumerable<string> templateNames)
        {
            int found = 0;
            foreach (string name in templateNames)
                if (TemplateExists(name)) found++;

            return found;
        }

        // ==================================================================
        //  Building the report
        // ==================================================================

        /// <summary>
        /// Loads a template, binds the rows to it and returns the live
        /// ReportDocument - typed as object, because this project does not
        /// reference the Crystal assemblies.
        ///
        /// Everything below is reflection: find the type, find the method, call
        /// it. If any step fails I return null, and the caller falls back to the
        /// built-in viewer. A report that shows the rows is the goal; which
        /// engine draws them is a detail.
        /// </summary>
        public static object BuildReport(string templateName, DataTable rows, IDictionary<string, object> parameters)
        {
            if (!IsRuntimeInstalled()) return null;
            if (rows == null) return null;

            string path = TemplatePath(templateName);
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                Type documentType = FindType(EngineAssembly, ReportDocumentType);
                if (documentType == null) return null;

                object document = Activator.CreateInstance(documentType);

                // document.Load(path)
                Invoke(document, "Load", new object[] { path });

                // document.SetDataSource(rows)
                MethodInfo setDataSource = documentType.GetMethod("SetDataSource", new Type[] { typeof(DataTable) });

                if (setDataSource != null)
                    setDataSource.Invoke(document, new object[] { rows });
                else
                    Invoke(document, "SetDataSource", new object[] { rows });

                ApplyParameters(document, parameters);

                return document;
            }
            catch (Exception error)
            {
                AppLog.Error("Crystal Reports could not open " + templateName + ".", error);
                return null;
            }
        }

        /// <summary>
        /// Passes the report's parameters (the date range, the purok, the
        /// status) into the template, so the .rpt can print a heading that says
        /// what period it covers.
        /// </summary>
        private static void ApplyParameters(object document, IDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            try
            {
                PropertyInfo fieldsProperty = document.GetType().GetProperty("ParameterFields");
                if (fieldsProperty == null) return;

                object fields = fieldsProperty.GetValue(document, null);
                if (fields == null) return;

                Type fieldsType = fields.GetType();
                MethodInfo add = fieldsType.GetMethod("Add", new Type[] { FindType(SharedAssembly, ParameterFieldType) });

                Type fieldType = FindType(SharedAssembly, ParameterFieldType);
                Type valuesType = FindType(SharedAssembly, ParameterValuesType);
                Type valueType = FindType(SharedAssembly, ParameterDiscreteValueType);
                if (fieldType == null || valuesType == null || valueType == null || add == null) return;

                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    object field = Activator.CreateInstance(fieldType);
                    fieldType.GetProperty("Name").SetValue(field, parameter.Key, null);

                    object values = Activator.CreateInstance(valuesType);
                    object value = Activator.CreateInstance(valueType);
                    valueType.GetProperty("Value").SetValue(value, parameter.Value, null);

                    valuesType.GetMethod("Add").Invoke(values, new object[] { value });
                    fieldType.GetProperty("CurrentValues").SetValue(field, values, null);

                    add.Invoke(fields, new object[] { field });
                }
            }
            catch (Exception error)
            {
                // A template with no parameters of that name is not a problem;
                // it simply prints without them.
                AppLog.Warn("The report parameters were not applied: " + error.Message);
            }
        }

        /// <summary>
        /// Creates the CrystalReportViewer control, already showing a document.
        ///
        /// It comes back as a plain Control, so the reports screen can host it
        /// inside an ordinary panel without knowing anything about Crystal. When
        /// this returns null - no runtime, or no template - the screen shows the
        /// built-in grid instead, and nobody sees an error.
        /// </summary>
        public static System.Windows.Forms.Control CreateViewer(object reportDocument)
        {
            if (reportDocument == null) return null;

            try
            {
                Type viewerType = FindType(ViewerAssembly, ViewerType);
                if (viewerType == null) return null;

                System.Windows.Forms.Control viewer =
                    (System.Windows.Forms.Control)Activator.CreateInstance(viewerType);

                viewer.Dock = System.Windows.Forms.DockStyle.Fill;

                PropertyInfo reportSource = viewerType.GetProperty("ReportSource");
                if (reportSource != null) reportSource.SetValue(viewer, reportDocument, null);

                PropertyInfo toolbar = viewerType.GetProperty("ToolbarPanelVisible");
                if (toolbar != null) toolbar.SetValue(viewer, true, null);

                PropertyInfo zoom = viewerType.GetProperty("Zoom");
                if (zoom != null && zoom.PropertyType == typeof(int)) zoom.SetValue(viewer, 100, null);

                return viewer;
            }
            catch (Exception error)
            {
                AppLog.Error("The Crystal Reports viewer could not be created.", error);
                return null;
            }
        }

        /// <summary>Closes the report document and lets the template go. The
        /// Crystal viewer holds file locks, and a locked .rpt cannot be
        /// replaced by an updated one.</summary>
        public static void CloseReport(object reportDocument)
        {
            if (reportDocument == null) return;

            try
            {
                Invoke(reportDocument, "Close", null);
                Invoke(reportDocument, "Dispose", null);
            }
            catch (Exception error)
            {
                AppLog.Warn("The Crystal report document did not close cleanly: " + error.Message);
            }
        }

        // ==================================================================
        //  Reflection helpers
        // ==================================================================

        private static Type FindType(string assemblyName, string typeName)
        {
            try
            {
                Assembly assembly = Assembly.Load(assemblyName);
                Type found = assembly.GetType(typeName, false, true);
                if (found != null) return found;

                // Some builds put the type in a different namespace revision,
                // so I fall back to a search by simple name.
                string simple = typeName.Substring(typeName.LastIndexOf('.') + 1);
                foreach (Type candidate in assembly.GetTypes())
                    if (string.Equals(candidate.Name, simple, StringComparison.Ordinal)) return candidate;

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object Invoke(object target, string methodName, object[] arguments)
        {
            if (target == null) return null;

            MethodInfo method = arguments == null
                ? target.GetType().GetMethod(methodName, Type.EmptyTypes)
                : target.GetType().GetMethod(methodName);

            if (method == null) return null;
            return method.Invoke(target, arguments);
        }
    }
}
