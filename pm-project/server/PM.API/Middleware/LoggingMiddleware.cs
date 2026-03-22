using Microsoft.AspNetCore.Http;

namespace PM.API.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");

            await _next(context);

            Console.WriteLine($"[RESPONSE] {context.Response.StatusCode}");
        }
    }
}