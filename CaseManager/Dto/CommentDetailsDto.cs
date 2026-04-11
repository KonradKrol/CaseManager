using FluentValidation;

namespace CaseManager.Dto;

public class CommentDetailsDto
{
    public Guid Id { get; init; }
    public Guid CaseId { get; init; }
    public Guid UserId { get; init; }
    public required string Message { get; init; }
}

public class CommentDetailsDtoValidator : AbstractValidator<CommentDetailsDto>
{
    public CommentDetailsDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty();
    }
}