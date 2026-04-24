using System.Text;
using CaseManager.Auth.Requirements;
using CaseManager.DomainModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CaseManager.Auth;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddJwtBearerAuth(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSecurityKey = config["Jwt:Key"] ?? throw new MissingFieldException("You must provide the Jwt:Key");

        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.Authority = "https://auth.case-manager-internal-api.eu";
                options.Audience = "case-manager";

                options.RequireHttpsMetadata = true;

                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = Claims.Name,
                    RoleClaimType = Claims.Role,
                    ValidateIssuer = true, // aka authority
                    ValidIssuer = "https://auth.case-manager-internal-api.eu",
                    ValidateAudience = true,
                    ValidateLifetime = true, // sprawdza nbf (not before) i exp (expiration). Czy przeterminowany?
                    ValidateIssuerSigningKey = true, // sprawdzaj, czy signature się zgadza
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine(ctx.Exception);
                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    public static IServiceCollection AddCookiesAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Cookies";
                options.DefaultSignInScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };

                options.Cookie.HttpOnly = true; // hides it from JS
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // enables HTTPS
                options.LoginPath = "/auth/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                options.SlidingExpiration = true;
            });

        return services;
    }

    public static IServiceCollection AddCaseManagerAuthorization(this IServiceCollection services,
        IConfiguration config)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.OnboardedOnly,
                builder => { builder.RequireClaim(Claims.OnboardingStatus, Claims.OnboardingStatuses.Done); })
            .AddPolicy(Policies.AdminOnly, builder => { builder.RequireRole(Claims.Roles.Admin); })
            .AddPolicy(Policies.AuthenticatedOnly, builder => { builder.RequireAuthenticatedUser(); })
            // .AddPolicy("EnsureUserExists") // TODO: Use .AddRequirements and handle not existing user for sensitive endpoints
            .AddPolicy(Policies.CaseAuthorOrActiveAdmin,
                builder => { builder.AddRequirements(new CaseAuthorOrActiveAdminRequirement()); })
            .AddPolicy(Policies.ItExpertOrAdmin,
                builder =>
                {
                    builder.AddRequirements(new JobTitleOrAdminRequirement { JobTitle = JobTitle.ItExpert });
                });

        services.AddScoped<IAuthorizationHandler, CaseAuthorOrActiveAdminHandler>();
        services.AddScoped<IAuthorizationHandler, JobTitleOrAdminHandler>();
        return services;
    }
}