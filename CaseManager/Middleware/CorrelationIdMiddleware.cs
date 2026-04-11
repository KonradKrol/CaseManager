namespace CaseManager.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // TraceIdentifier is like Guid.NewGuid for us
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? context.TraceIdentifier;
        context.TraceIdentifier = correlationId;

        context.Request.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        await next(context);
    }
}