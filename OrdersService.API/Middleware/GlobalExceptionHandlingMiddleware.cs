using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OrdersService.API.Middleware;

// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.GetType().ToString()}: {ex.Message}");

            if (ex.InnerException != null)
            {
                _logger.LogError($"Inner Exception: {ex.InnerException.GetType().ToString()}: {ex.InnerException.Message}");
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Message = ex.Message,
                Type = ex.GetType().ToString(),
            });
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
