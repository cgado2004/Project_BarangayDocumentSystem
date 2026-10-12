// ---------------------------------------------------------------------------
//  ReportDefinitions.cs - the list of reports the system can produce.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Interfaces;

namespace BarangayDocumentSystem.Services.Reports
{
    /// <summary>
    /// The reports menu, in one place.
    ///
    /// Each entry names the Crystal Reports template that belongs to it. That
    /// matters because of how the reports screen decides what to show: when the
    /// SAP Crystal Reports runtime is installed on the machine and the .rpt
    /// file is in the reports folder, the report opens in the Crystal viewer;
    /// when it is not, the same rows open in the built-in viewer and print
    /// through the built-in printer.
    ///
    /// I built it that way because of something practical: a barangay computer
    /// may or may not have the Crystal runtime installed, and a report that
    /// simply refuses to open would be worse than useless on the day the
    /// punong barangay asks for last month's collections. So the rows are
    /// ALWAYS available, and Crystal is the nicer way to look at them.
    ///
    /// See docs/05-crystal-reports.md for the field list of every template, so
    /// each .rpt can be drawn in the Crystal designer in a few minutes.
    /// </summary>
    public static class ReportDefinitions
    {
        public const string DailyTransactions = "daily";
        public const string DocumentRegister = "register";
        public const string Collections = "collections";
        public const string CensusSummary = "census";
        public const string PopulationByAge = "age";
        public const string PopulationByPurok = "purok";
        public const string ResidentsMasterList = "residents";
        public const string HouseholdsAndDependents = "households";
        public const string BusinessClearances = "business";
        public const string ActivityLog = "activity";
        public const string FeeSchedule = "fees";

        /// <summary>Every report, in the order the screen lists them: the
        /// counter reports first, then the population ones, then the audit
        /// ones.</summary>
        public static IList<ReportDefinition> All()
        {
            List<ReportDefinition> reports = new List<ReportDefinition>();

            reports.Add(Make(DailyTransactions, "Daily Transactions",
                "Every transaction filed on each day in the period, with what was released, "
                + "what is still waiting, what was issued free and how much was collected that day.",
                "DailyTransactions.rpt", true, false, true));

            reports.Add(Make(DocumentRegister, "Document Register",
                "Every request in the period, one row each, with its status and receipt - the "
                + "register the barangay keeps for its own records.",
                "DocumentRegister.rpt", true, false, true));

            reports.Add(Make(Collections, "Collections and Official Receipts",
                "The collection register: every official receipt written in the period, with its "
                + "series, its control number and who collected it.",
                "Collections.rpt", true, false, false));

            reports.Add(Make(BusinessClearances, "Business Clearances",
                "Every Barangay Business Clearance in the period, with the name of the business, "
                + "what it does and the purok it operates in.",
                "BusinessClearances.rpt", true, true, false));

            reports.Add(Make(CensusSummary, "Barangay Census Summary",
                "Population per purok with households, sex, age groups and every classification "
                + "the barangay keeps - the census sheet, ready to print.",
                "CensusSummary.rpt", false, true, false));

            reports.Add(Make(PopulationByAge, "Population by Age Group",
                "How many residents fall in each age group, male and female, for one purok or "
                + "for the whole barangay.",
                "PopulationByAge.rpt", false, true, false));

            reports.Add(Make(PopulationByPurok, "Population by Purok",
                "The population of each purok, with its households and its sex breakdown.",
                "PopulationByPurok.rpt", false, false, false));

            reports.Add(Make(ResidentsMasterList, "Residents Master List",
                "The registry itself: every resident with purok, age, classification, fee category "
                + "and active/inactive status. This is the report I take when the barangay asks "
                + "\"who is on file?\".",
                "ResidentMasterList.rpt", false, true, false));

            reports.Add(Make(HouseholdsAndDependents, "Households and Dependents",
                "One row per household: the head of the family, the dependents registered under "
                + "them, and how many people the household has.",
                "Households.rpt", false, true, false));

            reports.Add(Make(ActivityLog, "Activity Log",
                "Who did what, when - every sign-in, registration, filing, collection and change, "
                + "with the user name in the row.",
                "ActivityLog.rpt", true, false, false));

            reports.Add(Make(FeeSchedule, "Fee Schedule (Citizen's Charter)",
                "The fees the barangay charges and the basis behind each one, laid out the way the "
                + "Citizen's Charter posts them.",
                "FeeSchedule.rpt", false, false, false));

            return reports;
        }

        public static ReportDefinition Find(string key)
        {
            foreach (ReportDefinition definition in All())
                if (string.Equals(definition.Key, key, StringComparison.OrdinalIgnoreCase))
                    return definition;

            return null;
        }

        private static ReportDefinition Make(string key, string title, string description,
                                            string crystalTemplate, bool needsDates, bool needsPurok,
                                            bool needsStatus)
        {
            ReportDefinition definition = new ReportDefinition();
            definition.Key = key;
            definition.Title = title;
            definition.Description = description;
            definition.CrystalTemplate = crystalTemplate;
            definition.NeedsDateRange = needsDates;
            definition.NeedsPurok = needsPurok;
            definition.NeedsStatus = needsStatus;
            return definition;
        }
    }
}
