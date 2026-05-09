using CaseManager.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CaseManager.Middleware;

// TODO: Don't throw validation exceptions — just return 400 in controllers.
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception,
            $"Unhandled exception for {httpContext.Request.Path})");

        var problemDetails = exception switch
        {
            KeyNotFoundException keyNotFoundException =>
                new ProblemDetails
                {
                    Type = "https://httpstatuses.com/404",
                    Title = "Element not found",
                    Status = StatusCodes.Status404NotFound,
                    // Detail = $"The element", // TODO: Może damy informację, jakiego klucza nie znaleźliśmy?
                    Instance = httpContext.Request.Path,
                },
            BadHttpRequestException badHttpRequestException =>
                new ProblemDetails
                {
                    Title = "Bad HTTP Request",
                    Status = badHttpRequestException.StatusCode,
                    Detail = badHttpRequestException.Message,
                    Instance = httpContext.Request.Path
                },
            ValidationException validationException =>
                new ValidationProblemDetails
                {
                    Title = "Validation has failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        ["Errors"] = validationException.Errors
                            .GroupBy(x => x.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(x => x.ErrorMessage).ToArray()
                            )
                    }
                },
            HttpRequestException =>
                new ProblemDetails
                {
                    Title = "Http request error has occured",
                    Status = StatusCodes.Status502BadGateway,
                    Instance = httpContext.Request.Path
                },
            TaskCanceledException or OperationCanceledException =>
                new ProblemDetails
                {
                    Title = httpContext.RequestAborted.IsCancellationRequested
                        ? "Client closed request"
                        : "Upstream timeout",
                    Status = httpContext.RequestAborted.IsCancellationRequested
                        ? StatusCodes.Status499ClientClosedRequest
                        : StatusCodes.Status504GatewayTimeout,
                    Instance = httpContext.Request.Path,
                },
            TimeoutException =>
                new ProblemDetails
                {
                    Title = "Server timeout",
                    Status = StatusCodes.Status504GatewayTimeout,
                    Instance = httpContext.Request.Path,
                },
            FormatException =>
                new ProblemDetails
                {
                    Title = "Format is invalid",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more fields have incorrect format",
                    Instance = httpContext.Request.Path,
                },
            DomainEntityCreationException domainEntityCreationException =>
                new ProblemDetails()
                {
                    Title = "Entity creation has failed",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = domainEntityCreationException.Message,
                    Instance = httpContext.Request.Path
                },
            TooManyAdminsException tooManyAdminsException =>
                new ProblemDetails
                {
                    Title = "Too many admins",
                    Status = StatusCodes.Status409Conflict,
                    Detail = $"Cannot create admin account because the limit has been reached ({
                        tooManyAdminsException.AdminsLimit})",
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        ["AdminCreationTimestamp"] = tooManyAdminsException.AdminCreationTimestamp,
                    }
                },
            InvalidCaseStatusException invalidCaseStatusException =>
                new ProblemDetails
                {
                    Title = "Case status is invalid",
                    Status = StatusCodes.Status409Conflict,
                    Detail = $"Expected {invalidCaseStatusException.ExpectedStatus.ToString()} but got {
                        invalidCaseStatusException.Status.ToString()}",
                    Instance = httpContext.Request.Path,
                    Extensions =
                    {
                        ["Message"] = invalidCaseStatusException.MessageForClient,
                    }
                },
            CaseNotExistsException caseNotExistsException =>
                new ProblemDetails
                {
                    Title = "Case doesn't exist",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"Case with ID: {caseNotExistsException.CaseId} has not been found.",
                    Instance = httpContext.Request.Path,
                },
            InternalOutputValidationError =>
                new ProblemDetails
                {
                    Title = "An error occured",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = $"Please report the error to our team.",
                    Instance = httpContext.Request.Path,
                },
            _ =>
                new ProblemDetails
                {
                    Title = "An error occured",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "Please report the error to our team.",
                    Instance = httpContext.Request.Path,
                }
        };
        problemDetails.Type = $"https://httpstatuses.com/{problemDetails.Status}";

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        // Add traceId, set appropriate ContentType, etc.
        // In brief, make the response compliant with ProblemDetails specification.
        // We can also attach traceId manually
        await problemDetailsService.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}