using System.Security.Cryptography;
using System.Text;

namespace CaseManager.Utils;

public static class CybersecurityUtils
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}