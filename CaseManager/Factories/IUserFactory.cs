using CaseManager.Models;

namespace CaseManager.Factories;

public interface IUserFactory
{
    User Create(Guid id, string name, string surname, string email, string role);
}

public class RoleBasedUserFactory : IUserFactory
{
    public User Create(Guid id, string name, string surname, string email, string role)
    {
        return role is "Admin"
            ? new Admin()
            {
                Id = id,
                Name = name,
                Surname = surname,
                Email = email,
            }
            : new Worker()
            {
                Id = id,
                Name = name,
                Surname = surname,
                Email = email,
            };
    }
}