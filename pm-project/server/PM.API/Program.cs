using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Data.Repositories;
using PM.Core.Services;
using PM.Core.Interfaces;
using PM.API.Middleware;
using PM.API.Extensions;
using PM.Data.Seed;
using PM.API.Hubs;

using DotNetEnv;
using Serilog;

Env.Load();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Configuration.AddConfiguration(configuration);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TempDb"));

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddSingleton<ITokenService, TokenService>();
    builder.Services.AddScoped<IChatService, ChatService>();

    builder.Services.AddJwtAuth(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    RoleSeeder.SeedRoles(dbContext);

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseMiddleware<LoggingMiddleware>();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<ChatHub>("/chat");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}