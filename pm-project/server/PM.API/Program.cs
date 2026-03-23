using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Data.Repositories;
using PM.Core.Services;
using PM.Core.Interfaces;
using PM.API.Middleware;
using PM.API.Extensions;
using PM.Data.Seed;

using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TempDb"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
RoleSeeder.SeedRoles(dbContext);

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseMiddleware<LoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();