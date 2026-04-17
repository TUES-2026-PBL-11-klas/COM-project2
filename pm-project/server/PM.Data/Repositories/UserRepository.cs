using System.Diagnostics;
using OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Observability;

namespace PM.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public void AddUser(UserDMO user)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "UserRepository.AddUser", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "INSERT");
            activity?.SetTag("user.id", user.Id.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Adding user {UserId} ({Username})", user.Id, user.Username);
                _context.Users.Add(user);
                DataMetrics.UserAdded.Add(1, new KeyValuePair<string, object?>("db.system", "postgresql"));
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("User {UserId} staged for insert", user.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to stage user {UserId} for insert", user.Id);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "UserRepository.AddUser"));
            }
        }

        public UserDMO? GetByUsername(string username)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "UserRepository.GetByUsername", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "SELECT");
            activity?.SetTag("user.username", username);

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Querying user by username {Username}", username);
                var user = _context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefault(u => u.Username == username);

                DataMetrics.UserQueried.Add(1, new KeyValuePair<string, object?>("db.system", "postgresql"));
                activity?.SetTag("db.result.found", user is not null);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Query for username {Username} returned {Result}",
                    username, user is not null ? "found" : "not found");
                return user;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to query user by username {Username}", username);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "UserRepository.GetByUsername"));
            }
        }

        public void SaveChanges()
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "UserRepository.SaveChanges", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "COMMIT");

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Saving user changes to PostgreSQL");
                _context.SaveChanges();
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogDebug("User changes committed");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to commit user changes");
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "UserRepository.SaveChanges"));
            }
        }
    }
}