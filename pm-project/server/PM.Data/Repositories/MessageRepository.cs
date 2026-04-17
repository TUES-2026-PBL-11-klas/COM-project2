using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using PM.Data.Entities;
using PM.Data.Observability;

namespace PM.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(Cassandra.ISession session, ILogger<MessageRepository> logger)
        {
            _mapper = new Mapper(session);
            _logger = logger;

            // Note: MappingConfiguration is registered globally at startup (Program.cs).
        }

        public async Task<IEnumerable<MessageDMO>> GetMessagesForConversationAsync(Guid conversationId)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "MessageRepository.GetMessagesForConversationAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "cassandra");
            activity?.SetTag("db.operation", "SELECT");
            activity?.SetTag("conversation.id", conversationId.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Fetching messages for conversation {ConversationId}", conversationId);
                var messages = await _mapper.FetchAsync<MessageDMO>(
                    "WHERE conversationid = ?", conversationId);

                var list = new List<MessageDMO>(messages);
                DataMetrics.MessageQueried.Add(1,
                    new KeyValuePair<string, object?>("db.system", "cassandra"));
                activity?.SetTag("db.result.count", list.Count);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Fetched {Count} messages for conversation {ConversationId}",
                    list.Count, conversationId);
                return list;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to fetch messages for conversation {ConversationId}", conversationId);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "MessageRepository.GetMessagesForConversationAsync"));
            }
        }

        public async Task AddMessageAsync(MessageDMO message)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "MessageRepository.AddMessageAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "cassandra");
            activity?.SetTag("db.operation", "INSERT");
            activity?.SetTag("message.id", message.Id.ToString());
            activity?.SetTag("conversation.id", message.ConversationId.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Inserting message {MessageId} into conversation {ConversationId}",
                    message.Id, message.ConversationId);
                await _mapper.InsertAsync(message);
                DataMetrics.MessageAdded.Add(1,
                    new KeyValuePair<string, object?>("db.system", "cassandra"));
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Message {MessageId} inserted successfully", message.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to insert message {MessageId}", message.Id);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "MessageRepository.AddMessageAsync"));
            }
        }
    }
}