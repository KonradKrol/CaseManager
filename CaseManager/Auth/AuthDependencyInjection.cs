using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CaseManager.Auth;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddJwtBearerAuth(
        this IServiceCollection services, IConfiguration config, string securityKey)
    {
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
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey))
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
                options.Cookie.HttpOnly = true; // hides it from JS
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // enables HTTPS
                options.LoginPath = "/auth/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                options.SlidingExpiration = true;
            });

        return services;
    }
}