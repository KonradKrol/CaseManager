using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using CaseManager.Auth;
using CaseManager.BackgroundJobs;
using CaseManager.Dto;
using CaseManager.Middleware;
using CaseManager.DomainModels;
using CaseManager.Repository;
using CaseManager.Repository.InMemory;
using CaseManager.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserRole = CaseManager.DomainModels.UserRole;

var builder = WebApplication.CreateBuilder(args);

var jwtSecurityKey =  builder.Configuration["Jwt:Key"] ?? throw new MissingFieldException("You must provide the Jwt:Key");
builder.Services.AddJwtBearerAuth(builder.Configuration, jwtSecurityKey);

// builder.Services.AddCookiesAuth(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => { }, typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IJwtAuthService, SemiProdJwtAuthService>(sp =>
    new SemiProdJwtAuthService(builder.Configuration["Jwt:Key"] ?? throw new MissingFieldException("Missing Jwt:Key"),
        sp.GetRequiredService<IClock>()));
builder.Services.AddSingleton<IUserRepository, InMemoryUsers>();
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

app.MapPost("/cases", async (CreateCaseDto createCaseDto, HttpContext context, IMapper mapper,
    IValidator<CreateCaseDto> inputValidator,
    ICaseRepository caseRepository) =>

{
    inputValidator.ValidateAndThrow(createCaseDto);

    var userId = context.User.FindFirst("sub")?.Value;

    var caseId = Guid.NewGuid();

    var newCase = mapper.Map<Case>(createCaseDto, opt =>
    {
        opt.Items["Id"] = caseId;
        opt.Items["CreatedAt"] = DateTime.UtcNow;
    });

    var createCaseReturnDto = mapper.Map<CreateCaseReturnDto>(newCase);

    await caseRepository.AddCase(newCase);

    return Results.Created($"/cases/{caseId}", createCaseReturnDto);
}).RequireAuthorization(policyBuilder => policyBuilder.RequireRole(Claims.Roles.Admin));

app.MapGet("/cases/{id:guid}",
    (Guid id, IMapper mapper) =>
    {
        var loadedCase = new Case(id: id, assignedTo: [Guid.NewGuid(), Guid.NewGuid()], createdAt: DateTime.Now,
            status: CaseStatus.Open, description: "Trtalal flallalal",
            title: "Hops hops raz dwa trzy"); // GET FROM DATABASE

        var caseDetailsDto = mapper.Map<CaseDetailsDto>(loadedCase);

        return Results.Ok(caseDetailsDto);
    }).RequireAuthorization(policyBuilder => policyBuilder.RequireAuthenticatedUser());

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

    var response = new Dictionary<string, object>()
    {
        ["pageIndex"] = actualPageIndex,
        ["cases"] = caseDetailsDtos,
    };

    return Results.Ok(response);
}).RequireAuthorization(policyBuilder => policyBuilder.RequireAuthenticatedUser());

app.MapPatch("/cases", (IMapper mapper) => { return Results.NoContent(); });

app.MapPatch("/cases/{id}/history", (IMapper mapper) => { return Results.NoContent(); });

app.MapJwtBearerLoginEndpoint();
app.MapDebugClaimsEndpoint();

app.MapScalarApiReference().AllowAnonymous();
app.Run();