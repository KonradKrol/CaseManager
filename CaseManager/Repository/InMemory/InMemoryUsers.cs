using CaseManager.DomainModels;

namespace CaseManager.Repository.InMemory;

public class InMemoryUsers : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return Task.FromResult<IEnumerable<User>>(_users);
    }

    public Task<int> GetCountAsync()
    {
        return Task.FromResult(_users.Count);
    }

    public Task<User?> GetUserByIdAsync(Guid id)
    {
        return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
    }

    public Task<User?> GetUserByEmailAsync(string email)
    {
        return Task.FromResult(_users.SingleOrDefault(user => user.Email == email));
    }

    public async Task<bool> UserExistsAsync(Guid id)
    {
        return await GetUserByIdAsync(id) is not null;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await GetUserByEmailAsync(email) is not null;
    }

    public async Task<bool> EveryUserExistsAsync(IEnumerable<Guid> ids)
    {
        return (await GetNotExistingUserIdsAsync(ids)).Count() == 0;
    }

    public Task<IEnumerable<Guid>> GetNotExistingUserIdsAsync(IEnumerable<Guid> ids)
    {
        var notExistingIds = ids.Where(id => _users.All(user => user.Id != id)).ToList();

        return Task.FromResult<IEnumerable<Guid>>(notExistingIds);
    }

    public Task AddUserAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteUserAsync(Guid id)
    {
        var matches = _users.Where(x => x.Id == id).Take(2).ToList();

        if (matches.Count == 0)
            return Task.FromResult(false);

        if (matches.Count > 1)
            throw new InvalidOperationException($"Duplicate users with Id {id}");

        _users.Remove(matches[0]);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        var index = _users.FindIndex(x => x.Id == user.Id);

        if (index == -1)
            return Task.FromResult(false);

        if (_users.Count(x => x.Id == user.Id) > 1)
            throw new InvalidOperationException($"Duplicate users with Id {user.Id}");

        _users[index] = user;

        return Task.FromResult(true);
    }
}