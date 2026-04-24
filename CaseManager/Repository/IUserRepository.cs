using CaseManager.DomainModels;

namespace CaseManager.Repository;

// CQRS approach would suggest a repository returning DTOs, not domain objects (I think like Uncle Bob did)
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsers();
    Task<User?> GetUserById(Guid id);
    Task<User?> GetUserByEmail(string email);
    Task<bool> UserExists(Guid id);
    Task<bool> UserExists(string email);
    Task<bool> EveryUserExists(IEnumerable<Guid> ids);
    Task<IEnumerable<Guid>> GetNotExistingUserIds(IEnumerable<Guid> ids);
    Task AddUser(User user);
}