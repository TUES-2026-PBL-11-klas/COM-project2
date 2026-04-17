using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Observability;

namespace PM.Data.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConversationRepository> _logger;

        public ConversationRepository(AppDbContext context, ILogger<ConversationRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task<ConversationDMO?> GetByIdAsync(Guid id)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "ConversationRepository.GetByIdAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "SELECT");
            activity?.SetTag("conversation.id", id.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Fetching conversation {ConversationId}", id);
                var conv = await _context.Conversations
                    .Include(c => c.Participants)
                    .FirstOrDefaultAsync(c => c.Id == id);

                DataMetrics.ConversationQueried.Add(1,
                    new KeyValuePair<string, object?>("db.system", "postgresql"),
                    new KeyValuePair<string, object?>("query.type", "by_id"));
                activity?.SetTag("db.result.found", conv is not null);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Conversation {ConversationId} {Result}",
                    id, conv is not null ? "found" : "not found");
                return conv;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to fetch conversation {ConversationId}", id);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "ConversationRepository.GetByIdAsync"));
            }
        }

        public async Task<IEnumerable<ConversationDMO>> GetConversationsForUserAsync(Guid userId)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "ConversationRepository.GetConversationsForUserAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "SELECT");
            activity?.SetTag("user.id", userId.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Fetching conversations for user {UserId}", userId);
                var convs = await _context.Conversations
                    .Include(c => c.Participants)
                    .Where(c => c.Participants.Any(p => p.Id == userId))
                    .ToListAsync();

                DataMetrics.ConversationQueried.Add(1,
                    new KeyValuePair<string, object?>("db.system", "postgresql"),
                    new KeyValuePair<string, object?>("query.type", "by_user"));
                activity?.SetTag("db.result.count", convs.Count);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Found {Count} conversations for user {UserId}", convs.Count, userId);
                return convs;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to fetch conversations for user {UserId}", userId);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "ConversationRepository.GetConversationsForUserAsync"));
            }
        }

        public async Task AddAsync(ConversationDMO conversation)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "ConversationRepository.AddAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "INSERT");
            activity?.SetTag("conversation.id", conversation.Id.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Staging conversation {ConversationId} for insert", conversation.Id);
                await _context.Conversations.AddAsync(conversation);
                DataMetrics.ConversationAdded.Add(1,
                    new KeyValuePair<string, object?>("db.system", "postgresql"));
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Conversation {ConversationId} staged for insert", conversation.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to stage conversation {ConversationId}", conversation.Id);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "ConversationRepository.AddAsync"));
            }
        }

        public async Task SaveChangesAsync()
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "ConversationRepository.SaveChangesAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("db.operation", "COMMIT");

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogDebug("Saving conversation changes to PostgreSQL");
                await _context.SaveChangesAsync();
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogDebug("Conversation changes committed");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to commit conversation changes");
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "ConversationRepository.SaveChangesAsync"));
            }
        }
    }
}
