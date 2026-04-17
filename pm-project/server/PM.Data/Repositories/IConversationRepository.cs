using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public interface IConversationRepository
    {
        Task<ConversationDMO?> GetByIdAsync(Guid id);
        Task<IEnumerable<ConversationDMO>> GetConversationsForUserAsync(Guid userId);
        Task AddAsync(ConversationDMO conversation);
        Task SaveChangesAsync();
    }
}
