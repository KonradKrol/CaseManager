using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using CaseManager.Auth;
using CaseManager.Auth.RefreshTokens;
using CaseManager.BackgroundJobs;
using CaseManager.Config;
using CaseManager.DomainModels;
using CaseManager.Dto;
using CaseManager.Exceptions;
using CaseManager.HealthChecks;
using CaseManager.Loggers;
using CaseManager.Middleware;
using CaseManager.Repository;
using CaseManager.Repository.FileSystem;
using CaseManager.Repository.InMemory;
using CaseManager.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using SystemClock = CaseManager.Services.SystemClock;

var builder = WebApplication.CreateBuilder(args);

// TODO: Introduce Application Services (~ Use cases)

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddJwtBearerAuth(builder.Configuration);
    // builder.Services.AddCookiesAuth(builder.Configuration, environment: builder.Environment);
    builder.Services.AddSingleton<ISessionBlacklist, InMemorySessionBlacklist>();

    // builder.Services
    //     .AddAuthentication("Dev")
    //     .AddScheme<AuthenticationSchemeOptions, LocalDevAuthenticationHandler>("Dev", _ => { });
}
else
{
    builder.Services.AddJwtBearerAuth(builder.Configuration);
    // builder.Services.AddCookiesAuth(builder.Configuration, environment: builder.Environment);
    builder.Services.AddSingleton<ISessionBlacklist, InMemorySessionBlacklist>();
}

builder.Services.AddPasswordHasher();

builder.Services.AddCaseManagerAuthorization(builder.Configuration);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => { }, typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IRefreshTokenGenerator, DefaultRefreshTokenGenerator>();
builder.Services.AddSingleton<IJwtUserClaimsFactory, DefaultJwtUserClaimsFactory>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IJwtAuthService, SemiProdJwtAuthService>(sp =>
    new SemiProdJwtAuthService(builder.Configuration["Jwt:Key"] ?? throw new MissingFieldException("Missing Jwt:Key"),
        sp.GetRequiredService<IClock>(), new Logger<SemiProdJwtAuthService>(sp.GetRequiredService<ILoggerFactory>())));
builder.Services.AddSingleton<IRefreshTokens, InMemoryRefreshTokens>();
builder.Services.AddSingleton<IUserRepository, FileSystemUsers>();
builder.Services.AddSingleton<ICaseRepository, InMemoryCases>();
builder.Services.AddSingleton<ICommentRepository, InMemoryComments>();
builder.Services.AddHostedService<AddMockCommentsJob>();
builder.Services.AddHostedService<AddMockUsersJob>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields =
            HttpLoggingFields.RequestMethod |
            HttpLoggingFields.RequestPath |
            HttpLoggingFields.ResponseStatusCode |
            HttpLoggingFields.Duration;

        options.CombineLogs = true;
    });
}

builder.Services
    .AddProblemDetails(); // it adds `traceId` for distributed tracking (OpenTelemtry, cloud) and can be passed through many services.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddCheck<UsersRepositoryHealthCheck>("users", tags: ["ready", "critical"])
    .AddCheck<JwtHealthCheck>("jwt", tags: ["ready", "critical"]);
// .AddCheck<DatabaseHealthCheck>("db", tags: ["ready", "critical"]);

var serilogOutput = builder.Configuration["SerilogOutput"];
Console.WriteLine($"Serilog output is {serilogOutput}");
Log.Logger = serilogOutput
             == "Aws"
    ? SerilogFactories.CreateCloudWatchLogger(builder.Configuration)
    : SerilogFactories.CreateSeqLogger(builder.Configuration);

builder.Host.UseSerilog();

var app = builder.Build();
app.UseRouting();
app.MapOpenApi().AllowAnonymous();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseHsts();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LoggingContextMiddleware>();
app.UseHttpLogging();

app.MapControllers();

Console.WriteLine(app.Environment.EnvironmentName);

app.MapPost("/cases", async (CreateCaseDto createCaseDto, HttpContext context, IMapper mapper,
    IValidator<CreateCaseDto> inputValidator,
    ICaseRepository caseRepository, IUserRepository userRepository, ILoggerFactory loggerFactory) =>

{
    var logger = loggerFactory.CreateLogger("Endpoints.Cases.Post");

    var userSub = context.User.FindFirst("sub")?.Value;
    _ = Guid.TryParse(userSub, out var userId);

    var userExists = await userRepository.UserExistsAsync(userId);
    if (!userExists)
    {
        logger.LogWarning("Not existing user tried to create a case");
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
    }

    logger.LogInformation("Requested to create a case.");

    var assignedUserIds = createCaseDto.AssignedTo;
    if (assignedUserIds is not null)
    {
        var notExistingIds = await userRepository.GetNotExistingUserIdsAsync(assignedUserIds);
        if (notExistingIds.Any())
        {
            logger.LogWarning("Tried to assign not existing users to the case");
            return Results.Problem(
                title: "Some users do not exist",
                detail: "One or more provided user IDs were not found.",
                statusCode: 400,
                extensions: new Dictionary<string, object?>
                {
                    ["notExistingUsers"] = notExistingIds
                });
        }
    }

    inputValidator.ValidateAndThrow(createCaseDto);

    var caseId = Guid.NewGuid();
    var newCase = mapper.Map<Case>(createCaseDto, opt =>
    {
        opt.Items["Id"] = caseId;
        opt.Items["AuthorId"] = userId;
        opt.Items["CreatedAt"] = DateTime.UtcNow;
    });

    using (logger.BeginScope(new Dictionary<string, object>
           {
               ["CaseId"] = caseId
           }))
    {
        var createCaseReturnDto = mapper.Map<CreateCaseReturnDto>(newCase);

        await caseRepository.AddCase(newCase);

        logger.LogInformation("Created a case.");

        return Results.Created($"/cases/{caseId}", createCaseReturnDto);
    }
}).RequireAuthorization(Policies.OnboardedOnly);

app.MapGet("/cases/{id:guid}",
    async (Guid id, IMapper mapper, ICaseRepository caseRepository, ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("Endpoints.Cases.GetById");
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CaseId"] = id,
               }))
        {
            var loadedCase = await caseRepository.GetCaseById(id);

            if (loadedCase is null)
            {
                logger.LogWarning("Case not found.");
                return Results.Problem(statusCode: StatusCodes.Status404NotFound);
            }

            var caseDetailsDto = mapper.Map<CaseDetailsDto>(loadedCase);

            return Results.Ok(caseDetailsDto);
        }
    }).RequireAuthorization(Policies.AuthenticatedOnly);

// TODO: Dodaj filtrowanie (także po zakresie dat)
app.MapGet("/cases", async ([FromQuery] int? pageIndex, IMapper mapper, ILoggerFactory loggerFactory,
    ICaseRepository caseRepository) =>
{
    var logger = loggerFactory.CreateLogger("Endpoints.Cases.Get");

    var actualPageIndex = pageIndex ?? 0;
    const int pageLength = 20;
    var startingIndex = actualPageIndex * pageLength;
    var cases = (await caseRepository.GetFirstNCasesByCreatedAt(startingIndex, pageLength)).ToImmutableList();
    if (cases.Count == 0)
    {
        return Results.NoContent();
    }

    var caseDetailsDtos = cases.Select(mapper.Map<CaseDetailsDto>).ToImmutableList();

    var response = new Dictionary<string, object>
    {
        ["pageIndex"] = actualPageIndex,
        ["cases"] = caseDetailsDtos,
    };

    return Results.Ok(response);
}).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapPatch("/cases/{id:guid}", (System.Guid id, IMapper mapper) => { return Results.NoContent(); })
    .RequireAuthorization(Policies.CaseAuthorOrActiveAdmin);

app.MapPatch("/cases/{id:guid}/history", (Guid id, IMapper mapper) => { return Results.NoContent(); })
    .RequireAuthorization(Policies.CaseAuthorOrActiveAdmin);

app.MapPost("/cases/{caseId:guid}/comments", async (Guid caseId, ICaseRepository cases,
    ICommentRepository commentRepository, AddCommentDto addCommentDto, IMapper mapper, HttpContext context,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("Endpoints.Cases.Comments.Post");

    // TODO: I can move the scope here

    var caseExists = await cases.CaseExists(caseId);

    if (!caseExists)
    {
        logger.LogWarning("Case {CaseId} not found", caseId);
        return Results.Problem(statusCode: StatusCodes.Status404NotFound);
    }

    var sub = context.User.FindFirst("sub")?.Value;
    var parsedUserId = Guid.TryParse(sub ?? "", out var userId);
    if (!parsedUserId)
    {
        logger.LogWarning("Invalid or missing user id in claims");
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
    }

    var commentId = Guid.NewGuid();

    using (logger.BeginScope(new Dictionary<string, object>
           {
               ["CaseId"] = caseId,
               ["CommentId"] = commentId,
           }))
    {
        var comment = mapper.Map<Comment>(addCommentDto, options =>
        {
            options.Items["Id"] = commentId;
            options.Items["CaseId"] = caseId;
            options.Items["UserId"] = userId;
        });

        await commentRepository.AddComment(comment);

        logger.LogInformation(
            "Comment created");

        return Results.Created($"/comments/{commentId}", new { CommentId = commentId });
    }
}).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapGet("/cases/{caseId:guid}/comments",
    async (Guid caseId, ICommentRepository commentRepository, IMapper mapper, ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("Endpoints.Cases.Comments.Get");

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CaseId"] = caseId
               }))
        {
            try
            {
                var comments = await commentRepository.GetAllCommentsOf(caseId);

                var commentDtos = comments.Select(mapper.Map<CommentDetailsDto>);

                return Results.Ok(new { Comments = commentDtos });
            }
            catch (CaseNotExistsException)
            {
                logger.LogWarning("Haven't found a case with ID: {CaseId}", caseId);
                return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?>()
                    {
                        ["CaseId"] = caseId,
                    });
            }
        }
    }).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapGet("/comments/{id:guid}",
    async (Guid id, ICommentRepository commentRepository, IMapper mapper, ILoggerFactory loggerFactory) =>
    {
        var logger = loggerFactory.CreateLogger("Endpoints.Comments.Get");
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CommentId"] = id
               }))
        {
            var comment = await commentRepository.GetCommentById(id);

            if (comment is null)
            {
                logger.LogInformation("Comment not found");
                return Results.Problem(statusCode: StatusCodes.Status404NotFound);
            }

            var commentDto = mapper.Map<CommentDetailsDto>(comment);

            return Results.Ok(commentDto);
        }
    }).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapPost("/users",
    async ([FromBody] RegisterUserDto registerUserDto, IMapper mapper, IValidator<RegisterUserDto> validator,
        IUserRepository userRepository, ILoggerFactory loggerFactory, IPasswordHasher<User> hasher) =>
    {
        var logger = loggerFactory.CreateLogger("Endpoints.Users.Post");

        logger.LogInformation("Requested to sign up a user with title: {JobTitle}. Skips onboarding: {SkipOnboarding}",
            registerUserDto.JobTitle, registerUserDto.SkipOnboarding);

        validator.ValidateAndThrow(registerUserDto);

        if (registerUserDto.Password != registerUserDto.ConfirmPassword)
        {
            logger.LogInformation("Sign up attempt failed");
            return Results.Problem(title: "Passwords does not match.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (registerUserDto is { Role: "Admin", AdminConfirmation: not "confirm" })
        {
            logger.LogInformation("Sign up attempt failed");
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        var userId = Guid.NewGuid();

        var passwordHash = hasher.HashPassword(null!, registerUserDto.Password);

        var user = mapper.Map<User>(registerUserDto, options =>
        {
            options.Items["Id"] = userId;
            options.Items["PasswordHash"] = passwordHash;
        });

        await userRepository.AddUserAsync(user);

        var responseDictionary = new Dictionary<string, object>
        {
            ["Id"] = userId,
        };

        logger.LogInformation("Signed up a user with ID: {userId} and title: {JobTitle}.", user.Id,
            user.JobTitle.ToString());

        return Results.Created("", responseDictionary);
    }).RequireAuthorization(Policies.AdminOnly);

app.MapGet("/users",
    async (IMapper mapper, IUserRepository userRepository, ILoggerFactory loggerFactory, ClaimsPrincipal httpUser) =>
    {
        var logger = loggerFactory.CreateLogger("Endpoints.Users.Get");

        var users = (await userRepository.GetAllUsersAsync()).ToList();

        var userDetailsDtos = users.Select(mapper.Map<UserDetailsDto>);

        var userId = httpUser.FindFirstValue("sub");

        return Results.Ok(userDetailsDtos);
    }).RequireAuthorization(Policies.AdminOnly);

app.MapJwtBearerLoginEndpoints();

// app.MapCookiesLoginEndpoint();
// app.MapCookiesLogoutEndpoint();

app.MapDebugClaimsEndpoint();
app.MapDeleteUserEndpoint();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

// Visible in Scalar UI
app.MapGet("/health/details", async (HealthCheckService healthChecks) =>
    {
        var report = await healthChecks.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                durationMs = x.Value.Duration.TotalMilliseconds
            })
        };

        var statusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return Results.Json(response, statusCode: statusCode);
    })
    .WithTags("Health")
    .WithSummary("Health details")
    .RequireAuthorization(Policies.ItExpertOrAdmin);

app.MapScalarApiReference().AllowAnonymous();
app.Run();