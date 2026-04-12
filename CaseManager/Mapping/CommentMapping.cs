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
                return new Comment(id, dto.CaseId, dto.UserId, dto.Message);
            });

        CreateMap<Comment, CommentDetailsDto>();
    }
}