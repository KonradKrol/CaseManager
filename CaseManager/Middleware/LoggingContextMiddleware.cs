using System.Security.Claims;

namespace CaseManager.Middleware;

using Microsoft.Extensions.Logging;

public class LoggingContextMiddleware(RequestDelegate next, ILogger<LoggingContextMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceIdentifier = context.TraceIdentifier;
        var correlationId = context.Response.Headers[CorrelationIdHeader].FirstOrDefault();

        if (correlationId is null)
        {
            logger.LogWarning("CorrelationId does not exist at context.Request.Headers[CorrelationIdHeader]. Assigning empty guid.");
            correlationId = Guid.Empty.ToString();
        }

        var userId = context.User.FindFirstValue("sub");

        var attachedContext = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        };
        if (userId is not null)
        {
            attachedContext.Add("UserId", userId);
        }
        else
        {
            logger.LogDebug("UserId not found in HTTP claims");
        }

        using (logger.BeginScope(attachedContext))
        {
            logger.LogDebug("Attaching additional context (CorrelationId) to logs");
            await next(context);
        }
    }
}