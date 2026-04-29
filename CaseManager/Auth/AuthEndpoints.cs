using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using CaseManager.Auth.RefreshTokens;
using CaseManager.DomainModels;
using CaseManager.Dto;
using CaseManager.Repository;
using CaseManager.Services;
using CaseManager.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRole = CaseManager.DomainModels.UserRole;

namespace CaseManager.Auth;

public static class AuthEndpoints
{
    // TODO: Nadgoń, bo cookies ma lepsze security
    public static void MapJwtBearerLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LogInDto logInDto, IUserRepository userRepository,
            IJwtAuthService authService, IPasswordHasher<User> hasher, IJwtUserClaimsFactory userClaimsFactory,
            IRefreshTokens refreshTokens, IRefreshTokenGenerator refreshTokenGenerator) =>
        {
            var user = await userRepository.GetUserByEmailAsync(logInDto.Email);


            const string dummyHash =
                "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result =
                hasher.VerifyHashedPassword(user!, passwordToVerify,
                    logInDto.Password); // TODO: Don't use null-coallesce

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var userId = user.Id;

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

            var claims = userClaimsFactory.Create(user);

            var jwt = authService.GenerateJwt(claims);

            var rawToken =
                await GenerateAndPersistRefreshToken(userId, null, refreshTokens, refreshTokenGenerator);

            return Results.Ok(new { AccessToken = jwt, RefreshToken = rawToken });
        }).AllowAnonymous();

        app.MapPost("/auth/login_with_refresh_token", async (
            [FromBody] LogInWithRefreshTokenDto logInWithRefreshTokenDto, IRefreshTokens refreshTokens,
            IRefreshTokenGenerator refreshTokenGenerator, IUserRepository userRepository,
            IJwtUserClaimsFactory userClaimsFactory, IJwtAuthService authService, ILogger<Program> logger) =>
        {
            var inputToken = logInWithRefreshTokenDto.RefreshToken;
            var tokenFromDatabase = await refreshTokens.GetTokenAsync(inputToken);
            if (tokenFromDatabase is null || tokenFromDatabase.HasExpired(DateTime.UtcNow))
            {
                return Results.Unauthorized();
            }

            var userId = tokenFromDatabase.UserId;

            if (tokenFromDatabase.RevokedAt is not null)
            {
                var howManyRevoked = await refreshTokens.RevokeAllByUserAsync(userId);
                logger.LogInformation("Revoked {HowManyRevoked} tokens of user {UserId}", howManyRevoked, userId);
                return Results.Unauthorized();
            }

            var user = await userRepository.GetUserByIdAsync(userId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var claims = userClaimsFactory.Create(user);

            var jwt = authService.GenerateJwt(claims);

            var rawToken =
                await GenerateAndPersistRefreshToken(userId, tokenFromDatabase, refreshTokens, refreshTokenGenerator);

            return Results.Ok(new { RefreshToken = rawToken, AccessToken = jwt });
        }).AllowAnonymous();

        app.MapDelete("/auth/refresh_tokens",
            async (ClaimsPrincipal httpUser, IRefreshTokens refreshTokens, ILogger<Program> logger) =>
            {
                var userIdClaim = httpUser.FindFirst("sub")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var revoked = await refreshTokens.RevokeAllByUserAsync(userId);

                logger.LogInformation("Revoked {Count} tokens for user {UserId}", revoked, userId);

                return Results.Ok();
            }).RequireAuthorization(Policies.AuthenticatedOnly);
    }

    /// Returns the raw token and persists the hashed token
    private static async Task<string> GenerateAndPersistRefreshToken(Guid userId,
        RefreshToken? refreshTokenFromDatabase,
        IRefreshTokens refreshTokensRepository, IRefreshTokenGenerator tokensGenerator)
    {
        var rawToken = tokensGenerator.Generate();
        var hashedToken = CybersecurityUtils.Hash(rawToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        // TODO: store hashed tokens
        refreshTokenFromDatabase?.RevokedAt = DateTime.UtcNow;
        await refreshTokensRepository.AddAsync(newRefreshToken); // TODO: Ideally a transaction with DELETE
        return rawToken;
    }

    public static void MapCookiesLoginEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LogInDto logInDto, IUserRepository userRepository, IClock clock,
            HttpContext httpContext, ISessionBlacklist _, IPasswordHasher<User> hasher) =>
        {
            var user = await userRepository.GetUserByEmailAsync(logInDto.Email);

            const string dummyHash =
                "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result = hasher.VerifyHashedPassword(user!, passwordToVerify, logInDto.Password);

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
                    await RehashUserPassword(hasher, user, logInDto.Password, userRepository);
                    break;
                }
            }

            var userClaims =
                new JwtUserClaims(user.Id, logInDto.Email, user.Role, user.JobTitle, user.OnboardingStatus).ToClaims();

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

    public static void MapDeleteUserEndpoint(this WebApplication app)
    {
        app.MapDelete("/users/me", async (ClaimsPrincipal httpUser, IUserRepository userRepository) =>
        {
            var userIdClaim = httpUser.FindFirst("sub")?.Value;

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var user = await userRepository.GetUserByIdAsync(userId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (user.Role == UserRole.Admin)
            {
                return Results.Problem(title: "Forbidden", detail: "Admins cannot delete themselves",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            if (user.OnboardingStatus != OnboardingStatus.Done)
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Account deletion is disabled until onboarding is approved",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            var deletedSuccessfully = await userRepository.DeleteUserAsync(userId);
            return deletedSuccessfully ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization(Policies.AuthenticatedOnly);

        app.MapDelete("/users/{id:guid}",
            async (Guid id, ClaimsPrincipal httpUser, HttpContext ctx,
                IUserRepository userRepository) =>
            {
                var userIdClaim = httpUser.FindFirst("sub")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var user = await userRepository.GetUserByIdAsync(userId);
                switch (user)
                {
                    case null:
                        return Results.Unauthorized();
                    case { Role: UserRole.Admin, JobTitle: JobTitle.ItExpert }:
                    {
                        var deleted = await userRepository.DeleteUserAsync(id);
                        return deleted ? Results.Ok() : Results.NotFound();
                    }
                    default:
                        return Results.Problem(
                            title: "Forbidden",
                            detail: "Only It-Expert admins can delete other users' accounts",
                            statusCode: StatusCodes.Status403Forbidden
                        );
                }
            }).RequireAuthorization(Policies.AdminOnly).RequireAuthorization(Policies.OnboardedOnly);
    }
}