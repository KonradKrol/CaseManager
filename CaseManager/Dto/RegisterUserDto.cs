using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace CaseManager.Dto;

public class RegisterUserDto
{
    public string Name { get; init; }
    public string Surname { get; init; }
    public string Role { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
    public string ConfirmPassword { get; init; }
    public string? AdminConfirmation { get; init; }
}

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().NotNull();
        RuleFor(x => x.Surname).NotEmpty().NotNull();
        RuleFor(x => x.Role).Must(x => x is "Admin" or "Worker");
        RuleFor(x => x.Email).NotNull().EmailAddress();
        RuleFor(x => x.AdminConfirmation).NotNull().NotEmpty().When(x => x.Role is "Admin");
        RuleFor(x => x.Password).NotNull().NotEmpty()
            .Equal(x => x.ConfirmPassword);
    }
}

public class UserRegisteredDto
{
    [Required] public Guid UserId { get; init; }

    [Required] public string Role { get; init; }
}