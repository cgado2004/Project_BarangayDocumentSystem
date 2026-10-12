// ---------------------------------------------------------------------------
//  RepositoryBase.cs - the parts of a store that are the same everywhere.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    /// <summary>
    /// The work both stores do the same way.
    ///
    /// The in-memory store (the demo, and what my rule checks run against)
    /// still has to answer the reporting questions with the same column names
    /// the SQL store returns, or the report screen would need two code paths.
    /// So the table building lives here, in one place, and both stores use it.
    ///
    /// Rule I keep: the column names in these tables match the stored
    /// procedures exactly. If I rename one here, the reports screen goes
    /// blank, so this file and the two procedure scripts have to agree.
    /// </summary>
    public abstract class RepositoryBase
    {
        // ==================================================================
        //  Reference numbers
        // ==================================================================

        /// <summary>
        /// The next counter number for the year, worked out from the requests
        /// already on file. The SQL store does this with a counter table
        /// inside a transaction; here the list is the only thing there is.
        /// </summary>
        protected static string BuildReferenceNumber(int year, int sequence)
        {
            return year.ToString("0000") + "-" + sequence.ToString("000000");
        }

        protected static int NextSequence(IEnumerable<DocumentRequest> existing, int year)
        {
            int highest = 0;
            string prefix = year.ToString("0000") + "-";

            foreach (DocumentRequest request in existing)
            {
                if (request.ReferenceNumber == null) continue;
                if (!request.ReferenceNumber.StartsWith(prefix, StringComparison.Ordinal)) continue;

                int parsed;
                string tail = request.ReferenceNumber.Substring(prefix.Length);
                if (int.TryParse(tail, out parsed) && parsed > highest) highest = parsed;
            }

            return highest + 1;
        }

        // ==================================================================
        //  Report tables
        // ==================================================================

        /// <summary>The dashboard's list of numbers, in the order the screen
        /// shows them.</summary>
        protected static DataTable BuildDashboardTable(
            IList<Resident> residents, IList<Dependent> dependents, IList<DocumentRequest> requests,
            IList<OfficialReceipt> receipts, int activityToday,
            DateTime from, DateTime to)
        {
            DataTable table = NewTable("dashboard",
                new string[] { "metric", "value" },
                new Type[] { typeof(string), typeof(decimal) });

            AddRow(table, "Active residents", residents.Count(r => r.RecordState == RecordState.Active));
            AddRow(table, "Inactive residents", residents.Count(r => r.RecordState == RecordState.Inactive));
            AddRow(table, "Archived records", residents.Count(r => r.RecordState == RecordState.Archived));
            AddRow(table, "Households", residents.Count(r => r.IsHeadOfFamily && r.RecordState == RecordState.Active));
            AddRow(table, "Dependents on file", dependents.Count);
            AddRow(table, "Requests filed today", requests.Count(r => r.DateRequested.Date == DateTime.Today));
            AddRow(table, "Requests waiting", requests.Count(r => r.Status == RequestStatus.Pending));
            AddRow(table, "Requests being processed", requests.Count(r => r.Status == RequestStatus.Processing));
            AddRow(table, "Cleared requests", requests.Count(r => r.Status == RequestStatus.Cleared));
            AddRow(table, "Ready for release", requests.Count(r => r.Status == RequestStatus.ReadyForRelease));
            AddRow(table, "Released today",
                requests.Count(r => r.DateReleased.HasValue && r.DateReleased.Value.Date == DateTime.Today));
            AddRow(table, "Documents released in the period",
                requests.Count(r => r.DateReleased.HasValue && InRange(r.DateReleased.Value, from, to)));
            AddRow(table, "Collected in the period",
                receipts.Where(r => !r.IsVoid && InRange(r.OrDate, from, to)).Sum(r => r.Amount));
            AddRow(table, "Collected today",
                receipts.Where(r => !r.IsVoid && r.OrDate.Date == DateTime.Today).Sum(r => r.Amount));
            AddRow(table, "Issued free in the period",
                requests.Count(r => r.Fee <= 0m && r.Status == RequestStatus.Released
                                    && r.DateReleased.HasValue && InRange(r.DateReleased.Value, from, to)));
            AddRow(table, "Activity entries today", activityToday);

            return table;
        }

        protected static DataTable BuildPerDayTable(IList<DocumentRequest> requests,
                                                   DateTime from, DateTime to,
                                                   RequestStatus? status, string purok,
                                                   IDictionary<int, Resident> residentsById)
        {
            DataTable table = NewTable("per_day",
                new string[] { "txn_date", "filed", "released", "rejected", "waiting", "free_issued", "collected" },
                new Type[] { typeof(DateTime), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(decimal) });

            IList<DocumentRequest> filtered = requests.Where(r =>
                InRange(r.DateRequested, from, to)
                && (!status.HasValue || r.Status == status.Value)
                && (string.IsNullOrWhiteSpace(purok) || InPurok(residentsById, r.ResidentId, purok)))
                .ToList();

            IEnumerable<IGrouping<DateTime, DocumentRequest>> days =
                filtered.GroupBy(r => r.DateRequested.Date).OrderBy(g => g.Key);

            foreach (IGrouping<DateTime, DocumentRequest> day in days)
            {
                DataRow row = table.NewRow();
                row["txn_date"] = day.Key;
                row["filed"] = day.Count();
                row["released"] = day.Count(r => r.Status == RequestStatus.Released);
                row["rejected"] = day.Count(r => r.Status == RequestStatus.Rejected);
                row["waiting"] = day.Count(r => r.Status == RequestStatus.Pending);
                row["free_issued"] = day.Count(r => r.Fee <= 0m);
                row["collected"] = day.Where(r => r.IsPaid).Sum(r => r.Fee);
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildRegisterTable(IList<DocumentRequest> requests,
                                                     DateTime from, DateTime to, RequestStatus? status)
        {
            DataTable table = NewTable("document_register",
                new string[]
                {
                    "reference_number", "date_requested", "first_name", "last_name", "purok",
                    "document_type", "purpose", "status", "fee", "is_paid",
                    "official_receipt_no", "date_released"
                },
                new Type[]
                {
                    typeof(string), typeof(DateTime), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(string), typeof(string), typeof(decimal), typeof(int),
                    typeof(string), typeof(DateTime)
                });

            foreach (DocumentRequest request in requests
                .Where(r => InRange(r.DateRequested, from, to))
                .Where(r => !status.HasValue || r.Status == status.Value)
                .OrderBy(r => r.DateRequested))
            {
                DataRow row = table.NewRow();
                row["reference_number"] = request.ReferenceNumber;
                row["date_requested"] = request.DateRequested;
                row["first_name"] = FirstNameFrom(request.ResidentName);
                row["last_name"] = LastNameFrom(request.ResidentName);
                row["purok"] = string.Empty;
                row["document_type"] = request.GetDocumentName();
                row["purpose"] = request.Purpose;
                row["status"] = request.GetStatusText();
                row["fee"] = request.Fee;
                row["is_paid"] = request.IsPaid ? 1 : 0;
                row["official_receipt_no"] = request.OfficialReceiptNumber;
                row["date_released"] = (object)request.DateReleased ?? DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildCollectionsTable(IList<OfficialReceipt> receipts, DateTime from, DateTime to)
        {
            DataTable table = NewTable("collections",
                new string[]
                {
                    "or_date", "or_number", "series_code", "control_number", "payer_name",
                    "amount", "method", "collected_by", "is_void", "void_reason"
                },
                new Type[]
                {
                    typeof(DateTime), typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(decimal), typeof(string), typeof(string), typeof(int), typeof(string)
                });

            foreach (OfficialReceipt receipt in receipts
                .Where(r => InRange(r.OrDate, from, to))
                .OrderBy(r => r.OrDate).ThenBy(r => r.OrNumber))
            {
                DataRow row = table.NewRow();
                row["or_date"] = receipt.OrDate;
                row["or_number"] = receipt.OrNumber;
                row["series_code"] = receipt.SeriesCode;
                row["control_number"] = receipt.ControlNumber;
                row["payer_name"] = receipt.PayerName;
                row["amount"] = receipt.Amount;
                row["method"] = receipt.GetMethodText();
                row["collected_by"] = receipt.CollectedBy;
                row["is_void"] = receipt.IsVoid ? 1 : 0;
                row["void_reason"] = receipt.VoidReason;
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildBusinessTable(IList<DocumentRequest> requests, DateTime from, DateTime to)
        {
            DataTable table = NewTable("business",
                new string[]
                {
                    "reference_number", "date_requested", "business_name", "business_nature",
                    "business_purok", "business_ownership", "business_employees", "status",
                    "fee", "is_paid", "official_receipt_no", "first_name", "last_name"
                },
                new Type[]
                {
                    typeof(string), typeof(DateTime), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(int), typeof(string), typeof(decimal), typeof(int),
                    typeof(string), typeof(string), typeof(string)
                });

            foreach (DocumentRequest request in requests
                .Where(r => r.DocumentType == DocumentType.BarangayBusinessClearance)
                .Where(r => InRange(r.DateRequested, from, to))
                .OrderBy(r => r.DateRequested))
            {
                BusinessDetails business = request.Business ?? new BusinessDetails();

                DataRow row = table.NewRow();
                row["reference_number"] = request.ReferenceNumber;
                row["date_requested"] = request.DateRequested;
                row["business_name"] = business.BusinessName;
                row["business_nature"] = business.NatureOfBusiness;
                row["business_purok"] = business.Purok;
                row["business_ownership"] = business.OwnershipType;
                row["business_employees"] = business.EmployeeCount;
                row["status"] = request.GetStatusText();
                row["fee"] = request.Fee;
                row["is_paid"] = request.IsPaid ? 1 : 0;
                row["official_receipt_no"] = request.OfficialReceiptNumber;
                row["first_name"] = FirstNameFrom(request.ResidentName);
                row["last_name"] = LastNameFrom(request.ResidentName);
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildCensusTable(IList<Resident> residents, string purok)
        {
            DataTable table = NewTable("census",
                new string[]
                {
                    "purok", "residents", "households", "male", "female", "seniors", "pwd",
                    "indigent", "solo_parent", "four_ps", "students", "newcomers", "temporary", "permanent"
                },
                new Type[]
                {
                    typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                    typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                    typeof(int), typeof(int)
                });

            IEnumerable<Resident> active = residents.Where(r => r.RecordState == RecordState.Active)
                .Where(r => string.IsNullOrWhiteSpace(purok) || string.Equals(r.Purok, purok, StringComparison.OrdinalIgnoreCase));

            foreach (IGrouping<string, Resident> group in active.GroupBy(r => r.Purok).OrderBy(g => g.Key))
            {
                DataRow row = table.NewRow();
                row["purok"] = group.Key;
                row["residents"] = group.Count();
                row["households"] = group.Count(r => r.IsHeadOfFamily);
                row["male"] = group.Count(r => r.Gender == Gender.Male);
                row["female"] = group.Count(r => r.Gender == Gender.Female);
                row["seniors"] = group.Count(r => r.HasClassification(ResidentClassification.SeniorCitizen));
                row["pwd"] = group.Count(r => r.HasClassification(ResidentClassification.PWD));
                row["indigent"] = group.Count(r => r.HasClassification(ResidentClassification.Indigent));
                row["solo_parent"] = group.Count(r => r.HasClassification(ResidentClassification.SoloParent));
                row["four_ps"] = group.Count(r => r.HasClassification(ResidentClassification.FourPsBeneficiary));
                row["students"] = group.Count(r => r.IsStudentFeeCategory);
                row["newcomers"] = group.Count(r => r.ResidencyStatus == ResidencyStatus.Newcomer);
                row["temporary"] = group.Count(r => r.ResidencyStatus == ResidencyStatus.Temporary);
                row["permanent"] = group.Count(r => r.ResidencyStatus == ResidencyStatus.Permanent);
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildPopulationByPurokTable(IList<Resident> residents, bool activeOnly)
        {
            DataTable table = NewTable("population_by_purok",
                new string[] { "purok", "residents", "households", "male", "female" },
                new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int) });

            IEnumerable<Resident> wanted = activeOnly
                ? residents.Where(r => r.RecordState == RecordState.Active)
                : residents;

            foreach (IGrouping<string, Resident> group in wanted.GroupBy(r => r.Purok).OrderByDescending(g => g.Count()))
            {
                DataRow row = table.NewRow();
                row["purok"] = group.Key;
                row["residents"] = group.Count();
                row["households"] = group.Count(r => r.IsHeadOfFamily);
                row["male"] = group.Count(r => r.Gender == Gender.Male);
                row["female"] = group.Count(r => r.Gender == Gender.Female);
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildPopulationByAgeTable(IList<Resident> residents, string purok, bool activeOnly)
        {
            DataTable table = NewTable("population_by_age",
                new string[] { "age_bracket", "male", "female", "total" },
                new Type[] { typeof(string), typeof(int), typeof(int), typeof(int) });

            IEnumerable<Resident> wanted = residents
                .Where(r => !activeOnly || r.RecordState == RecordState.Active)
                .Where(r => string.IsNullOrWhiteSpace(purok) || string.Equals(r.Purok, purok, StringComparison.OrdinalIgnoreCase));

            List<Resident> list = wanted.ToList();

            for (int bucket = 0; bucket < Resident.CensusAgeLabels.Length; bucket++)
            {
                int index = bucket;
                List<Resident> inBracket = list.Where(r => Resident.CensusAgeBucket(r.GetAge()) == index).ToList();

                DataRow row = table.NewRow();
                row["age_bracket"] = Resident.CensusAgeLabels[bucket];
                row["male"] = inBracket.Count(r => r.Gender == Gender.Male);
                row["female"] = inBracket.Count(r => r.Gender == Gender.Female);
                row["total"] = inBracket.Count;
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildMasterListTable(IList<Resident> residents, string purok,
                                                       RecordState? state, ResidencyStatus? residency)
        {
            DataTable table = NewTable("resident_master",
                new string[]
                {
                    "last_name", "first_name", "middle_name", "suffix", "purok", "date_of_birth",
                    "gender", "civil_status", "contact_number", "occupation", "classification",
                    "is_student_fee_category", "is_business_owner", "is_head_of_family",
                    "residency_status", "record_state", "date_of_residency"
                },
                new Type[]
                {
                    typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(DateTime), typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(int), typeof(int), typeof(int), typeof(string), typeof(string),
                    typeof(DateTime)
                });

            foreach (Resident resident in residents
                .Where(r => string.IsNullOrWhiteSpace(purok) || string.Equals(r.Purok, purok, StringComparison.OrdinalIgnoreCase))
                .Where(r => !state.HasValue || r.RecordState == state.Value)
                .Where(r => !residency.HasValue || r.ResidencyStatus == residency.Value)
                .OrderBy(r => r.Purok).ThenBy(r => r.LastName).ThenBy(r => r.FirstName))
            {
                DataRow row = table.NewRow();
                row["last_name"] = resident.LastName;
                row["first_name"] = resident.FirstName;
                row["middle_name"] = resident.MiddleName;
                row["suffix"] = resident.Suffix;
                row["purok"] = resident.Purok;
                row["date_of_birth"] = resident.DateOfBirth;
                row["gender"] = resident.Gender.ToString();
                row["civil_status"] = resident.CivilStatus.ToString();
                row["contact_number"] = resident.ContactNumber;
                row["occupation"] = resident.Occupation;
                row["classification"] = resident.GetClassificationText();
                row["is_student_fee_category"] = resident.IsStudentFeeCategory ? 1 : 0;
                row["is_business_owner"] = resident.IsBusinessOwner ? 1 : 0;
                row["is_head_of_family"] = resident.IsHeadOfFamily ? 1 : 0;
                row["residency_status"] = EnumText.Of(resident.ResidencyStatus);
                row["record_state"] = EnumText.Of(resident.RecordState);
                row["date_of_residency"] = resident.DateOfResidency;
                table.Rows.Add(row);
            }

            return table;
        }

        protected static DataTable BuildActivityTable(IList<ActivityLogEntry> entries)
        {
            DataTable table = NewTable("activity",
                new string[]
                {
                    "occurred_on", "username", "role", "module", "action",
                    "target_type", "target_reference", "details"
                },
                new Type[]
                {
                    typeof(DateTime), typeof(string), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(string), typeof(string)
                });

            foreach (ActivityLogEntry entry in entries.OrderByDescending(e => e.OccurredOn))
            {
                DataRow row = table.NewRow();
                row["occurred_on"] = entry.OccurredOn;
                row["username"] = entry.Username;
                row["role"] = EnumText.Of(entry.Role);
                row["module"] = entry.GetModuleText();
                row["action"] = entry.Action;
                row["target_type"] = entry.TargetType;
                row["target_reference"] = entry.TargetReference;
                row["details"] = entry.Details;
                table.Rows.Add(row);
            }

            return table;
        }

        // ==================================================================
        //  Small helpers
        // ==================================================================

        private static DataTable NewTable(string name, string[] columns, Type[] types)
        {
            DataTable table = new DataTable(name);
            for (int i = 0; i < columns.Length; i++) table.Columns.Add(columns[i], types[i]);
            return table;
        }

        private static void AddRow(DataTable table, string metric, decimal value)
        {
            DataRow row = table.NewRow();
            row["metric"] = metric;
            row["value"] = value;
            table.Rows.Add(row);
        }

        protected static bool InRange(DateTime value, DateTime from, DateTime to)
        {
            return value.Date >= from.Date && value.Date <= to.Date;
        }

        private static bool InPurok(IDictionary<int, Resident> residentsById, int residentId, string purok)
        {
            if (residentsById == null || string.IsNullOrWhiteSpace(purok)) return true;

            Resident resident;
            if (!residentsById.TryGetValue(residentId, out resident)) return false;
            return string.Equals(resident.Purok, purok, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The register uses the first and last name separately, and
        /// the request only carries the joined name, so I split it back on the
        /// last space. Good enough for a printed register, and it saves a
        /// second lookup on every row.</summary>
        private static string FirstNameFrom(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            int lastSpace = fullName.Trim().LastIndexOf(' ');
            return lastSpace <= 0 ? fullName.Trim() : fullName.Trim().Substring(0, lastSpace);
        }

        private static string LastNameFrom(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            int lastSpace = fullName.Trim().LastIndexOf(' ');
            return lastSpace <= 0 ? string.Empty : fullName.Trim().Substring(lastSpace + 1);
        }
    }
}
