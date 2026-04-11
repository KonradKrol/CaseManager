using CaseManager.Models;

namespace CaseManager.Repository;

// CQRS approach would suggest a repository returning DTOs, not domain objects (I think like Uncle Bob did)
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsers();
    Task<User?> GetUserById(Guid id);
    Task AddUser(User user);
}