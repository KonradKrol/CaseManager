namespace CaseManager.Auth.RefreshTokens;

public interface IRefreshTokens
{
    Task AddAsync(RefreshToken token);
    Task<bool> DeleteAsync(string token);
    Task<RefreshToken?> GetTokenAsync(string rawToken);
    Task<IEnumerable<RefreshToken>> GetUserTokensAsync(Guid userId);
    Task<RefreshToken?> GetLatestUserTokenAsync(Guid userId);
    Task<int> RevokeAllByUserAsync(Guid userId);
}