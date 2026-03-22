using PM.Data.Entities;
using Microsoft.EntityFrameworkCore;
using PM.Data.Context;

namespace PM.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public void AddUser(UserDMO user)
        {
            _context.Users.Add(user);
        }

        public UserDMO? GetByUsername(string username)
        {
            return _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Username == username);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}