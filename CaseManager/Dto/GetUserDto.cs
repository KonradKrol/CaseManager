using FluentValidation;

namespace CaseManager.Dto;

public class UserDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Surname { get; init; }
    public string Role { get; init; }
    public string Email { get; init; }
}

public class UserDetailsDtoValidator : AbstractValidator<UserDetailsDto>
{
    public UserDetailsDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Surname).NotEmpty();
        RuleFor(x => x.Role).Must(x => x is "Admin" or "Worker");
    }
}