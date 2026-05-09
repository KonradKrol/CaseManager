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
    public static void MapJwtBearerLoginEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async ([FromBody] LogInDto logInDto, IUserRepository userRepository,
            IJwtAuthService authService, IPasswordHasher<User> hasher, IJwtUserClaimsFactory userClaimsFactory,
            IRefreshTokens refreshTokens, IRefreshTokenGenerator refreshTokenGenerator, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Endpoints.Auth.Jwt.Login");

            var user = await userRepository.GetUserByEmailAsync(logInDto.Email);

            const string dummyHash =
                "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result =
                hasher.VerifyHashedPassword(user!, passwordToVerify,
                    logInDto.Password); // TODO: Don't use null-coallesce

            if (user is null)
            {
                logger.LogWarning(
                    "Failed login attempt for email {Email}",
                    logInDto.Email);
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
            }

            var userId = user.Id;

            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       ["UserId"] = userId
                   }))
            {
                switch (result)
                {
                    case PasswordVerificationResult.Failed:
                        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
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

                logger.LogInformation("Logged in");

                return Results.Ok(new { AccessToken = jwt, RefreshToken = rawToken });
            }
        }).AllowAnonymous();

        app.MapPost("/auth/login_with_refresh_token", async (
            [FromBody] LogInWithRefreshTokenDto logInWithRefreshTokenDto, IRefreshTokens refreshTokens,
            IRefreshTokenGenerator refreshTokenGenerator, IUserRepository userRepository,
            IJwtUserClaimsFactory userClaimsFactory, IJwtAuthService authService, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Endpoints.Auth.Jwt.LoginWithRefreshToken");

            var inputToken = logInWithRefreshTokenDto.RefreshToken;
            var tokenFromDatabase = await refreshTokens.GetTokenAsync(inputToken);

            if (tokenFromDatabase is null || tokenFromDatabase.HasExpired(DateTime.UtcNow))
            {
                logger.LogWarning("Invalid or expired refresh token login attempt");
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
            }

            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       ["RefreshTokenId"] = tokenFromDatabase.Id,
                       ["UserId"] = tokenFromDatabase.UserId,
                   }))
            {
                var userId = tokenFromDatabase.UserId;

                var latestToken = await refreshTokens.GetLatestUserTokenAsync(userId);

                var moreRecentTokenExists = latestToken is not null && latestToken.Id != tokenFromDatabase.Id;

                if (tokenFromDatabase.RevokedAt is not null || moreRecentTokenExists)
                {
                    logger.LogWarning("Tried to use old refresh token — revoking all tokens");
                    var howManyRevoked = await refreshTokens.RevokeAllByUserAsync(userId);
                    logger.LogDebug("Revoked {HowManyRevoked} refresh tokens after using an old one",
                        howManyRevoked);
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                var user = await userRepository.GetUserByIdAsync(userId);
                if (user is null)
                {
                    logger.LogWarning(
                        "Refresh token belongs to non-existing user");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                var claims = userClaimsFactory.Create(user);

                var jwt = authService.GenerateJwt(claims);

                var rawToken =
                    await GenerateAndPersistRefreshToken(userId, tokenFromDatabase, refreshTokens,
                        refreshTokenGenerator);

                logger.LogInformation("User logged in via refresh token");

                return Results.Ok(new { AccessToken = jwt, RefreshToken = rawToken });
            }
        }).AllowAnonymous();

        app.MapDelete("/auth/refresh_tokens",
            async (ClaimsPrincipal httpUser, IRefreshTokens refreshTokens, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Endpoints.Auth.Jwt.RefreshTokens.Delete");

                var userIdClaim = httpUser.FindFirst("sub")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("Request without valid 'sub' claim attempted to revoke refresh tokens");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                var revokedCount = await refreshTokens.RevokeAllByUserAsync(userId);

                logger.LogInformation("Revoked {RevokedTokensCount} tokens for user {UserId}", revokedCount, userId);

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
            HttpContext httpContext, ISessionBlacklist _, IPasswordHasher<User> hasher, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Endpoints.Auth.Cookies.Login");

            var user = await userRepository.GetUserByEmailAsync(logInDto.Email);

            const string dummyHash =
                "AQAAAAIAAw1AAAAAEDthRHNbQDbCZSbmNjkfdTa0EJD6HSqxf1zGIxIn7tC0weEBcWo2USOXP42N6se41w==";

            var passwordToVerify = user?.PasswordHash ?? dummyHash;

            var result = hasher.VerifyHashedPassword(user!, passwordToVerify, logInDto.Password);

            if (user is null)
            {
                logger.LogWarning(
                    "Failed login attempt for email {Email}",
                    logInDto.Email);
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
            }

            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       ["UserId"] = user.Id
                   }))
            {
                switch (result)
                {
                    case PasswordVerificationResult.Failed:
                        logger.LogWarning(
                            "Failed login attempt for email {Email}",
                            logInDto.Email);
                        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                    case PasswordVerificationResult.Success:
                        break;
                    case PasswordVerificationResult.SuccessRehashNeeded:
                    {
                        await RehashUserPassword(hasher, user, logInDto.Password, userRepository);
                        break;
                    }
                }

                var userClaims =
                    new JwtUserClaims(user.Id, logInDto.Email, user.Role, user.JobTitle, user.OnboardingStatus)
                        .ToClaims();

                var sessionId = Guid.NewGuid().ToString();

                using (logger.BeginScope(new Dictionary<string, object>
                       {
                           ["SessionId"] = sessionId
                       }))
                {
                    var otherClaims = new List<Claim>
                    {
                        new("session_id", sessionId)
                    };

                    var claims = userClaims.Concat(otherClaims);

                    var identity = new ClaimsIdentity(claims: claims, authenticationType: "Cookies",
                        roleType: Claims.Role);
                    var principal = new ClaimsPrincipal(identity);

                    await httpContext.SignInAsync("Cookies", principal);

                    logger.LogInformation("User logged in (returning cookies).");

                    return Results.Ok();
                }
            }
        }).AllowAnonymous();
    }

    private static async Task RehashUserPassword(IPasswordHasher<User> hasher, User user, string inputPassword,
        IUserRepository userRepository)
    {
        var newHash = hasher.HashPassword(user, inputPassword);
        var updatedUser = user.UpdatePassword(newHash);
        await userRepository.UpdateUserAsync(updatedUser);
    }

    // TODO: Logout: jwt bearer
    public static void MapCookiesLogoutEndpoint(this WebApplication app)
    {
        app.MapPost("/auth/logout",
            async (ISessionBlacklist sessionBlacklist, HttpContext context, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Endpoints.Auth.Cookies.Logout");
                var sessionId = context.User.FindFirst("session_id")?.Value;

                if (sessionId is null)
                {
                    logger.LogWarning("Request without valid 'session_id' claim attempted to log-out");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                using (logger.BeginScope(new Dictionary<string, object>
                       {
                           ["SessionId"] = sessionId
                       }))
                {
                    await sessionBlacklist.RevokeSession(sessionId);

                    await context.SignOutAsync("Cookies");

                    logger.LogInformation(
                        "Logged out");

                    return Results.Ok();
                }
            });
    }

    public static void MapDebugClaimsEndpoint(this WebApplication app)
    {
        app.MapGet("/debug_claims",
                (HttpContext ctx, ILoggerFactory loggerFactory) =>
                {
                    var logger = loggerFactory.CreateLogger("Endpoints.DebugClaims");

                    logger.LogDebug(
                        "Requested claims debug");

                    return ctx.User.Claims.Select(c => new { c.Type, c.Value });
                })
            .RequireAuthorization(Policies.ItExpertOrAdmin);
    }

    public static void MapDeleteUserEndpoint(this WebApplication app)
    {
        app.MapDelete("/users/me",
            async (ClaimsPrincipal httpUser, IUserRepository userRepository, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Endpoints.Users.Me.Delete");

                var userIdClaim = httpUser.FindFirst("sub")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("Request without valid 'sub' claim attempted user self-deletion");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                logger.LogInformation(
                    "Requested self-deletion");

                var user = await userRepository.GetUserByIdAsync(userId);
                if (user is null)
                {
                    logger.LogWarning(
                        "User entity not found during self-deletion");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                if (user.Role == UserRole.Admin)
                {
                    logger.LogWarning("Administrator tried to delete themselves");
                    return Results.Problem(title: "Forbidden", detail: "Admins cannot delete themselves",
                        statusCode: StatusCodes.Status403Forbidden);
                }

                if (user.OnboardingStatus != OnboardingStatus.Done)
                {
                    logger.LogWarning("Not onboarded user tried to delete themselves");

                    return Results.Problem(
                        title: "Forbidden",
                        detail: "Account deletion is disabled until onboarding is approved",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                var deletedSuccessfully = await userRepository.DeleteUserAsync(userId);

                if (deletedSuccessfully)
                {
                    logger.LogInformation("User deleted themselves.");
                }
                else
                {
                    logger.LogWarning(
                        "Failed to delete user {UserId} because repository returned false",
                        user.Id);
                }

                return deletedSuccessfully
                    ? Results.Ok()
                    : Results.Problem(statusCode: 500, title: "Deletion failed"); // Always use ProblemDetails
            }).RequireAuthorization(Policies.AuthenticatedOnly);

        app.MapDelete("/users/{id:guid}",
            async (Guid id, ClaimsPrincipal httpUser, HttpContext ctx,
                IUserRepository userRepository, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Endpoints.Users.Delete");

                var targetUserId = id;

                var userIdClaim = httpUser.FindFirst("sub")?.Value;

                if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                {
                    logger.LogWarning("Request without valid 'sub' claim attempted admin-access user deletion");
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                }

                using (logger.BeginScope(new
                       {
                           DeletionTargetUserId = targetUserId, AdminUserId = userId
                       })) // I defensively add AdminUserId if middleware changes and we won't have UserId
                {
                    logger.LogInformation("Admin requested user deletion");

                    var adminUser = await userRepository.GetUserByIdAsync(userId);
                    switch (adminUser)
                    {
                        case null:
                            logger.LogWarning(
                                "User not authorized to delete other users");
                            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
                        case { Role: UserRole.Admin, JobTitle: JobTitle.ItExpert }:
                        {
                            var deleted = await userRepository.DeleteUserAsync(targetUserId);

                            if (deleted)
                            {
                                logger.LogInformation("User deleted by admin");
                            }
                            else
                            {
                                logger.LogWarning(
                                    "User deletion failed (repository returned false)");
                            }

                            return deleted ? Results.Ok() : Results.Problem(statusCode: 500, title: "Deletion failed");
                        }
                        default:
                            var reason = "Only It-Expert admins can delete other users' accounts";
                            logger.LogWarning(
                                "Failed to delete a user: {Reason}",
                                reason);
                            return Results.Problem(
                                title: "Forbidden",
                                detail: reason,
                                statusCode: StatusCodes.Status403Forbidden
                            );
                    }
                }
            }).RequireAuthorization(Policies.AdminOnly).RequireAuthorization(Policies.OnboardedOnly);
    }
}