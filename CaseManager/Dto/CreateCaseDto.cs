using CaseManager.Models;
using FluentValidation;

namespace CaseManager.Dto;

public record CreateCaseDto(Guid Id, List<Guid>? AssignedTo, string Title, string Description);

public class CreateCaseDtoValidator : AbstractValidator<CreateCaseDto>
{
    public CreateCaseDtoValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
        // RuleFor(x => x.AssignedTo);
    }
}