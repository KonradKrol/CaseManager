namespace CaseManager.Auth.RefreshTokens;

using System.Security.Cryptography;

public interface IRefreshTokenGenerator
{
    string Generate();
}

public class DefaultRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}