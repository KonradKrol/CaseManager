using AutoMapper;
using CaseManager.Dto;
using CaseManager.DomainModels;

namespace CaseManager.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<RegisterUserDto, User>()
            .ConstructUsing((dto, context) =>
            {
                var id = (Guid)context.Items["Id"];
                var passwordHash = (string)context.Items["PasswordHash"];

                var onboardingStatus = dto.SkipOnboarding ? OnboardingStatus.Done : OnboardingStatus.NotStarted;

                return new User(id, dto.Name, dto.Surname, dto.Email, Enum.Parse<UserRole>(dto.Role),
                    Enum.Parse<JobTitle>(dto.JobTitle), onboardingStatus, passwordHash);
            });

        CreateMap<User, UserRegisteredDto>();

        CreateMap<User, UserDetailsDto>();
    }
}