using Microsoft.EntityFrameworkCore;
using PM.Core.Services;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;

namespace PM.Tests
{
    public class ChatServiceTests
    {
        private sealed class InMemoryMessageRepository : IMessageRepository
        {
            private readonly List<MessageDMO> _messages = new();

            public Task<IEnumerable<MessageDMO>> GetMessagesForChatAsync(Guid chatId)
            {
                IEnumerable<MessageDMO> result = _messages.Where(m => m.ChatId == chatId).OrderBy(m => m.CreatedAt).ToList();
                return Task.FromResult(result);
            }

            public Task AddMessageAsync(MessageDMO message)
            {
                _messages.Add(message);
                return Task.CompletedTask;
            }
        }

        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateOrGetChatAsync_CreatesChat_AndIncrementsStudents()
        {
            using var ctx = CreateContext(Guid.NewGuid().ToString());
            var sender = new UserDMO { Username = "sender", Email = "sender@example.com", PasswordHash = "pw" };
            var mentor = new UserDMO { Username = "mentor.user", Email = "mentor@example.com", PasswordHash = "pw" };
            var profile = new MentorProfile { User = mentor, UserId = mentor.Id, Subjects = "Math", StudentsHelped = 0 };

            ctx.Users.AddRange(sender, mentor);
            ctx.MentorProfiles.Add(profile);
            ctx.SaveChanges();

            var service = new ChatService(ctx, new InMemoryMessageRepository());
            var chat = await service.CreateOrGetChatAsync(sender.Id, mentor.Id.ToString());

            Assert.Equal(sender.Id, chat.User1Id);
            Assert.Equal(mentor.Id, chat.User2Id);
            Assert.Equal(1, ctx.MentorProfiles.Single().StudentsHelped);
        }

        [Fact]
        public async Task SendMessageAsync_UpdatesChatMetadata()
        {
            using var ctx = CreateContext(Guid.NewGuid().ToString());
            var sender = new UserDMO { Username = "sender", Email = "sender@example.com", PasswordHash = "pw" };
            var mentor = new UserDMO { Username = "mentor", Email = "mentor@example.com", PasswordHash = "pw" };
            var chat = new Chat { User1Id = sender.Id, User2Id = mentor.Id, Name = "Mentor" };

            ctx.Users.AddRange(sender, mentor);
            ctx.Chats.Add(chat);
            ctx.SaveChanges();

            var repo = new InMemoryMessageRepository();
            var service = new ChatService(ctx, repo);

            var message = await service.SendMessageAsync(chat.Id, sender.Id, "hello");

            Assert.Equal(chat.Id, message.ChatId);
            Assert.Equal(sender.Id, message.SenderId);

            var storedChat = ctx.Chats.Single();
            Assert.Equal("hello", storedChat.LastMessageContent);
            Assert.Equal(sender.Id, storedChat.LastMessageSenderId);
            Assert.NotNull(storedChat.LastMessageAt);
        }

        [Fact]
        public async Task GetChatMessagesAsync_ReturnsOrdered()
        {
            using var ctx = CreateContext(Guid.NewGuid().ToString());
            var repo = new InMemoryMessageRepository();
            await repo.AddMessageAsync(new MessageDMO { ChatId = Guid.Parse("11111111-1111-1111-1111-111111111111"), SenderId = Guid.NewGuid(), Content = "b", CreatedAt = DateTime.UtcNow });
            await repo.AddMessageAsync(new MessageDMO { ChatId = Guid.Parse("11111111-1111-1111-1111-111111111111"), SenderId = Guid.NewGuid(), Content = "a", CreatedAt = DateTime.UtcNow.AddMinutes(-1) });

            var service = new ChatService(ctx, repo);
            var list = (await service.GetChatMessagesAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"))).ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal("a", list[0].Content);
            Assert.Equal("b", list[1].Content);
        }
    }
}
