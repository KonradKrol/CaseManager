using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AutoMapper;
using CaseManager;
using CaseManager.BackgroundJobs;
using CaseManager.Dto;
using CaseManager.Middleware;
using CaseManager.DomainModels;
using CaseManager.Repository;
using CaseManager.Repository.InMemory;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => { }, typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IUserRepository, InMemoryUsers>();
builder.Services.AddSingleton<ICaseRepository, InMemoryCases>();
builder.Services.AddSingleton<ICommentRepository, InMemoryComments>();
builder.Services.AddHostedService<AddMockCommentsJob>();

builder.Services
    .AddProblemDetails(); // it adds `traceId` for distributed tracking (OpenTelemtry, cloud) and can be passed through many services.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// TODO: Niespójność: niektóre responses mają traceId, niektóre nie.

app.MapOpenApi();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();

// TODO: Endpointy PUT powinny zwrócić 409 gdy chcemy zrobić to samo drugi raz (zachowujemy idempotencję)

// Post is NOT idempotent
app.MapPost("/cases", async (CreateCaseDto createCaseDto, IMapper mapper, IValidator<CreateCaseDto> inputValidator,
    IValidator<CreateCaseReturnDto> outputValidator, ICaseRepository caseRepository) =>
{
    inputValidator.ValidateAndThrow(createCaseDto);

    var caseId = Guid.NewGuid();

    var newCase = mapper.Map<Case>(createCaseDto, opt =>
    {
        opt.Items["Id"] = caseId;
        opt.Items["CreatedAt"] = DateTime.UtcNow;
    });

    var createCaseReturnDto = mapper.Map<CreateCaseReturnDto>(newCase);
    outputValidator.ValidateAndThrow(createCaseReturnDto);

    await caseRepository.AddCase(newCase);

    return Results.Created($"/cases/{caseId}", createCaseReturnDto);
});

app.MapGet("/cases/{id:guid}",
    (Guid id, IMapper mapper,
        IValidator<CaseDetailsDto> outputValidator) =>
    {
        var loadedCase = new Case(id: id, assignedTo: [Guid.NewGuid(), Guid.NewGuid()], createdAt: DateTime.Now,
            status: CaseStatus.Open, description: "Trtalal flallalal", title: "Hops hops raz dwa trzy"); // GET FROM DATABASE

        var caseDetailsDto = mapper.Map<CaseDetailsDto>(loadedCase);
        outputValidator.ValidateAndThrow(caseDetailsDto);
        return Results.Ok(caseDetailsDto);
    });

// TODO: Dodaj filtrowanie (także po zakresie dat)
app.MapGet("cases", async ([FromQuery] int? pageIndex, IMapper mapper, ILogger<Program> logger,
    ICaseRepository caseRepository, IValidator<CaseDetailsDto> outputValidator) =>
{
    var actualPageIndex = pageIndex ?? 0;
    const int pageLength = 2;
    var startingIndex = actualPageIndex * pageLength;
    var cases = (await caseRepository.GetFirstNCasesByCreatedAt(startingIndex, pageLength)).ToImmutableList();
    if (cases.Count == 0)
    {
        return Results.NoContent();
    }

    var caseDetailsDtos = cases.Select(mapper.Map<CaseDetailsDto>).ToImmutableList();

    logger.LogDebug($"Case details DTOs we wanna return: {string.Join(", ", caseDetailsDtos)}");

    outputValidator.ValidateOutputDtosAndThrowFirstError(caseDetailsDtos);

    var response = new Dictionary<string, object>()
    {
        ["pageIndex"] = actualPageIndex,
        ["cases"] = caseDetailsDtos,
    };

    return Results.Ok(response);
});

app.MapPatch("cases", (IMapper mapper) => { return Results.NoContent(); });

app.MapPatch("/cases/{id}/history", (IMapper mapper) => { return Results.NoContent(); });

app.MapPost("/auth/login", (IMapper mapper) => { return Results.Forbid(); });


app.MapControllers();
app.MapScalarApiReference();
app.Run();