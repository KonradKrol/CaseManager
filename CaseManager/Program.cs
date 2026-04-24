using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AutoMapper;
using CaseManager.Auth;
using CaseManager.BackgroundJobs;
using CaseManager.Config;
using CaseManager.DomainModels;
using CaseManager.Dto;
using CaseManager.Middleware;
using CaseManager.Repository;
using CaseManager.Repository.FileSystem;
using CaseManager.Repository.InMemory;
using CaseManager.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using SystemClock = CaseManager.Services.SystemClock;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication("Dev")
        .AddScheme<AuthenticationSchemeOptions, LocalDevAuthenticationHandler>("Dev", _ => { });
}
else
{
    builder.Services.AddJwtBearerAuth(builder.Configuration);
    // builder.Services.AddCookiesAuth(builder.Configuration);
}

builder.Services.AddCaseManagerAuthorization(builder.Configuration);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => { }, typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IJwtAuthService, SemiProdJwtAuthService>(sp =>
    new SemiProdJwtAuthService(builder.Configuration["Jwt:Key"] ?? throw new MissingFieldException("Missing Jwt:Key"),
        sp.GetRequiredService<IClock>()));
builder.Services.AddSingleton<IUserRepository, FileSystemUsers>();
builder.Services.AddSingleton<ICaseRepository, InMemoryCases>();
builder.Services.AddSingleton<ICommentRepository, InMemoryComments>();
builder.Services.AddHostedService<AddMockCommentsJob>();
builder.Services.AddHostedService<AddMockUsersJob>();

builder.Services
    .AddProblemDetails(); // it adds `traceId` for distributed tracking (OpenTelemtry, cloud) and can be passed through many services.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
app.UseRouting();
app.MapOpenApi().AllowAnonymous();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseHsts();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine(app.Environment.EnvironmentName);

app.MapPost("/cases", async (CreateCaseDto createCaseDto, HttpContext context, IMapper mapper,
    IValidator<CreateCaseDto> inputValidator,
    ICaseRepository caseRepository, IUserRepository userRepository) =>

{
    var userSub = context.User.FindFirst("sub")?.Value;
    _ = Guid.TryParse(userSub, out var userId);

    var assignedUserIds = createCaseDto.AssignedTo;
    if (assignedUserIds is not null)
    {
        var notExistingIds = await userRepository.GetNotExistingUserIds(assignedUserIds);
        if (notExistingIds.Any())
        {
            return Results.BadRequest(new Dictionary<string, object> // TODO: Is 409 good?
            {
                ["NotExistingUsers"] = notExistingIds,
            });
        }
    }


    var userExists = await userRepository.UserExists(userId);
    if (!userExists)
    {
        return Results.Unauthorized();
    }

    inputValidator.ValidateAndThrow(createCaseDto);

    var caseId = Guid.NewGuid();
    var newCase = mapper.Map<Case>(createCaseDto, opt =>
    {
        opt.Items["Id"] = caseId;
        opt.Items["AuthorId"] = userId;
        opt.Items["CreatedAt"] = DateTime.UtcNow;
    });

    var createCaseReturnDto = mapper.Map<CreateCaseReturnDto>(newCase);

    await caseRepository.AddCase(newCase);

    return Results.Created($"/cases/{caseId}", createCaseReturnDto);
}).RequireAuthorization(Policies.OnboardedOnly);

app.MapGet("/cases/{id:guid}",
    (Guid id, IMapper mapper) =>
    {
        var loadedCase = new Case(id: id, authorId: Guid.NewGuid(), assignedTo: [Guid.NewGuid(), Guid.NewGuid()],
            createdAt: DateTime.Now,
            status: CaseStatus.Open, description: "Trtalal flallalal",
            title: "Hops hops raz dwa trzy"); // GET FROM DATABASE

        var caseDetailsDto = mapper.Map<CaseDetailsDto>(loadedCase);

        return Results.Ok(caseDetailsDto);
    }).RequireAuthorization(Policies.AuthenticatedOnly);

// TODO: Dodaj filtrowanie (także po zakresie dat)
app.MapGet("/cases", async ([FromQuery] int? pageIndex, IMapper mapper, ILogger<Program> logger,
    ICaseRepository caseRepository) =>
{
    var actualPageIndex = pageIndex ?? 0;
    const int pageLength = 20;
    var startingIndex = actualPageIndex * pageLength;
    var cases = (await caseRepository.GetFirstNCasesByCreatedAt(startingIndex, pageLength)).ToImmutableList();
    if (cases.Count == 0)
    {
        return Results.NoContent();
    }

    var caseDetailsDtos = cases.Select(mapper.Map<CaseDetailsDto>).ToImmutableList();

    logger.LogDebug($"Case details DTOs we wanna return: {string.Join(", ", caseDetailsDtos)}");

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
    ICommentRepository commentRepository, AddCommentDto addCommentDto, IMapper mapper) =>
{
    // var caseExists = await cases.CaseExists(caseId);
    //
    // if (!caseExists)
    // {
    //     return Results.NotFound();
    // }
    // TODO: Czy to i tak będzie wyłapane przez ICommentRepository?

    var commentId = Guid.NewGuid();
    var comment = mapper.Map<Comment>(addCommentDto, options =>
    {
        options.Items["Id"] = commentId;
        options.Items["CaseId"] = caseId;
    });

    await commentRepository.AddComment(comment);

    return Results.Created($"/comments/{caseId}", new { CommentId = commentId });
}).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapGet("/cases/{caseId:guid}/comments", async (Guid caseId, ICommentRepository commentRepository, IMapper mapper) =>
{
    var comments = await commentRepository.GetAllCommentsOf(caseId);
    var commentDtos = comments.Select(mapper.Map<CommentDetailsDto>);

    return Results.Ok(new { Comments = commentDtos });
}).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapGet("/comments/{id:guid}", async (Guid id, ICommentRepository commentRepository, IMapper mapper) =>
{
    var comment = await commentRepository.GetCommentById(id);
    var commentDto = mapper.Map<CommentDetailsDto>(comment);

    return commentDto;
}).RequireAuthorization(Policies.AuthenticatedOnly);

app.MapPost("/users",
    async ([FromBody] RegisterUserDto registerUserDto, IMapper mapper, IValidator<RegisterUserDto> validator,
        IUserRepository userRepository, ILogger<Program> logger) =>
    {
        validator.ValidateAndThrow(registerUserDto);

        if (registerUserDto.Password != registerUserDto.ConfirmPassword)
        {
            logger.LogInformation("Passwords does not match — cannot sign up");
            return Results.BadRequest("Passwords does not match.");
        }

        if (registerUserDto is { Role: "Admin", AdminConfirmation: not "confirm" })
        {
            logger.LogWarning("Wrong AdminConfirmation ({AdminConfirmation})", registerUserDto.AdminConfirmation);
            return Results.Unauthorized();
        }

        var userId = Guid.NewGuid();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerUserDto.Password);

        var user = mapper.Map<User>(registerUserDto, options =>
        {
            options.Items["Id"] = userId;
            options.Items["PasswordHash"] = passwordHash;
        });

        await userRepository.AddUser(user);

        var responseDictionary = new Dictionary<string, object>
        {
            ["Id"] = userId,
        };

        return Results.Created("", responseDictionary);
    }).RequireAuthorization(Policies.AdminOnly);

app.MapGet("/users", async (IMapper mapper, IUserRepository userRepository, ILogger<Program> logger) =>
{
    var users = (await userRepository.GetAllUsers()).ToList();

    var userDetailsDtos = users.Select(mapper.Map<UserDetailsDto>);

    return Results.Ok(userDetailsDtos);
}).RequireAuthorization(Policies.AdminOnly);

app.MapJwtBearerLoginEndpoint();
app.MapDebugClaimsEndpoint();

app.MapScalarApiReference().AllowAnonymous();
app.Run();