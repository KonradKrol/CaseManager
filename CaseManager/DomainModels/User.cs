using CaseManager.Exceptions;

namespace CaseManager.DomainModels;

// TODO: Może w przyszłości wydzielić hasło.
public class User
{
    public User(Guid id, string name, string surname, string email, UserRole role, JobTitle jobTitle,
        OnboardingStatus onboardingStatus, string passwordHash)
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

        if (role is UserRole.Admin && !jobTitle.IsAllowedToBeAdmin())
        {
            throw new DomainEntityCreationException("This JobTitle is not allowed to be Admin");
        }

        Id = id;
        Name = name;
        Surname = surname;
        Email = email;
        Role = role;
        JobTitle = jobTitle;
        OnboardingStatus = onboardingStatus;
        PasswordHash = passwordHash;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Surname { get; }
    public string Email { get; }
    public UserRole Role { get; }
    public JobTitle JobTitle { get; }
    public OnboardingStatus OnboardingStatus { get; }
    public string PasswordHash { get; }

    public string FullName => $"{Name} {Surname}";
}

public enum OnboardingStatus
{
    NotStarted,
    InProgress,
    Done,
}

public enum JobTitle
{
    Ceo,
    DepartmentDirector,
    ItExpert,
    OfficeWorker,
    HrSpecialist,
    Marketing,
}

public static class JobTitleExtensions
{
    extension(JobTitle jobTitle)
    {
        public bool IsAllowedToBeAdmin()
        {
            return AllowedJobTitles().Contains(jobTitle);
        }
    }

    private static HashSet<JobTitle> AllowedJobTitles()
    {
        return
        [
            JobTitle.Ceo,
            JobTitle.ItExpert,
            JobTitle.DepartmentDirector
        ];
    }
}

public enum UserRole
{
    Admin,
    RegularUser
}