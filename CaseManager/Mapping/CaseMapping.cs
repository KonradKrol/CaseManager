using AutoMapper;
using CaseManager.Dto;
using CaseManager.DomainModels;
using Microsoft.OpenApi;

namespace CaseManager.Mapping;

public class CaseProfile : Profile
{
    public CaseProfile()
    {
        CreateMap<CreateCaseDto, Case>()
            .ConstructUsing((dto, context) =>
            {
                var id = (Guid)context.Items["Id"];
                var authorId = (Guid)context.Items["AuthorId"];
                var createdAt = (DateTime)context.Items["CreatedAt"];

                return new Case(id, authorId, dto.Title, dto.Description, dto.AssignedTo ?? [], CaseStatus.Open,
                    createdAt);
            });

        CreateMap<Case, CreateCaseReturnDto>()
            .ForMember(dest => dest.CaseId, opt => opt.MapFrom(src => src.Id)).ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString())).ConstructUsing(src => new CreateCaseReturnDto(
                src.Id,
                src.Status.ToString(),
                src.CreatedAt
            ));
        ;

        CreateMap<Case, CaseDetailsDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}