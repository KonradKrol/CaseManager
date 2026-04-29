using CaseManager.Services;
using CaseManager.Utils;

namespace CaseManager.Auth.RefreshTokens;

public class InMemoryRefreshTokens(IClock clock) : IRefreshTokens
{
    private readonly HashSet<RefreshToken> _tokens = new();

    public Task AddAsync(RefreshToken token)
    {
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string token)
    {
        var howMany = _tokens.RemoveWhere(refreshTokenObject => refreshTokenObject.TokenHash == token);
        return Task.FromResult(howMany > 0);
    }

    public Task<RefreshToken?> GetTokenAsync(string rawToken)
    {
        var hash = CybersecurityUtils.Hash(rawToken);
        return Task.FromResult(_tokens.FirstOrDefault(refreshTokenObject => refreshTokenObject.TokenHash == hash));
    }

    public Task<IEnumerable<RefreshToken>> GetUserTokensAsync(Guid userId)
    {
        return Task.FromResult(_tokens.Where(token => token.Id == userId));
    }

    public Task<RefreshToken?> GetLatestUserTokenAsync(Guid userId)
    {
        return Task.FromResult(_tokens
            .Where(t => t.Id == userId)
            .MaxBy(t => t.ExpiresAt));
    }

    public Task<int> RevokeAllByUserAsync(Guid userId)
    {
        var now = clock.UtcNow();
        var tokensToRevoke = _tokens.Where(token => token.UserId == userId && token.RevokedAt is null).ToList();
        foreach (var refreshToken in tokensToRevoke)
        {
            refreshToken.RevokedAt = now;
        }
        return Task.FromResult(tokensToRevoke.Count);
    }
}