using FluentValidation;

namespace CaseManager.Dto;

public class UpdateCaseDto
{
    public string? NewStatus { get; init; }
    public List<Guid>? RemovedAssignments { get; init; }
    public List<Guid>? NewAssignments { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public class UpdateCaseDtoValidator : AbstractValidator<UpdateCaseDto>
{
    public UpdateCaseDtoValidator()
    {
        RuleFor(x => x).Must(x =>
                x.NewStatus is not null || x.NewAssignments?.Count > 0 || x.RemovedAssignments?.Count > 0)
            .WithMessage("At least one field must be provided.");
        RuleFor(x => x.NewStatus).Must(x => x is "InProgress" or "Closed" or null);
        RuleFor(x => x).Must(x =>
        {
            var noRemovedAssignments = x.RemovedAssignments == null || x.RemovedAssignments.Count == 0;
            var noNewAssignments = x.NewAssignments == null || x.NewAssignments.Count == 0;
            if (x.NewStatus is "Closed")
            {
                return noRemovedAssignments && noNewAssignments;
            }

            return true;
        });
        RuleFor(x => x.ClosedAt).NotEmpty().When(x => x.NewStatus is "Closed");
        RuleFor(x => x.ClosedAt).Empty().When(x => x.NewStatus is not "Closed");
    }
}