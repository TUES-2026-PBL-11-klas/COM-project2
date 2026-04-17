using System;
using System.Linq;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace PM.Tests
{
    public class UserRepositoryTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Server=localhost;Port=5432;Database=pmdb;User Id=postgres;Password=postgrespassword;")
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public void AddUser_And_GetByUsername_Should_Work()
        {
            // Arrange
            using var context = GetDbContext();
            var repo = new UserRepository(context, NullLogger<UserRepository>.Instance);
            var username = "testuser_" + Guid.NewGuid();
            var email = username + "@test.com";

            var user = new UserDMO
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = "hash"
            };

            // Act
            repo.AddUser(user);
            repo.SaveChanges();

            var retrievedUser = repo.GetByUsername(username);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Username.Should().Be(username);
        }
    }
}
