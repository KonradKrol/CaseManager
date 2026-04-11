using System.Text.Json.Serialization;
using AutoMapper;
using CaseManager;
using CaseManager.Dto;
using CaseManager.Factories;
using CaseManager.Middleware;
using CaseManager.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => { }, typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IUserFactory, RoleBasedUserFactory>();
builder.Services
    .AddProblemDetails(); // it adds `traceId` for distributed tracking (OpenTelemtry, cloud) and can be passed through many services.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.MapOpenApi();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapPost("/cases",
    (CreateCaseDto createCaseDto, IMapper mapper, IValidator<CreateCaseDto> inputValidator,
        IValidator<CreateCaseReturnDto> outputValidator) =>
    {
        inputValidator.ValidateAndThrow(createCaseDto);

        var newCase = mapper.Map<Case>(createCaseDto, opt =>
        {
            opt.Items["Id"] = Guid.NewGuid();
            opt.Items["CreatedAt"] = DateTime.UtcNow;
        });

        var createCaseReturnDto = mapper.Map<CreateCaseReturnDto>(newCase);
        outputValidator.ValidateAndThrow(createCaseReturnDto);
        return Results.Ok(createCaseReturnDto);
    });

app.MapGet("/cases/{id:guid}",
    (Guid id, IMapper mapper,
        IValidator<CaseDetailsDto> outputValidator) =>
    {
        var loadedCase = new Case()
        {
            Id = id,
            AssignedTo = [Guid.NewGuid(), Guid.NewGuid()],
            CreatedAt = DateTime.Now,
            Status = CaseStatus.Open,
            Description = "Trtalal flallalal",
            Title = "Hops hops raz dwa trzy"
        }; // GET FROM DATABASE

        var caseDetailsDto = mapper.Map<CaseDetailsDto>(loadedCase);
        outputValidator.ValidateAndThrow(caseDetailsDto);
        return Results.Ok(caseDetailsDto);
    });

// TODO: Dodaj filtrowanie (także po zakresie dat)
app.MapGet("cases",
    ([FromQuery] string? filterByStatus, [FromQuery] string? filterByKeyword, IMapper mapper) =>
    {
        return Results.NoContent();
    });

app.MapPatch("cases", (IMapper mapper) => { return Results.NoContent(); });

app.MapPatch("/cases/{id}/history", (IMapper mapper) => { return Results.NoContent(); });

app.MapPost("/auth/login", (IMapper mapper) => { return Results.Forbid(); });


app.MapControllers();
app.MapScalarApiReference();
app.Run();