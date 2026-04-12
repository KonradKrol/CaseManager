using CaseManager.Exceptions;

namespace CaseManager.DomainModels;

public class User
{
    public User(Guid id, string name, string surname, string email, UserRole role)
    {
        if (name.Length == 0)
        {
            throw new DomainEntityCreationException("Name can't be empty.");
        }

        if (surname.Length == 0)
        {
            throw new DomainEntityCreationException("Surname can't be empty.");
        }

        if (email.Length == 0)
        {
            throw new DomainEntityCreationException("Email can't be empty.");
        }

        Id = id;
        Name = name;
        Surname = surname;
        Email = email;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Surname { get; }
    public string Email { get; }
    public UserRole Role { get; }

    public string FullName => $"{Name} {Surname}";
}

public enum UserRole
{
    Admin,
    Worker
}