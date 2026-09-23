using System;
using System.Data;
using System.Data.SqlClient;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    internal static class SqlResidentMapping
    {
        internal const string Columns =
            "FirstName, MiddleName, LastName, " +
            "Suffix, DateOfBirth, Gender, " +
            "CivilStatus, Purok, Address, " +
            "ContactNumber, Occupation, DateOfResidency, " +
            "IsRegisteredVoter, IsSeniorCitizen, IsPersonWithDisability, " +
            "IsIndigent, IsStudent, IsSoloParent, " +
            "HasUsedJobseekerBenefit";
        internal const string Parameters =
            "@FirstName, @MiddleName, @LastName, " +
            "@Suffix, @DateOfBirth, @Gender, " +
            "@CivilStatus, @Purok, @Address, " +
            "@ContactNumber, @Occupation, @DateOfResidency, " +
            "@IsRegisteredVoter, @IsSeniorCitizen, @IsPersonWithDisability, " +
            "@IsIndigent, @IsStudent, @IsSoloParent, " +
            "@HasUsedJobseekerBenefit";
        internal const string Assignments =
            "FirstName = @FirstName, MiddleName = @MiddleName, LastName = @LastName, " +
            "Suffix = @Suffix, DateOfBirth = @DateOfBirth, Gender = @Gender, " +
            "CivilStatus = @CivilStatus, Purok = @Purok, Address = @Address, " +
            "ContactNumber = @ContactNumber, Occupation = @Occupation, DateOfResidency = @DateOfResidency, " +
            "IsRegisteredVoter = @IsRegisteredVoter, IsSeniorCitizen = @IsSeniorCitizen, IsPersonWithDisability = @IsPersonWithDisability, " +
            "IsIndigent = @IsIndigent, IsStudent = @IsStudent, IsSoloParent = @IsSoloParent, " +
            "HasUsedJobseekerBenefit = @HasUsedJobseekerBenefit";

        internal static void AddParameters(SqlCommand command, Resident record)
        {
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 80).Value = record.FirstName ?? "";
            command.Parameters.Add("@MiddleName", SqlDbType.NVarChar, 80).Value = record.MiddleName ?? "";
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 80).Value = record.LastName ?? "";
            command.Parameters.Add("@Suffix", SqlDbType.NVarChar, 20).Value = record.Suffix ?? "";
            command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = record.DateOfBirth;
            command.Parameters.Add("@Gender", SqlDbType.Int).Value = (int)record.Gender;
            command.Parameters.Add("@CivilStatus", SqlDbType.Int).Value = (int)record.CivilStatus;
            command.Parameters.Add("@Purok", SqlDbType.NVarChar, 60).Value = record.Purok ?? "";
            command.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = record.Address ?? "";
            command.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 15).Value = record.ContactNumber ?? "";
            command.Parameters.Add("@Occupation", SqlDbType.NVarChar, 100).Value = record.Occupation ?? "";
            command.Parameters.Add("@DateOfResidency", SqlDbType.Date).Value = record.DateOfResidency;
            command.Parameters.Add("@IsRegisteredVoter", SqlDbType.Bit).Value = record.IsRegisteredVoter;
            command.Parameters.Add("@IsSeniorCitizen", SqlDbType.Bit).Value = record.IsSeniorCitizen;
            command.Parameters.Add("@IsPersonWithDisability", SqlDbType.Bit).Value = record.IsPersonWithDisability;
            command.Parameters.Add("@IsIndigent", SqlDbType.Bit).Value = record.IsIndigent;
            command.Parameters.Add("@IsStudent", SqlDbType.Bit).Value = record.IsStudent;
            command.Parameters.Add("@IsSoloParent", SqlDbType.Bit).Value = record.IsSoloParent;
            command.Parameters.Add("@HasUsedJobseekerBenefit", SqlDbType.Bit).Value = record.HasUsedJobseekerBenefit;
        }

        internal static Resident Read(SqlDataReader reader, bool snapshot = false)
        {
            return new Resident
            {
                ResidentId = (int)reader["ResidentId"],
                Version = snapshot ? 0 : (int)reader["Version"],
                FirstName = (string)reader["FirstName"],
                MiddleName = (string)reader["MiddleName"],
                LastName = (string)reader["LastName"],
                Suffix = (string)reader["Suffix"],
                DateOfBirth = (DateTime)reader["DateOfBirth"],
                Gender = (Gender)(int)reader["Gender"],
                CivilStatus = (CivilStatus)(int)reader["CivilStatus"],
                Purok = (string)reader["Purok"],
                Address = (string)reader["Address"],
                ContactNumber = (string)reader["ContactNumber"],
                Occupation = (string)reader["Occupation"],
                DateOfResidency = (DateTime)reader["DateOfResidency"],
                IsRegisteredVoter = (bool)reader["IsRegisteredVoter"],
                IsSeniorCitizen = (bool)reader["IsSeniorCitizen"],
                IsPersonWithDisability = (bool)reader["IsPersonWithDisability"],
                IsIndigent = (bool)reader["IsIndigent"],
                IsStudent = (bool)reader["IsStudent"],
                IsSoloParent = (bool)reader["IsSoloParent"],
                HasUsedJobseekerBenefit = (bool)reader["HasUsedJobseekerBenefit"]
            };
        }
    }
}
