using CaseManager.DomainModels;
using CaseManager.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CaseManager.HealthChecks;

public class JwtHealthCheck(IJwtAuthService jwtAuthService, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            return HealthCheckResult.Unhealthy("Jwt:Key is missing");
        }

        var token = jwtAuthService.GenerateJwt(new JwtUserClaims(Guid.Empty, "", UserRole.RegularUser, JobTitle.DepartmentDirector, OnboardingStatus.InProgress));

        if (string.IsNullOrWhiteSpace(token))
        {
            return HealthCheckResult.Unhealthy("Token generation is broken");
        }
        
        return HealthCheckResult.Healthy();
    }
}