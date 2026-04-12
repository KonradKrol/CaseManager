using CaseManager.DomainModels;
using FluentValidation;

namespace CaseManager.Dto;

// It can throw if Guid's are inappropriate. We can introduce some middleware in the future to protect from errors leaking the underlying domain models.
public record CreateCaseDto(List<Guid>? AssignedTo, string Title, string Description);

public class CreateCaseDtoValidator : AbstractValidator<CreateCaseDto>
{
    public CreateCaseDtoValidator()
    {
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
        // RuleFor(x => x.AssignedTo);
    }
}

public record CreateCaseReturnDto(Guid CaseId, string Status, DateTime CreatedAt);

public class CreateCaseReturnDtoValidator : AbstractValidator<CreateCaseReturnDto>
{
    public CreateCaseReturnDtoValidator()
    {
        RuleFor(x => x.CaseId).NotNull().NotEmpty();
        RuleFor(x => x.Status).NotNull()
            .Must(x => x is "Open" or "InProgress" or "Closed");
        // RuleFor(x => x.CreatedAt).NotNull().LessThanOrEqualTo(DateTime.UtcNow);
    }
}