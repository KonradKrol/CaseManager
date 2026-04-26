using CaseManager.Auth;
using CaseManager.DomainModels;
using CaseManager.Repository;
using Microsoft.AspNetCore.Identity;

namespace CaseManager.BackgroundJobs;

public class AddMockUsersJob(
    IUserRepository userRepository, IPasswordHasher<User> passwordHasher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var usersCount = await userRepository.GetCountAsync();
        if (usersCount < 4)
            await SeedUsers();
    }

    private async Task SeedUsers()
    {
        await userRepository.AddUserAsync(CreateUser("konrad.krol123@gmail.com", "Konrad", "Król", UserRole.RegularUser,
            JobTitle.HrSpecialist,
            "kamilstoch", OnboardingStatus.InProgress));
        await userRepository.AddUserAsync(CreateUser("johnsmith@yahoo.com", "John", "Smith", UserRole.Admin,
            JobTitle.ItExpert,
            "hey!hey!"));
        await userRepository.AddUserAsync(CreateUser("kuki@czarny-rynek.pl", "Kamil", "Rymaszewski",
            UserRole.RegularUser,
            JobTitle.OfficeWorker,
            "cimci!rimci"));
        await userRepository.AddUserAsync(CreateUser("konrad@yahoo.com", "Konrad", "Król", UserRole.Admin,
            JobTitle.ItExpert,
            "abc123", idOverride: LocalDevAuthenticationHandler.LocalDevUserId));
    }

    private string HashPassword(string input)
    {
        var hash = passwordHasher.HashPassword(null!, input);

        return hash;
    }


    private User CreateUser(
        string email,
        string name,
        string surname,
        UserRole role,
        JobTitle jobTitle,
        string rawPassword,
        OnboardingStatus onboardingStatus = OnboardingStatus.Done,
        Guid? idOverride = null)
    {
        var hash = HashPassword(rawPassword); // TODO: allow rehashing after the, so pass the User object

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