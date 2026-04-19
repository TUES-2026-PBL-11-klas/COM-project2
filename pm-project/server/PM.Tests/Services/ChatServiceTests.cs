using Microsoft.EntityFrameworkCore;
using PM.Core.Services;
using PM.Data.Context;
using PM.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PM.Tests.Services
{
    public class ChatServiceTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            return new AppDbContext(options);
        }

        [Fact]
        public async Task SendMessageAsync_AddsMessageToDatabase()
        {
            // Arrange
            var dbContext = GetDbContext();
            var service = new ChatService(dbContext);

            var chatId = Guid.NewGuid();
            var senderId = Guid.NewGuid();

            // Act
            var result = await service.SendMessageAsync(chatId, senderId, "Hello World!");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chatId, result.ChatId);
            Assert.Equal(senderId, result.SenderId);
            Assert.Equal("Hello World!", result.Content);
            
            // Verify db context
            var count = await dbContext.Messages.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetChatMessagesAsync_ReturnsOrderedMessagesForChat()
        {
            // Arrange
            var dbContext = GetDbContext();
            var service = new ChatService(dbContext);

            var chatId = Guid.NewGuid();
            
            // Add a message in the past
            dbContext.Messages.Add(new Message { Id = Guid.NewGuid(), ChatId = chatId, SenderId = Guid.NewGuid(), Content = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
            // Add a message now
            dbContext.Messages.Add(new Message { Id = Guid.NewGuid(), ChatId = chatId, SenderId = Guid.NewGuid(), Content = "Second", CreatedAt = DateTime.UtcNow });
            
            // Add a message for another chat
            dbContext.Messages.Add(new Message { Id = Guid.NewGuid(), ChatId = Guid.NewGuid(), SenderId = Guid.NewGuid(), Content = "Other Chat", CreatedAt = DateTime.UtcNow });
            
            await dbContext.SaveChangesAsync();

            // Act
            var messages = await service.GetChatMessagesAsync(chatId);

            // Assert
            Assert.NotNull(messages);
            var msgList = messages.ToList();
            Assert.Equal(2, msgList.Count);
            Assert.Equal("First", msgList[0].Content);
            Assert.Equal("Second", msgList[1].Content);
        }
    }
}
