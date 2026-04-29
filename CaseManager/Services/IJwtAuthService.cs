using System.Security.Claims;
using CaseManager.Auth;
using CaseManager.DomainModels;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CaseManager.Services;

public interface IJwtAuthService
{
    string GenerateJwt(JwtUserClaims claims);
}

public interface IJwtUserClaimsFactory
{
    JwtUserClaims Create(User user);
}

public class DefaultJwtUserClaimsFactory : IJwtUserClaimsFactory
{
    public JwtUserClaims Create(User user)
    {
        return new JwtUserClaims(user.Id, user.Email, user.Role, user.JobTitle,
            user.OnboardingStatus);
    }
}

public record JwtUserClaims(
    Guid Sub,
    string Email,
    UserRole Role,
    JobTitle JobTitle,
    OnboardingStatus OnboardingStatus);

public static class JwtUserClaimsExtensions
{
    extension(JwtUserClaims userClaims)
    {
        public List<Claim> ToClaims()
        {
            return new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Sub, userClaims.Sub.ToString()),
                new(JwtRegisteredClaimNames.Email, userClaims.Email),
                new(Claims.Role, userClaims.Role.ToString()),
                new(Claims.JobTitle, userClaims.JobTitle.ToString()),
                new(Claims.OnboardingStatus, userClaims.OnboardingStatus.ToString())
            };
        }
    }
}