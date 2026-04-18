using PM.Data.Context;
using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public void AddChat(Chat chat)
        {
            _context.Chats.Add(chat);
        }

        public Chat? GetChatBetweenUsers(Guid userId1, Guid userId2)
        {
            return _context.Chats.FirstOrDefault(c =>
                (c.User1.Id == userId1 && c.User2.Id == userId2) ||
                (c.User1.Id == userId2 && c.User2.Id == userId1));
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}