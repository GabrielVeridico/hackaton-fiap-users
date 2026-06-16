using System.Text.RegularExpressions;

namespace FGC.Users.API.Middlewares;

public partial class RequestResponseLoggingMiddleware
{
    private static readonly Regex SensitiveFieldRegex = BuildSensitiveFieldRegex();

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var req = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var safeReq = MaskSensitive(req);
        _logger.LogInformation("Incoming request {Method} {Path} CorrelationId={Cid} Body={Body}", context.Request.Method, context.Request.Path, context.Request.Headers["x-correlation-id"].ToString(), safeReq);

        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Position = 0;
        var resp = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        var safeResp = MaskSensitive(resp);
        _logger.LogInformation("Outgoing response {StatusCode} CorrelationId={Cid} Body={Body}", context.Response.StatusCode, context.Request.Headers["x-correlation-id"].ToString(), safeResp);
    }

    private static string MaskSensitive(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        return SensitiveFieldRegex.Replace(body, "\"$1\":\"***\"");
    }

    [GeneratedRegex("\"(password|senha|token|secret|accessToken|refreshToken)\"\\s*:\\s*\"[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex BuildSensitiveFieldRegex();
}
