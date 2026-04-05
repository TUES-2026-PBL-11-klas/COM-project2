using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PM.API.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("[REQUEST] {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);

            _logger.LogInformation("[RESPONSE] {StatusCode}", context.Response.StatusCode);
        }
    }
}