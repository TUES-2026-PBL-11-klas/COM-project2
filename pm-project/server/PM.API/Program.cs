using Cassandra;
using Cassandra.Mapping;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PM.API.Extensions;
using PM.API.Hubs;
using PM.API.Middleware;
using PM.Core.Interfaces;
using PM.Core.Services;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Observability;
using PM.Data.Repositories;
using PM.Data.Seed;

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "../../infra/.env"));

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConfiguration(configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var cassandraHost = builder.Configuration.GetSection("Cassandra:ContactPoints").Get<string[]>()?.FirstOrDefault() ?? "127.0.0.1";
var cassandraKeyspace = builder.Configuration["Cassandra:Keyspace"] ?? "pm_keyspace";

var cluster = Cluster.Builder()
    .AddContactPoint(cassandraHost)
    .Build();

Cassandra.ISession? session = null;
int retries = 0;
while (retries < 15)
{
    try
    {
        Console.WriteLine($"[INIT] Connecting to Cassandra (Attempt {retries + 1})...");
        var keyspaceSession = cluster.Connect();
        keyspaceSession.Execute($"CREATE KEYSPACE IF NOT EXISTS {cassandraKeyspace} WITH replication = {{'class':'SimpleStrategy', 'replication_factor':1}};");
        keyspaceSession.Dispose();

        session = cluster.Connect(cassandraKeyspace);
        session.Execute(@"
            CREATE TABLE IF NOT EXISTS messages (
                Id uuid PRIMARY KEY,
                ChatId uuid,
                SenderId uuid,
                Content text,
                Attachments list<text>,
                CreatedAt timestamp
            );
        ");
        session.Execute("CREATE INDEX IF NOT EXISTS idx_chat_id ON messages (ChatId);");
        Console.WriteLine("[INIT] Cassandra connected and schema verified.");
        break;
    }
    catch (Exception ex)
    {
        retries++;
        Console.WriteLine($"[INIT] Cassandra not ready: {ex.Message}. Retrying in 5s...");
        Thread.Sleep(5000);
    }
}

if (session == null) throw new Exception("Could not connect to Cassandra after multiple attempts.");

MappingConfiguration.Global.Define(
    new Map<MessageDMO>()
        .TableName("messages")
        .PartitionKey(m => m.Id));

builder.Services.AddSingleton(cluster);
builder.Services.AddSingleton<Cassandra.ISession>(session);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

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

builder.Logging.AddConsole();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("pm-api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(DataActivitySource.SourceName)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://grafana-alloy:4317");
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(DataMetrics.MeterName)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://grafana-alloy:4317");
            });
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    RoleSeeder.SeedRoles(dbContext);
    MentorSeeder.SeedTestMentors(dbContext);
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chat");

app.Run();
