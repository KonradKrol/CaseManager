using AutoMapper;
using CaseManager.Dto;
using CaseManager.Models;

namespace CaseManager.Mapping;

public class CaseProfile : Profile
{
    public CaseProfile()
    {
        CreateMap<CreateCaseDto, Case>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => CaseStatus.Open))
            
            // CAUTION! We need to set it after mapping. Or we can use opt.Items["CreatedAt"]
            // .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ClosedAt, opt => opt.Ignore());
    }
}