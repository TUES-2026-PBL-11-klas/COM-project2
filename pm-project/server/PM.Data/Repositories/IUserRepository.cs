using PM.Data.Entities;

namespace PM.Data.Repositories
{
    public interface IUserRepository
    {
        void AddUser(UserDMO user);
        UserDMO? GetByUsername(string username);
        void SaveChanges();
    }
}