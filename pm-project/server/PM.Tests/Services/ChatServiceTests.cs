using Microsoft.EntityFrameworkCore;
using Moq;
using PM.Core.Services;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;
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
        public async Task SendMessageAsync_CallsRepositoryAndReturnsMessage()
        {
            var dbContext = GetDbContext();
            var messageRepo = new Mock<IMessageRepository>();
            messageRepo.Setup(m => m.AddMessageAsync(It.IsAny<MessageDMO>())).Returns(Task.CompletedTask);
            var service = new ChatService(dbContext, messageRepo.Object);

            var chatId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            dbContext.Chats.Add(new PM.Data.Entities.Chat { Id = chatId, Name = "test", User1Id = Guid.Empty, User2Id = Guid.Empty });
            await dbContext.SaveChangesAsync();

            var result = await service.SendMessageAsync(chatId, senderId, "Hello World!");

            Assert.NotNull(result);
            Assert.Equal(chatId, result.ChatId);
            Assert.Equal(senderId, result.SenderId);
            Assert.Equal("Hello World!", result.Content);

            messageRepo.Verify(m => m.AddMessageAsync(It.IsAny<MessageDMO>()), Times.Once);
        }

        [Fact]
        public async Task GetChatMessagesAsync_ReturnsOrderedMessagesForChat()
        {
            var dbContext = GetDbContext();
            var messageRepo = new Mock<IMessageRepository>();
            var chatId = Guid.NewGuid();

            var msgs = new[] {
                new MessageDMO { Id = Guid.NewGuid(), ChatId = chatId, SenderId = Guid.NewGuid(), Content = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new MessageDMO { Id = Guid.NewGuid(), ChatId = chatId, SenderId = Guid.NewGuid(), Content = "Second", CreatedAt = DateTime.UtcNow }
            };

            messageRepo.Setup(m => m.GetMessagesForChatAsync(chatId)).ReturnsAsync(msgs);
            var service = new ChatService(dbContext, messageRepo.Object);

            var messages = await service.GetChatMessagesAsync(chatId);

            Assert.NotNull(messages);
            var msgList = messages.ToList();
            Assert.Equal(2, msgList.Count);
            Assert.Equal("First", msgList[0].Content);
            Assert.Equal("Second", msgList[1].Content);
        }
    }
}
