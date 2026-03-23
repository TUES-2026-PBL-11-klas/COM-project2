using PM.Core.Exceptions;

namespace PM.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

                context.Response.StatusCode = ex switch
                {
                    UserAlreadyExistsException => 400,
                    InvalidCredentialsException => 401,
                    UserNotFoundException => 404,
                    _ => 500, // tva e za default
                };
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = ex.Message
                    }));
            }
        }
    }
}