using CaseManager.Services;
using CaseManager.Utils;
using Microsoft.AspNetCore.SignalR;

namespace CaseManager.Auth.RefreshTokens;

public class InMemoryRefreshTokens(IClock clock, ILogger<InMemoryRefreshTokens> logger) : IRefreshTokens
{
    private readonly HashSet<RefreshToken> _tokens = new();

    public Task AddAsync(RefreshToken token)
    {
        logger.LogDebug("Adding a refresh token for {UserId} expiring at {ExpiresAt}", token.UserId,
            token.ExpiresAt); // TODO: Is it secure?
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string tokenHash)
    {
        var tokenToRemove = _tokens.SingleOrDefault(refreshTokenObject => refreshTokenObject.TokenHash == tokenHash);

        var hasDeleted = false;
        if (tokenToRemove is not null)
            hasDeleted = _tokens.Remove(tokenToRemove);

        logger.LogDebug(
            "Refresh token deletion attempted for user {UserId}. Success: {Success}",
            tokenToRemove?.UserId.ToString() ?? "N/A", hasDeleted);

        return Task.FromResult(hasDeleted);
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
            .Where(t => t.UserId == userId)
            .MaxBy(t => t.ExpiresAt));
    }

    public Task<int> RevokeAllByUserAsync(Guid userId)
    {
        var now = clock.UtcNow();

        logger.LogDebug(
            "All tokens revocation requested for user {UserId}", userId);

        var tokensToRevoke = _tokens.Where(token => token.UserId == userId && token.RevokedAt is null).ToList();
        foreach (var refreshToken in tokensToRevoke)
        {
            refreshToken.RevokedAt = now;
        }

        logger.LogDebug(
            "Revoked all ({RevokedTokensCount}) tokens for user {UserId}", tokensToRevoke.Count, userId);

        return Task.FromResult(tokensToRevoke.Count);
    }
}