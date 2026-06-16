namespace FGC.Users.API.Middlewares;

public interface ICorrelationContext
{
    string CorrelationId { get; set; }
}

public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlation)
    {
        if (context.Request.Headers.TryGetValue("x-correlation-id", out var cid))
        {
            correlation.CorrelationId = cid.ToString();
        }
        else
        {
            correlation.CorrelationId = Guid.NewGuid().ToString();
            context.Request.Headers["x-correlation-id"] = correlation.CorrelationId;
        }

        context.Response.Headers["x-correlation-id"] = correlation.CorrelationId;
        await _next(context);
    }
}
