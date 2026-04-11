using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace CaseManager.Dto;

public class AddCommentDto
{
    public Guid CaseId { get; init; }
    public Guid UserId { get; init; }
    public string Message { get; init; }
}

public class AddCommentDtoValidator : AbstractValidator<AddCommentDto>
{
    public AddCommentDtoValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty().NotNull().WithMessage("CaseId must be set");
        RuleFor(x => x.UserId).NotEmpty().NotNull().WithMessage("UserId must be set");
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class CommentAddedDto
{
    [Required] public Guid CommentId { get; init; }
    [Required] public DateTime CreatedAt { get; init; }
}