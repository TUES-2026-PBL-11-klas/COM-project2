using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public interface IMessageRepository
    {
        void AddMessage(Message message);
        IEnumerable<Message> GetMessagesForChat(Guid chatId);
        void SaveChanges();
    }
}