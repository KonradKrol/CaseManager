using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CaseManager.Auth;

public class LocalDevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public LocalDevAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    public static Guid LocalDevUserId = Guid.Parse("714af0c0-de76-4585-b15d-f9afc3c76641");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "konrad-dev"),
            new Claim(ClaimTypes.Name, "Konrad"),
            new Claim(JwtRegisteredClaimNames.Sub, LocalDevUserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "konradek@gmail.com"),
            new Claim(Claims.Role, Claims.Roles.Admin),
            new Claim(Claims.JobTitle, "ItExpert"),
            new Claim(Claims.OnboardingStatus, "Done"),
            new Claim(JwtRegisteredClaimNames.Jti, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.String)
        };

        var importantClaims = claims
            .Where(c =>
                c.Type is Claims.JobTitle or Claims.Role or JwtRegisteredClaimNames.Sub or Claims.OnboardingStatus)
            .ToDictionary(c => c.Type, c => c.Value);

        Logger.LogInformation("Important claims: {@Claims}", importantClaims);

        var identity = new ClaimsIdentity(claims, "Dev", Claims.Name, Claims.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Dev");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}