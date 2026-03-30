using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace PM.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _appDbContext;

        public ChatService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Message> SendMessageAsync(Guid chatId, Guid senderId, string content)
        {
            var message = new Message
            {
                ChatId = chatId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _appDbContext.Messages.Add(message);
            await _appDbContext.SaveChangesAsync();

            return message;
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId)
        {
            return await _appDbContext.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}