using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaseManager.Auth;
using CaseManager.DomainModels;
using Microsoft.IdentityModel.Tokens;

namespace CaseManager.Services;

public class SemiProdJwtAuthService(string securityKey, IClock clock) : IJwtAuthService
{
    private const string IssuerUrl = "https://auth.case-manager-internal-api.eu";
    private const string Audience = "case-manager";

    public string GenerateJwt(JwtUserClaims claims)
    {
        var (userId, email, role, jobTitle, onboardingStatus) = claims;
        var userIdString = userId.ToString();

        var roleString = role.ToString();
        var jobTitleString = jobTitle.ToString();
        var onboardingStatusString = onboardingStatus.ToString();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(securityKey));

        // key jest bazą do stworzenia globalnego signature, łączącego: (1) payload i (2) header w ramach działania HMAC
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: IssuerUrl,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userIdString),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(Claims.Role, roleString),
                new Claim(Claims.JobTitle, jobTitleString),
                new Claim(Claims.OnboardingStatus, onboardingStatusString),
                new Claim(JwtRegisteredClaimNames.Jti, clock.NowOffset().ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.String)
            ], // Do JWT dajemy stałe rzeczy i tożsamość (identity)
            notBefore: clock.UtcNow(),
            expires: clock.UtcNow().Add(TimeSpan.FromMinutes(15)),
            signingCredentials: credentials
        );

        var cleanJwt = new JwtSecurityTokenHandler().WriteToken(token);

        return cleanJwt;
    }
};