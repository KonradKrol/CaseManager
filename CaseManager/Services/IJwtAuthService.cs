using CaseManager.DomainModels;

namespace CaseManager.Services;

public interface IJwtAuthService
{
    string GenerateJwt(JwtUserClaims claims);
}

public record JwtUserClaims(Guid Sub, string Email, UserRole Role, JobTitle JobTitle, OnboardingStatus OnboardingStatus);