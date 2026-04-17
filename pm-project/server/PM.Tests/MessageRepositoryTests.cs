using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using PM.Data.Entities;
using PM.Data.Repositories;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace PM.Tests
{
    public class MessageRepositoryTests : IDisposable
    {
        private ISession _session;
        private Cluster _cluster;

        public MessageRepositoryTests()
        {
            _cluster = Cluster.Builder().AddContactPoint("localhost").Build();
            var tempSession = _cluster.Connect();
            tempSession.Execute("CREATE KEYSPACE IF NOT EXISTS pm_keyspace WITH replication = {'class':'SimpleStrategy', 'replication_factor':1};");
            tempSession.Dispose();

            _session = _cluster.Connect("pm_keyspace");
            _session.Execute(@"
                CREATE TABLE IF NOT EXISTS messages (
                    Id uuid PRIMARY KEY,
                    ConversationId uuid,
                    UserId uuid,
                    Content text,
                    Attachments list<text>,
                    CreatedAt timestamp
                );
            ");
            _session.Execute("CREATE INDEX IF NOT EXISTS idx_conversation_id ON messages (ConversationId);");

            // Define mapping
            Cassandra.Mapping.MappingConfiguration.Global.Define(
                new Cassandra.Mapping.Map<MessageDMO>()
                    .TableName("messages")
                    .PartitionKey(u => u.Id)
            );
        }

        [Fact]
         public async Task Add_And_GetMessages_Should_Work()
         {
             // Arrange
             var repo = new MessageRepository(_session, NullLogger<MessageRepository>.Instance);
             
             var convId = Guid.NewGuid();
             var msg1 = new MessageDMO
             {
                 Id = Guid.NewGuid(),
                 ConversationId = convId,
                 UserId = Guid.NewGuid(),
                 Content = "Hello",
                 CreatedAt = DateTime.UtcNow
             };
             
             var msg2 = new MessageDMO
             {
                 Id = Guid.NewGuid(),
                 ConversationId = convId,
                 UserId = Guid.NewGuid(),
                 Content = "World",
                 CreatedAt = DateTime.UtcNow.AddSeconds(1)
             };

             // Act
             await repo.AddMessageAsync(msg1);
             await repo.AddMessageAsync(msg2);

             var messages = await repo.GetMessagesForConversationAsync(convId);

             // Assert
             messages.Should().NotBeNull();
             var messagesList = messages.ToList();
             messagesList.Should().HaveCount(2);
             messagesList.Should().Contain(m => m.Content == "Hello");
             messagesList.Should().Contain(m => m.Content == "World");
         }

        public void Dispose()
        {
            _session?.Dispose();
            _cluster?.Dispose();
        }
    }
}
