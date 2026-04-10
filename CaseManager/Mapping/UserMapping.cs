using AutoMapper;
using CaseManager.Dto;
using CaseManager.Models;

namespace CaseManager.Mapping;

// TODO: map it

// TODO: Chyba osobny mapper dla różnych User types?
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<RegisterUserDto, User>().ForMember(x => x.Id,
            opt => opt.MapFrom((_, _, _, context) => (Guid)context.Items["Id"]));

        CreateMap<User, UserRegisteredDto>();

        CreateMap<User, UserDetailsDto>();
    }
}