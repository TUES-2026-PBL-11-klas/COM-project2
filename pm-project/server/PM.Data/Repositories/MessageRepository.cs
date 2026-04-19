using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PM.Data.Entities;
using PM.Data.Observability;

namespace PM.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IMapper _mapper;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(Cassandra.ISession session, ILogger<MessageRepository>? logger = null)
        {
            _mapper = new Mapper(session);
            _logger = logger ?? NullLogger<MessageRepository>.Instance;

        }

        public async Task<IEnumerable<MessageDMO>> GetMessagesForChatAsync(Guid chatId)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "MessageRepository.GetMessagesForChatAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "cassandra");
            activity?.SetTag("db.operation", "SELECT");
            activity?.SetTag("chat.id", chatId.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Fetching messages for chat {ChatId}", chatId);
                var messages = await _mapper.FetchAsync<MessageDMO>(
                    "WHERE chatid = ?", chatId);

                var list = new List<MessageDMO>(messages);
                DataMetrics.MessageQueried.Add(1,
                    new KeyValuePair<string, object?>("db.system", "cassandra"));
                activity?.SetTag("db.result.count", list.Count);
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Fetched {Count} messages for chat {ChatId}",
                    list.Count, chatId);
                return list;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);
                _logger.LogError(ex, "Failed to fetch messages for chat {ChatId}", chatId);
                throw;
            }
            finally
            {
                sw.Stop();
                DataMetrics.OperationDuration.Record(
                    sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "MessageRepository.GetMessagesForChatAsync"));
            }
        }

        public async Task AddMessageAsync(MessageDMO message)
        {
            using var activity = DataActivitySource.Source.StartActivity(
                "MessageRepository.AddMessageAsync", ActivityKind.Client);
            activity?.SetTag("db.system", "cassandra");
            activity?.SetTag("db.operation", "INSERT");
            activity?.SetTag("message.id", message.Id.ToString());
            activity?.SetTag("chat.id", message.ChatId.ToString());

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Inserting message {MessageId} into chat {ChatId}",
                    message.Id, message.ChatId);
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
