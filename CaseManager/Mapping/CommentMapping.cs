using AutoMapper;
using CaseManager.Dto;
using CaseManager.DomainModels;

namespace CaseManager.Mapping;

public class CommentMapping : Profile
{
    public CommentMapping()
    {
        CreateMap<AddCommentDto, Comment>()
            .ConstructUsing((dto, context) =>
            {
                var id = (Guid)context.Items["Id"];
                var caseId = (Guid)context.Items["CaseId"];
                return new Comment(id, caseId, dto.UserId, dto.Message);
            });

        CreateMap<Comment, CommentDetailsDto>();
    }
}