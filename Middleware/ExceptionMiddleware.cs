using System.Net;
using System.Text.Json;

namespace InventoryAPI.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gabim: {Message}", ex.Message);
            context.Response.ContentType = "application/json";
            var (code, msg) = ex switch
            {
                InvalidOperationException => (400, ex.Message),
                KeyNotFoundException => (404, ex.Message),
                UnauthorizedAccessException => (401, "Nuk keni leje."),
                _ => (500, ex.Message + " | " + ex.InnerException?.Message)
            };
            context.Response.StatusCode = code;
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new { statusCode = code, message = msg, timestamp = DateTime.UtcNow },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
