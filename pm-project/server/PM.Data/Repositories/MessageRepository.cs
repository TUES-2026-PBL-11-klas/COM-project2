using PM.Data.Context;
using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public IEnumerable<Message> GetMessagesForChat(Guid chatId)
        {
            return _context.Messages.Where(m => m.Chat.Id == chatId);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}