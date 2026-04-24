using CaseManager.Auth;
using CaseManager.DomainModels;
using CaseManager.Repository;

namespace CaseManager.BackgroundJobs;

public class AddMockUsersJob(
    IUserRepository userRepository) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SeedUsers();
    }

    private void SeedUsers()
    {
        userRepository.AddUser(CreateUser("konrad.krol123@gmail.com", "Konrad", "Król", UserRole.RegularUser,
            JobTitle.HrSpecialist,
            "kamilstoch", OnboardingStatus.InProgress));
        userRepository.AddUser(CreateUser("johnsmith@yahoo.com", "John", "Smith", UserRole.Admin, JobTitle.ItExpert,
            "hey!hey!"));
        userRepository.AddUser(CreateUser("kuki@czarny-rynek.pl", "Kamil", "Rymaszewski", UserRole.RegularUser,
            JobTitle.OfficeWorker,
            "cimci!rimci"));
        userRepository.AddUser(CreateUser("konrad@yahoo.com", "Konrad", "Król", UserRole.Admin,
            JobTitle.ItExpert,
            "abc123", idOverride: LocalDevAuthenticationHandler.LocalDevUserId));
    }


    private static User CreateUser(
        string email,
        string name,
        string surname,
        UserRole role,
        JobTitle jobTitle,
        string rawPassword,
        OnboardingStatus onboardingStatus = OnboardingStatus.Done,
        Guid? idOverride = null)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        return new User(
            idOverride ?? Guid.NewGuid(),
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