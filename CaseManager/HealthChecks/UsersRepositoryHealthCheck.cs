using CaseManager.Repository;
using CaseManager.Repository.FileSystem;
using CaseManager.Repository.InMemory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CaseManager.HealthChecks;

public class UsersRepositoryHealthCheck(IUserRepository users, ILogger<UsersRepositoryHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        switch (users)
        {
            case FileSystemUsers fileSystemUsers:
                await fileSystemUsers.GetCountAsync();
                return HealthCheckResult.Healthy();
            case InMemoryUsers inMemoryUsers:
                await inMemoryUsers.GetCountAsync();
                return HealthCheckResult.Healthy();
        }
        
        logger.LogWarning("Returning Unhealthy, because no user repository type matched.");
        return HealthCheckResult.Unhealthy();
    }
}