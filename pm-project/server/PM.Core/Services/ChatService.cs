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

            // simulated mentor auto-reply: if the other participant is a mentor (has MentorProfile)
            try
            {
                var chat = _appDbContext.Chats.FirstOrDefault(c => c.Id == chatId);
                if (chat != null)
                {
                    // find mentor user id (a MentorProfile owned by user1 or user2)
                    var possibleMentorId = Guid.Empty;
                    var mp1 = _appDbContext.MentorProfiles.FirstOrDefault(m => m.UserId == chat.User1Id);
                    var mp2 = _appDbContext.MentorProfiles.FirstOrDefault(m => m.UserId == chat.User2Id);
                    if (mp1 != null && chat.User1Id != senderId) possibleMentorId = chat.User1Id;
                    else if (mp2 != null && chat.User2Id != senderId) possibleMentorId = chat.User2Id;

                    if (possibleMentorId != Guid.Empty)
                    {
                        var reply = new Message
                        {
                            ChatId = chatId,
                            SenderId = possibleMentorId,
                            Content = "Hi — thanks for reaching out! I can help you with that. Tell me more about what you need.",
                            CreatedAt = DateTime.UtcNow.AddSeconds(1)
                        };
                        _appDbContext.Messages.Add(reply);
                        await _appDbContext.SaveChangesAsync();
                    }
                }
            }
            catch { }

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