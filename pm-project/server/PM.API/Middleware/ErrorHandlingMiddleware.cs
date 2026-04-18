using PM.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace PM.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                var statusCode = ex switch
                {
                    UserAlreadyExistsException => 400,
                    InvalidCredentialsException => 401,
                    UserNotFoundException => 404,
                    _ => 500,
                };

                    if (statusCode >= 500)
                    {
                        _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
                    }
                    else
                    {
                        _logger.LogWarning("Handled exception while processing {Method} {Path} -> {StatusCode}: {Error}", context.Request.Method, context.Request.Path, statusCode, ex.Message);
                    }

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = ex.Message
                    }));
            }
        }
    }
}