namespace CaseManager.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // TraceIdentifier is internal ASP.NET Core request ID.

        var traceIdentifier = context.TraceIdentifier;
        var correlationIdFromHeader = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        
        if (!Guid.TryParse(correlationIdFromHeader, out var parsed))
        {
            parsed = Guid.NewGuid();
        }

        var correlationId = parsed.ToString();
        
        logger.LogDebug("Generated correlationId {CorrelationId} for a request with traceIdentifier {TraceIdentifier}", correlationId, traceIdentifier);
        
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        await next(context);
    }
}