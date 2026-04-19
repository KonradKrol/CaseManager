using CaseManager.DomainModels;
using CaseManager.Repository;

namespace CaseManager.BackgroundJobs;

public class AddMockUsersJob(
    IUserRepository userRepository) : BackgroundService
{
    private const int MockedCommentsCount = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SeedUsers();
    }

    public void SeedUsers()
    {
        userRepository.AddUser(CreateUser("konrad.krol123@gmail.com", "Konrad", "Król", UserRole.RegularUser,
            JobTitle.HrSpecialist,
            "kamilstoch"));
        userRepository.AddUser(CreateUser("johnsmith@yahoo.com", "John", "Smith", UserRole.Admin, JobTitle.ItExpert,
            "hey!hey!"));
        userRepository.AddUser(CreateUser("kuki@czarny-rynek.pl", "Kamil", "Rymaszewski", UserRole.RegularUser,
            JobTitle.OfficeWorker,
            "cimci!rimci"));
    }


    private static User CreateUser(
        string email,
        string name,
        string surname,
        UserRole role,
        JobTitle jobTitle,
        string rawPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        return new User(
            Guid.NewGuid(),
            name,
            surname,
            email,
            role,
            jobTitle,
            OnboardingStatus.Done,
            hash
        );
    }
}