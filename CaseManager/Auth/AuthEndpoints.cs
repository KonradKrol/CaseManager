using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using CaseManager.DomainModels;
using CaseManager.Dto;
using CaseManager.Repository;
using CaseManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRole = CaseManager.DomainModels.UserRole;

namespace CaseManager.Auth;

public static class AuthEndpoints
{
    // TODO: Nadgoń, bo cookies ma lepsze security
    public static void MapJwtBearerLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LoginDto loginDto, IUserRepository userRepository,
            IJwtAuthService authService, IPasswordHasher<User> hasher) =>
        {
            var user = await userRepository.GetUserByEmailAsync(loginDto.Email);

            const string dummyHash = "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result = hasher.VerifyHashedPassword(user!, passwordToVerify, loginDto.Password); // TODO: Don't use null-coallesce

            if (user is null)
            {
                return Results.Unauthorized();
            }

            switch (result)
            {
                case PasswordVerificationResult.Failed:
                    return Results.Unauthorized();
                case PasswordVerificationResult.Success:
                    break;
                case PasswordVerificationResult.SuccessRehashNeeded:
                {
                    await RehashUserPassword(hasher, user, passwordToVerify, userRepository);
                    break;
                }
            }

            var claims = new JwtUserClaims(user.Id, loginDto.Email, user.Role, user.JobTitle, user.OnboardingStatus);

            var jwt = authService.GenerateJwt(claims);

            return Results.Ok(new { jwt });
        }).AllowAnonymous();
    }

    public static void MapCookiesLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LoginDto loginDto, IUserRepository userRepository, IClock clock,
            HttpContext httpContext, ISessionBlacklist _, IPasswordHasher<User> hasher) =>
        {
            var user = await userRepository.GetUserByEmailAsync(loginDto.Email);

            const string dummyHash = "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result = hasher.VerifyHashedPassword(user!, passwordToVerify, loginDto.Password);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            switch (result)
            {
                case PasswordVerificationResult.Failed:
                    return Results.Unauthorized();
                case PasswordVerificationResult.Success:
                    break;
                case PasswordVerificationResult.SuccessRehashNeeded:
                {
                    await RehashUserPassword(hasher, user, loginDto.Password, userRepository);
                    break;
                }
            }

            var userClaims =
                new JwtUserClaims(user.Id, loginDto.Email, user.Role, user.JobTitle, user.OnboardingStatus).ToClaims();

            var sessionId = Guid.NewGuid().ToString();

            var otherClaims = new List<Claim>
            {
                new("session_id", sessionId)
            };

            var claims = userClaims.Concat(otherClaims);

            var identity = new ClaimsIdentity(claims: claims, authenticationType: "Cookies", roleType: Claims.Role);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync("Cookies", principal);

            return Results.Ok();
        }).AllowAnonymous();
    }

    private static async Task RehashUserPassword(IPasswordHasher<User> hasher, User user, string inputPassword,
        IUserRepository userRepository)
    {
        var newHash = hasher.HashPassword(user, inputPassword);
        var updatedUser = user.UpdatePassword(newHash);
        await userRepository.UpdateUserAsync(updatedUser);
    }

    public static void MapCookiesLogoutEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/logout", async (ISessionBlacklist sessionBlacklist, HttpContext context) =>
        {
            var sessionId = context.User.FindFirst("session_id")?.Value;

            if (sessionId is null) return Results.Unauthorized();

            await sessionBlacklist.RevokeSession(sessionId);

            await context.SignOutAsync("Cookies");

            return Results.Ok();
        });
    }

    public static void MapDebugClaimsEndpoint(this WebApplication app)
    {
        app.MapGet("/debug_claims",
                (HttpContext ctx) => { return ctx.User.Claims.Select(c => new { c.Type, c.Value }); })
            .RequireAuthorization(Policies.ItExpertOrAdmin);
    }
}