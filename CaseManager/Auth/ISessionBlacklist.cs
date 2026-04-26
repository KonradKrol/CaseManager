namespace CaseManager.Auth;

public interface ISessionBlacklist
{
    public Task<bool> SessionIsRevoked(string sessionId);

    public Task RevokeSession(string sessionId);

}

public class InMemorySessionBlacklist : ISessionBlacklist
{
    private readonly HashSet<string> _revokedSessions = new();

    public Task<bool> SessionIsRevoked(string sessionId)
    {
        return Task.FromResult(_revokedSessions.Contains(sessionId));
    }

    public Task RevokeSession(string sessionId)
    {
        _revokedSessions.Add(sessionId);
        
        return Task.CompletedTask;
    }
}

// TODO: RedisSessionBlacklist