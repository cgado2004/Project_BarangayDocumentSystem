using System;
using System.Collections.Generic;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces;











public interface IBarangayRepository
{

    IReadOnlyList<Resident> Residents { get; }
    IReadOnlyList<DocumentRequest> Requests { get; }

    Resident AddResident(ResidentDetails details);
    void UpdateResident(Resident resident, ResidentDetails details);





    void ArchiveResident(Resident resident, string destination, string reason);


    void RestoreResident(Resident resident);


    IEnumerable<Resident> SearchResidents(string term);


    IEnumerable<Resident> SearchArchivedResidents(string term);


    DocumentRequest CreateRequest(Resident resident, DocumentType type, string purpose, bool isStudent = false);






    void SaveRequest(DocumentRequest request);
    IEnumerable<DocumentRequest> GetRequestsByStatus(RequestStatus? status);
    string GetNextOfficialReceiptNo();

    BarangayStatistics GetStatistics();






    void Reload();




    string CurrentUser { get; set; }


    void LogActivity(string action, string details);


    IReadOnlyList<ActivityEntry> GetActivity(DateTime day);
}


public record ActivityEntry(DateTime LoggedOn, string Username, string Action, string Details);












public record ResidentDetails(
    string FirstName,
    string MiddleName,
    string LastName,
    string Suffix,
    DateTime DateOfBirth,
    Gender Gender,
    CivilStatus CivilStatus,
    string Purok,
    string HouseNumber,
    string ContactNumber,
    string Occupation,
    DateTime DateOfResidency,
    bool IsRegisteredVoter,
    ResidentClassification Classification,
    IReadOnlyList<Dependent>? Dependents = null);








public record BarangayStatistics(
    int TotalResidents,
    int RegisteredVoters,
    int SeniorCitizens,
    int TotalRequests,
    int Pending,
    int Processing,
    int ReadyForRelease,
    int Released,
    decimal TotalCollected,
    int IssuedFreeOfCharge,
    IReadOnlyDictionary<string, int> RequestsByDocumentType,
    IReadOnlyDictionary<string, int> ResidentsByPurok);

public record UserAccount(int UserId, string Username, string FullName, string PasswordHash, UserRole Role, bool IsArchived);

public record LoginThrottle(int FailedCount, DateTime? LockedUntil);

public interface IUserRepository
{
    bool AnyUsers();

    UserAccount? Find(string username);

    UserAccount? GetById(int userId);

    void Create(string username, string fullName, string passwordHash, UserRole role);

    void Update(int userId, string username, string fullName, UserRole role);

    void ChangePassword(int userId, string passwordHash);

    void SetArchived(int userId, bool archived);

    void Delete(int userId);

    IReadOnlyList<UserAccount> ListUsers();

    LoginThrottle GetLoginThrottle(string username);

    LoginThrottle RecordLoginFailure(string username);

    void ClearLoginFailures(string username);
}
