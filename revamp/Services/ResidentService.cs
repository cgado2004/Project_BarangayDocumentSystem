using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Services
{
    public class ResidentService
    {
        private readonly IBarangayRepository repository;

        public ResidentService(IBarangayRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public List<Resident> Search(string searchText = "")
        {
            string term = (searchText ?? "").Trim();
            return repository.GetResidents()
                .Where(resident => (resident.FullName + " " + resident.Purok + " " + resident.ContactNumber)
                    .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(resident => resident.LastName).ThenBy(resident => resident.FirstName).ToList();
        }

        public Resident Get(int residentId) { return repository.GetResident(residentId); }

        public void Save(Resident resident)
        {
            ResidentValidator.Validate(resident);
            if (resident.ResidentId != 0)
            {
                var existing = repository.GetResident(resident.ResidentId);
                if (existing.HasUsedJobseekerBenefit && !resident.HasUsedJobseekerBenefit)
                    throw new InvalidOperationException("The first-time jobseeker benefit cannot be reset after it has been used.");
            }

            var clean = resident.Copy();
            clean.FirstName = clean.FirstName.Trim();
            clean.LastName = clean.LastName.Trim();
            clean.MiddleName = (clean.MiddleName ?? "").Trim();
            clean.Suffix = (clean.Suffix ?? "").Trim();
            clean.Address = clean.Address.Trim();
            clean.Purok = clean.Purok.Trim();
            clean.ContactNumber = clean.ContactNumber.Trim();
            clean.Occupation = (clean.Occupation ?? "").Trim();
            clean.DateOfBirth = clean.DateOfBirth.Date;
            clean.DateOfResidency = clean.DateOfResidency.Date;
            repository.SaveResident(clean);
            resident.ResidentId = clean.ResidentId;
            resident.Version = clean.Version;
        }

        public void Delete(int residentId) { repository.DeleteResident(residentId); }
    }
}
