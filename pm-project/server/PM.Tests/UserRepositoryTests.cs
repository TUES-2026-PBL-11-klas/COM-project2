using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;

namespace PM.Tests
{
    public class UserRepositoryTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void AddUser_And_GetByUsername_Should_Work()
        {
            using var context = CreateContext(Guid.NewGuid().ToString());
            var repo = new UserRepository(context);
            var username = "testuser_" + Guid.NewGuid();

            var user = new UserDMO
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = "hash"
            };

            repo.AddUser(user);
            repo.SaveChanges();

            var retrievedUser = repo.GetByUsername(username);

            Assert.NotNull(retrievedUser);
            Assert.Equal(username, retrievedUser!.Username);
            Assert.Equal(user.Email, retrievedUser.Email);
        }
    }
}
