using PM.Data.Entities;

namespace PM.Data.Repositories
{
   public interface IChatRepository
    {
        void AddChat(Chat chat);
        Chat? GetChatBetweenUsers(Guid userId1, Guid userId2);
        void SaveChanges();
    }
}