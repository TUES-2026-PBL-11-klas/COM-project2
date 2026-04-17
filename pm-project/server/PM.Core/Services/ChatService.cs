using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PM.Core.Interfaces;
using PM.Data.Repositories;
using PM.Data.Entities;

namespace PM.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;

        public ChatService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<MessageDMO> SendMessageAsync(Guid conversationId, Guid senderId, string content)
        {
            var message = new MessageDMO
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<string>()
            };

            await _messageRepository.AddMessageAsync(message);

            return message;
        }

        public async Task<IEnumerable<MessageDMO>> GetChatMessagesAsync(Guid conversationId)
        {
            return await _messageRepository.GetMessagesForConversationAsync(conversationId);
        }
    }
}