using PM.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PM.Core.Interfaces
{
    public interface IChatService
    {
        Task<MessageDMO> SendMessageAsync(Guid conversationId, Guid senderId, string content);
        Task<IEnumerable<MessageDMO>> GetChatMessagesAsync(Guid conversationId);
    }
}