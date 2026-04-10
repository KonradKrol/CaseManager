using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace CaseManager.Dto;

public record GetCaseDto([Required] Guid CaseId);

public record CaseDetailsDto(
    Guid Id,
    List<Guid> AssignedTo,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt
);

public class CaseDetailsDtoValidator : AbstractValidator<CaseDetailsDto>
{
    public CaseDetailsDtoValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
        RuleFor(x => x.Status).NotNull().NotEmpty().Must(x => x is "Open" or "InProgress" or "Closed");
        RuleFor(x => x.CreatedAt).NotNull();
        RuleFor(x => x.ClosedAt).NotEmpty().When(x => x.Status is "Closed");
        RuleFor(x => x.ClosedAt).Empty().When(x => x.Status is not "Closed").WithMessage("ClosedAt cannot be present when the case is not closed yet.");
    }
}