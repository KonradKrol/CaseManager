using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CaseManager.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next)
{
    // Wiele działań przeprowadzamy przez mutowanie `context`
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            var problemDetails = e switch
            {
                ValidationException ve =>
                    new ProblemDetails
                    {
                        Title = "Validation has failed",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "One or more validation errors occurred.",
                        Instance = context.Request.Path,
                        Extensions =
                        {
                            ["errors"] = ve.Errors
                                .GroupBy(x => x.PropertyName)
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.Select(x => x.ErrorMessage).ToArray()
                                )
                        }
                    },
                _ =>
                    new ProblemDetails
                    {
                        Title = "An error occured",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = e.Message,
                        Instance = context.Request.Path
                    }
            };

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Request.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}