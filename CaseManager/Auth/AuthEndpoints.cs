using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using CaseManager.DomainModels;
using CaseManager.Dto;
using CaseManager.Repository;
using CaseManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using UserRole = CaseManager.DomainModels.UserRole;

namespace CaseManager.Auth;

public static class AuthEndpoints
{
    public static void MapJwtBearerLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LoginDto loginDto, IUserRepository userRepository,
            IJwtAuthService authService, IMapper mapper) =>
        {
            var user = await userRepository.GetUserByEmail(loginDto.Email);

            const string dummyHash = "$2a$11$C6UzMDM.H6dfI/f/IKcEeO9mG0w8pQ6F0F5w5Y9OQp6Y5r5a5f5a6";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var passwordMatches = BCrypt.Net.BCrypt.Verify(loginDto.Password, passwordToVerify);

            if (user is null || !passwordMatches)
            {
                return Results.Unauthorized();
            }

            var claims = new JwtUserClaims(user.Id, loginDto.Email, user.Role, user.JobTitle, user.OnboardingStatus);

            var jwt = authService.GenerateJwt(claims);

            return Results.Ok(new { jwt });
        }).AllowAnonymous();
    }

    // TODO: Od zera, poprawnie, produkcyjnie
    public static void MapCookiesLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LoginDto loginDto, IUserRepository userRepository, IClock clock,
            HttpContext httpContext) =>
        {
            // var user = await userRepository.GetUserByEmail(loginDto.Email);

            var user = new User(Guid.NewGuid(), "Dawid", "Kubacki", "ok@ok.ok", UserRole.Admin,
                JobTitle.DepartmentDirector, OnboardingStatus.Done, "ok");

            if (user == null || user.PasswordHash != loginDto.Password)
            {
                Console.WriteLine(user.PasswordHash);
                return Results.Unauthorized();
            }

            List<Claim> claims =
            [
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, loginDto.Email),
                new(Claims.Role, user.Role.ToString()), // TODO: do poprawy
                new(JwtRegisteredClaimNames.Jti, clock.NowOffset().ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.String)
            ];

            var identity = new ClaimsIdentity(claims: claims, authenticationType: "Cookies", roleType: Claims.Role);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync("Cookies", principal);

            return Results.Ok();
        }).AllowAnonymous();
    }

    public static void MapDebugClaimsEndpoint(this WebApplication app)
    {
        app.MapGet("/debug_claims",
                (HttpContext ctx) => { return ctx.User.Claims.Select(c => new { c.Type, c.Value }); })
            .RequireAuthorization(Policies.ItExpertOrAdmin);
    }
}