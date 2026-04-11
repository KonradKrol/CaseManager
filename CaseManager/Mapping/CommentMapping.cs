using AutoMapper;
using CaseManager.Dto;
using CaseManager.Models;

namespace CaseManager.Mapping;

public class CommentMapping : Profile
{
    public CommentMapping()
    {
        CreateMap<AddCommentDto, Comment>().ForMember(x => x.Id,
            opt => opt.MapFrom((_, _, _, context) => (Guid)context.Items["Id"]));

        CreateMap<Comment, CommentDetailsDto>();
    }
}