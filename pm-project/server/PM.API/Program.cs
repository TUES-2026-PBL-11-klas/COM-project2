using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Data.Repositories;
using PM.Core.Services;
using PM.Core.Interfaces;
using PM.API.Middleware;
using PM.API.Extensions;
using PM.Data.Seed;
using PM.API.Hubs;

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using DotNetEnv;

Env.Load();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);
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
        policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService("pm-api")
    );

    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;

    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri("http://grafana-alloy:4318/v1/logs");
        otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("pm-api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://grafana-alloy:4318/v1/traces");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://grafana-alloy:4318/v1/metrics");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
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