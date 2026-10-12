// ---------------------------------------------------------------------------
//  CensusService.cs - counting the barangay.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    /// <summary>The headline numbers, in one object, so the census screen can
    /// show them at the top without walking the tables again.</summary>
    public class CensusSnapshot
    {
        public int ActiveResidents { get; set; }
        public int InactiveResidents { get; set; }
        public int ArchivedRecords { get; set; }
        public int Households { get; set; }
        public int Dependents { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public int Seniors { get; set; }
        public int PersonsWithDisability { get; set; }
        public int Indigent { get; set; }
        public int SoloParents { get; set; }
        public int FourPsBeneficiaries { get; set; }
        public int Students { get; set; }
        public int Newcomers { get; set; }
        public int Temporary { get; set; }
        public int Permanent { get; set; }
        public int BusinessOwners { get; set; }
        public int Voters { get; set; }

        /// <summary>The barangay's total population for the census: residents
        /// plus the dependents registered under them. The barangay counts
        /// people, not records, and a dependent is a person.</summary>
        public int TotalPopulation { get { return ActiveResidents + Dependents; } }

        public double AverageHouseholdSize
        {
            get { return Households == 0 ? 0d : Math.Round((double)TotalPopulation / Households, 2); }
        }
    }

    /// <summary>
    /// The census.
    ///
    /// The barangay keeps a population record every year, and the numbers it
    /// needs are always the same: how many people, how many households, how
    /// many by purok, by age, by sex, and how many belong to each group it
    /// serves (seniors, persons with disability, solo parents, 4Ps, students).
    ///
    /// Two decisions worth writing down. First, only ACTIVE residents are
    /// counted - a person who moved out or died is not part of the population,
    /// which is the practical reason the deactivate button exists at all.
    /// Second, the age brackets come from the Resident class, so the screen,
    /// the printed report and the census sheet can never disagree about what
    /// "working age" means.
    /// </summary>
    public class CensusService
    {
        private readonly IBarangayRepository _repository;

        public CensusService(IBarangayRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        /// <summary>The numbers at the top of the census screen.</summary>
        public CensusSnapshot GetSnapshot(string purok)
        {
            ResidentQuery query = new ResidentQuery();
            query.IncludeInactive = true;
            query.Purok = purok == null ? string.Empty : purok;

            IList<Resident> residents = _repository.GetResidents(query);

            CensusSnapshot snapshot = new CensusSnapshot();
            snapshot.Dependents = 0;

            foreach (Resident resident in residents)
            {
                if (resident.RecordState == RecordState.Inactive) snapshot.InactiveResidents++;
                if (resident.RecordState == RecordState.Archived) snapshot.ArchivedRecords++;
                if (resident.RecordState != RecordState.Active) continue;

                snapshot.ActiveResidents++;
                if (resident.Gender == Gender.Male) snapshot.Male++; else snapshot.Female++;
                if (resident.IsHeadOfFamily) snapshot.Households++;
                if (resident.HasClassification(ResidentClassification.SeniorCitizen)) snapshot.Seniors++;
                if (resident.HasClassification(ResidentClassification.PWD)) snapshot.PersonsWithDisability++;
                if (resident.HasClassification(ResidentClassification.Indigent)) snapshot.Indigent++;
                if (resident.HasClassification(ResidentClassification.SoloParent)) snapshot.SoloParents++;
                if (resident.HasClassification(ResidentClassification.FourPsBeneficiary)) snapshot.FourPsBeneficiaries++;
                if (resident.IsStudentFeeCategory) snapshot.Students++;
                if (resident.IsBusinessOwner) snapshot.BusinessOwners++;
                if (resident.IsRegisteredVoter) snapshot.Voters++;

                switch (resident.ResidencyStatus)
                {
                    case ResidencyStatus.Newcomer: snapshot.Newcomers++; break;
                    case ResidencyStatus.Temporary: snapshot.Temporary++; break;
                    case ResidencyStatus.Permanent: snapshot.Permanent++; break;
                }

                if (resident.IsHeadOfFamily)
                    snapshot.Dependents += _repository.GetDependents(resident.ResidentId).Count;
            }

            return snapshot;
        }

        /// <summary>Population per purok, which is the table the barangay
        /// actually posts on the wall.</summary>
        public DataTable GetPopulationByPurok(bool activeOnly)
        {
            return _repository.GetPopulationByPurok(activeOnly);
        }

        public DataTable GetPopulationByAge(string purok, bool activeOnly)
        {
            return _repository.GetPopulationByAgeBracket(purok, activeOnly);
        }

        /// <summary>Everything the census sheet holds: population, households,
        /// sex, classifications and residency status, per purok.</summary>
        public DataTable GetSummary(string purok)
        {
            return _repository.GetCensusSummary(purok);
        }

        /// <summary>
        /// The households, one row per head of family, with the household size
        /// worked out from the dependents on file.
        ///
        /// This is the report that proves the dependents registration does
        /// something: without it somebody would have to count people on paper
        /// again.
        /// </summary>
        public DataTable GetHouseholds(string purok)
        {
            DataTable table = new DataTable("households");
            table.Columns.Add("purok", typeof(string));
            table.Columns.Add("head_of_family", typeof(string));
            table.Columns.Add("classification", typeof(string));
            table.Columns.Add("dependents", typeof(int));
            table.Columns.Add("household_size", typeof(int));
            table.Columns.Add("dependents_list", typeof(string));

            ResidentQuery query = new ResidentQuery();
            query.IncludeInactive = false;
            query.Purok = purok == null ? string.Empty : purok;

            foreach (Resident resident in _repository.GetResidents(query))
            {
                if (!resident.IsHeadOfFamily) continue;

                IList<Dependent> dependents = _repository.GetDependents(resident.ResidentId);
                List<string> names = new List<string>();
                foreach (Dependent dependent in dependents)
                    names.Add(dependent.FullName + " (" + dependent.GetRelationText() + ", " + dependent.GetAge() + ")");

                DataRow row = table.NewRow();
                row["purok"] = resident.Purok;
                row["head_of_family"] = resident.GetFullName();
                row["classification"] = resident.GetClassificationText();
                row["dependents"] = dependents.Count;
                row["household_size"] = dependents.Count + 1;
                row["dependents_list"] = string.Join("; ", names.ToArray());
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>The registrations per purok, for the one-line summary above
        /// the census screen: "Purok Orchids has 21 residents in 8 households".</summary>
        public string Describe(string purok, CensusSnapshot snapshot)
        {
            string where = string.IsNullOrWhiteSpace(purok) ? "The whole barangay" : purok;

            return where + ": " + snapshot.ActiveResidents + " active resident(s), "
                 + snapshot.Households + " household(s), " + snapshot.Dependents + " dependent(s), "
                 + "average household size " + snapshot.AverageHouseholdSize + ".";
        }
    }
}
