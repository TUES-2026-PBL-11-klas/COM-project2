using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<MessageDMO>> GetMessagesForConversationAsync(Guid conversationId);
        Task AddMessageAsync(MessageDMO message);
    }
}