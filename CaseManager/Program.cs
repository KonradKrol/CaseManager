using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAutoMapper((_, _) => {}, typeof(Program));

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

// TODO: Add a git repo
app.MapPost("/createCase")
{

}

app.Run();