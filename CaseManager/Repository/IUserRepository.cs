using CaseManager.DomainModels;

namespace CaseManager.Repository;

// CQRS approach would suggest a repository returning DTOs, not domain objects (I think like Uncle Bob did)

// TODO: Make it production-ready: (1) add cancellation tokens, (2) remove GetAllUsers and other too expensive methods
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> GetCountAsync();
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> UserExistsAsync(Guid id);
    Task<bool> UserExistsAsync(string email);
    Task<bool> EveryUserExistsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<Guid>> GetNotExistingUserIdsAsync(IEnumerable<Guid> ids);
    Task AddUserAsync(User user);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> UpdateUserAsync(User user);
}