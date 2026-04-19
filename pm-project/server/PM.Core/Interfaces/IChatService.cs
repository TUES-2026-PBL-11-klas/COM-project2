using PM.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PM.Core.Interfaces
{
    public interface IChatService
    {
        Task<Chat> CreateOrGetChatAsync(Guid senderId, string mentorId);
        Task<IReadOnlyCollection<Chat>> GetChatsForUserAsync(Guid userId);
        Task<Chat?> GetChatByIdAsync(Guid chatId);
        Task<MessageDMO> SendMessageAsync(Guid chatId, Guid senderId, string content);
        Task<IReadOnlyCollection<MessageDMO>> GetChatMessagesAsync(Guid chatId);
        Task<IReadOnlyCollection<Guid>> DeleteMentorChatsAsync(Guid mentorUserId, Guid? mentorProfileId = null);
    }
}
