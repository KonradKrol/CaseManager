using System.ComponentModel.DataAnnotations;
using CaseManager.DomainModels;
using FluentValidation;

namespace CaseManager.Dto;

// To make it secure: (1) hash the password, (2) use HTTPS, (3) "invalid credentials" instead of "wrong password"
public class LoginDto
{
    public string Email { get; init; }
    public string Password { get; init; }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class LoggedInDto
{
    // I will learn about tokens LATER. Just some special secret string :)
    [Required] public string Token { get; init; }
    [Required] public DateTime ExpiresAt { get; init; }
    [Required] public LoggedUserDto User { get; init; }
}

public record LoggedUserDto([Required] Guid Id, [Required] [EmailAddress] string Email, [Required] UserRole Role);