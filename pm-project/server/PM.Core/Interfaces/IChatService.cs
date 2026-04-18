using PM.Data.Entities;

namespace PM.Core.Interfaces
{
    public interface IChatService
    {
        Task<Message> SendMessageAsync(Guid chatId, Guid senderId, string content);
        Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId);
    }
}