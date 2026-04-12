using CaseManager.DomainModels;

namespace CaseManager.Repository;

public interface ICaseRepository
{
    Task<IEnumerable<Case>> GetFirstNCasesByCreatedAt(int startingAtIndex, int n);
    Task<Case?> GetCaseById(Guid id);
    Task<bool> CaseExists(Guid id);
    Task AddCase(Case @case);
}