using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace CaseManager.Dto;

public class AddCommentDto
{
    public string Message { get; init; }
}

public class AddCommentDtoValidator : AbstractValidator<AddCommentDto>
{
    public AddCommentDtoValidator()
    {
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class CommentAddedDto
{
    [Required] public Guid CommentId { get; init; }
    [Required] public DateTime CreatedAt { get; init; }
}