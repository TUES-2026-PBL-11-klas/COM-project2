using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Core.Services;
using PM.Data.Entities;
using Xunit;

namespace PM.Tests
{
    public class ChatServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task SendMessageAsync_SavesMessage()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            var svc = new ChatService(ctx);

            var chatId = Guid.NewGuid();
            var senderId = Guid.NewGuid();

            var msg = await svc.SendMessageAsync(chatId, senderId, "hello");

            Assert.Equal("hello", msg.Content);
            Assert.Equal(chatId, msg.ChatId);

            var saved = ctx.Messages.FirstOrDefault(m => m.Id == msg.Id);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task GetChatMessagesAsync_ReturnsOrdered()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            var chatId = Guid.NewGuid();

            ctx.Messages.Add(new Message { ChatId = chatId, Content = "a", CreatedAt = DateTime.UtcNow.AddMinutes(-1) });
            ctx.Messages.Add(new Message { ChatId = chatId, Content = "b", CreatedAt = DateTime.UtcNow });
            ctx.SaveChanges();

            var svc = new ChatService(ctx);
            var list = (await svc.GetChatMessagesAsync(chatId)).ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal("a", list[0].Content);
            Assert.Equal("b", list[1].Content);
        }
    }
}
