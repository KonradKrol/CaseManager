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

    public Task AddUser(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}