using CaseManager.DomainModels;

namespace CaseManager.Repository.InMemory;

public class InMemoryUsers : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<IEnumerable<User>> GetAllUsers()
    {
        return Task.FromResult<IEnumerable<User>>(_users);
    }

    public Task<User?> GetUserById(Guid id)
    {
        return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
    }

    public Task<User?> GetUserByEmail(string email)
    {
        return Task.FromResult(_users.SingleOrDefault(user => user.Email == email));
    }

    public async Task<bool> UserExists(Guid id)
    {
        return await GetUserById(id) is not null;
    }

    public async Task<bool> UserExists(string email)
    {
        return await GetUserByEmail(email) is not null;
    }

    public Task<bool> EveryUserExists(IEnumerable<Guid> ids, out IEnumerable<Guid> notExistingIds)
    {
        notExistingIds = ids.Where(id => _users.All(user => user.Id != id)).ToList();

        var everyUserExists = notExistingIds.Count() == 0;

        return Task.FromResult(everyUserExists);
    }

    public Task AddUser(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}