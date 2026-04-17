using System;
using System.Linq;
using System.Threading.Tasks;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace PM.Tests
{
    public class ConversationRepositoryTests
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
        public async Task Add_And_GetConversation_Should_Work()
        {
            // Arrange
            using var context = GetDbContext();
            var conversationRepo = new ConversationRepository(context, NullLogger<ConversationRepository>.Instance);
            var userRepo = new UserRepository(context, NullLogger<UserRepository>.Instance);
            
            var user1 = new UserDMO { Id = Guid.NewGuid(), Username = "user1_" + Guid.NewGuid(), PasswordHash = "hash" };
            var user2 = new UserDMO { Id = Guid.NewGuid(), Username = "user2_" + Guid.NewGuid(), PasswordHash = "hash" };
            userRepo.AddUser(user1);
            userRepo.AddUser(user2);
            userRepo.SaveChanges();

            var conversation = new ConversationDMO
            {
                Id = Guid.NewGuid(),
                Name = "Test Group",
                CreatedAt = DateTime.UtcNow,
                Participants = new List<UserDMO> { user1, user2 }
            };

            // Act
            await conversationRepo.AddAsync(conversation);
            await conversationRepo.SaveChangesAsync();

            // clear tracker to simulate new request
            context.ChangeTracker.Clear();

            var retrievedConv = await conversationRepo.GetByIdAsync(conversation.Id);
            var user1Convs = await conversationRepo.GetConversationsForUserAsync(user1.Id);

            // Assert
            retrievedConv.Should().NotBeNull();
            retrievedConv!.Name.Should().Be("Test Group");
            retrievedConv.Participants.Should().HaveCount(2);

            user1Convs.Should().ContainSingle();
            user1Convs.First().Id.Should().Be(conversation.Id);
        }
    }
}
