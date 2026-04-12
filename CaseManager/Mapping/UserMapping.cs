using AutoMapper;
using CaseManager.Dto;
using CaseManager.DomainModels;
using UserRole = CaseManager.DomainModels.UserRole;

namespace CaseManager.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<RegisterUserDto, User>()
            .ConstructUsing((dto, context) =>
            {
                var id = (Guid)context.Items["Id"];

                return new User(id, dto.Name, dto.Surname, dto.Email, Enum.Parse<UserRole>(dto.Role));
            });

        CreateMap<User, UserRegisteredDto>();

        CreateMap<User, UserDetailsDto>();
    }
}