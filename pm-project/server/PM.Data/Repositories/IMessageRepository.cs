using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<MessageDMO>> GetMessagesForChatAsync(Guid chatId);
        Task AddMessageAsync(MessageDMO message);
    }
}
