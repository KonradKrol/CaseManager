namespace CaseManager.Auth.RefreshTokens;

public class RefreshToken
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; } = null;

    public bool HasExpired(DateTime now)
    {
        return now >= ExpiresAt;
    }
}