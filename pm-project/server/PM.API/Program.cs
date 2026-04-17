using Microsoft.EntityFrameworkCore;
using PM.Data.Observability;
using PM.Data.Context;
using PM.Data.Repositories;
using PM.Core.Services;
using PM.Core.Interfaces;
using PM.API.Middleware;
using PM.API.Extensions;
using PM.Data.Seed;
using PM.API.Hubs;
using Cassandra;

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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

    // Postgres EF Core Setup
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Cassandra Setup
    var cassandraHost = builder.Configuration.GetSection("Cassandra:ContactPoints").Get<string[]>()?.FirstOrDefault() ?? "127.0.0.1";
    var cluster = Cluster.Builder().AddContactPoint(cassandraHost).Build();
    
    // Auto-create Keyspace and Table if missing
    var tempSession = cluster.Connect();
    tempSession.Execute("CREATE KEYSPACE IF NOT EXISTS pm_keyspace WITH replication = {'class':'SimpleStrategy', 'replication_factor':1};");
    tempSession.Dispose();

    var session = cluster.Connect("pm_keyspace");
    session.Execute(@"
        CREATE TABLE IF NOT EXISTS messages (
            Id uuid PRIMARY KEY,
            ConversationId uuid,
            UserId uuid,
            Content text,
            Attachments list<text>,
            CreatedAt timestamp
        );
    ");
    session.Execute("CREATE INDEX IF NOT EXISTS idx_conversation_id ON messages (ConversationId);");

    Cassandra.Mapping.MappingConfiguration.Global.Define(
        new Cassandra.Mapping.Map<PM.Data.Entities.MessageDMO>()
            .TableName("messages")
            .PartitionKey(u => u.Id)
    );

    builder.Services.AddSingleton<Cassandra.ISession>(session);

    // Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
    builder.Services.AddSingleton<IMessageRepository, MessageRepository>();

    // Services
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

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("pm-api"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(DataActivitySource.SourceName)   // PM.Data repository spans
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
                .AddMeter(DataMetrics.MeterName)            // PM.Data counters + histogram
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
    dbContext.Database.EnsureCreated();
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