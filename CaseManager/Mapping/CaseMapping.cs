using AutoMapper;
using CaseManager.Dto;
using CaseManager.Models;
using Microsoft.OpenApi;

namespace CaseManager.Mapping;

public class CaseProfile : Profile
{
    public CaseProfile()
    {
        CreateMap<CreateCaseDto, Case>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => CaseStatus.Open))

            // CAUTION! We need to set it after mapping. Or we can use opt.Items["CreatedAt"]
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom((_, _, _, context) => (Guid)context.Items["Id"]))
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom((_, _, _, context) => (DateTime)context.Items["CreatedAt"]))
            .ForMember(dest => dest.ClosedAt, opt => opt.Ignore());

        CreateMap<Case, CreateCaseReturnDto>()
            .ForMember(dest => dest.CaseId, opt => opt.MapFrom(src => src.Id)).ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString())).ConstructUsing(src => new CreateCaseReturnDto(
                src.Id,
                src.Status.ToString(),
                src.CreatedAt
            ));;

        CreateMap<Case, CaseDetailsDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}