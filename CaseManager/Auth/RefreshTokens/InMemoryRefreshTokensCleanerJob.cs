namespace CaseManager.Auth.RefreshTokens;

public class InMemoryRefreshTokensCleanerJob(InMemoryRefreshTokens inMemoryRefreshTokens) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            // await inMemoryRefreshTokens.DeleteAllNotLatestTokensAsync();
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}