using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace CaseManager.Dto;

public class RegisterUserDto
{
    public string Name { get; init; }
    public string Surname { get; init; }
    public string Role { get; init; }
    public string Email { get; init; }
    public string JobTitle { get; init; }
    public string Password { get; init; }
    public string ConfirmPassword { get; init; }
    public string? AdminConfirmation { get; init; }
    public bool SkipOnboarding { get; init; } = false;
}

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Surname).NotEmpty().NotNull();
        RuleFor(x => x.Role).Must(x => x is "Admin" or "RegularUser");
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.JobTitle).NotEmpty();
        RuleFor(x => x.AdminConfirmation).NotNull().NotEmpty().When(x => x.Role is "Admin");
        RuleFor(x => x.Password).NotEmpty()
            .Equal(x => x.ConfirmPassword);
    }
}

public class UserRegisteredDto
{
    [Required] public Guid UserId { get; init; }
}