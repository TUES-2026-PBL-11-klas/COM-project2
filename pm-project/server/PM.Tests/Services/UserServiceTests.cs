using Microsoft.EntityFrameworkCore;
using Moq;
using PM.Core.DTOs;
using PM.Core.Exceptions;
using PM.Core.Services;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.AspNetCore.Identity;

namespace PM.Tests.Services
{
    public class UserServiceTests
    {
        private (AppDbContext, Mock<IUserRepository>, UserService) SetupService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            // Add default roles
            context.Roles.Add(new Role { Name = "Student" });
            context.Roles.Add(new Role { Name = "Admin" });
            context.SaveChanges();

            var mockRepo = new Mock<IUserRepository>();

            var service = new UserService(mockRepo.Object, context);

            return (context, mockRepo, service);
        }

        [Fact]
        public void Register_ThrowsException_IfUserExists()
        {
            var (_, mockRepo, service) = SetupService();

            mockRepo.Setup(x => x.GetByUsername("existingUser"))
                    .Returns(new UserDMO { Username = "existingUser" });

            var request = new RegisterRequestDto { Username = "existingUser", Password = "password", Email = "test@test.com" };

            Assert.Throws<UserAlreadyExistsException>(() => service.Register(request));
        }

        [Fact]
        public void Register_CreatesNewUser_WithDefaultRole_IfNoRolesProvided()
        {
            var (context, mockRepo, service) = SetupService();

            mockRepo.Setup(x => x.GetByUsername("newUser")).Returns((UserDMO)null);

            UserDMO savedUser = null;
            mockRepo.Setup(x => x.AddUser(It.IsAny<UserDMO>()))
                    .Callback<UserDMO>(u => savedUser = u);

            var request = new RegisterRequestDto { Username = "newUser", Password = "pwd", Email = "test@test.com" };

            var result = service.Register(request);

            Assert.NotNull(savedUser);
            Assert.Equal("newUser", savedUser.Username);
            Assert.Contains(savedUser.Roles, r => r.Name == "Student");
            Assert.Equal("newUser", result.Username);
            mockRepo.Verify(x => x.AddUser(It.IsAny<UserDMO>()), Times.Once);
            mockRepo.Verify(x => x.SaveChanges(), Times.Once);
            
            // Password shouldn't be plain text
            Assert.NotEqual("pwd", savedUser.PasswordHash);
        }

        [Fact]
        public void Login_ThrowsInvalidCredentials_WhenUserNotFound()
        {
            var (_, mockRepo, service) = SetupService();
            mockRepo.Setup(x => x.GetByUsername("nonexistent")).Returns((UserDMO)null);

            var request = new LoginRequestDto { Username = "nonexistent", Password = "pwd" };
            Assert.Throws<InvalidCredentialsException>(() => service.Login(request));
        }

        [Fact]
        public void Login_ReturnsResponse_WithRoles_WhenCredentialsAreValid()
        {
            var (_, mockRepo, service) = SetupService();
            
            var passwordHasher = new PasswordHasher<UserDMO>();
            var dummyUser = new UserDMO
            {
                Username = "validUser",
                Email = "valid@test.com",
                Roles = new List<Role> { new Role { Name = "Student" } }
            };
            dummyUser.PasswordHash = passwordHasher.HashPassword(dummyUser, "correctPwd");

            mockRepo.Setup(x => x.GetByUsername("validUser")).Returns(dummyUser);

            var request = new LoginRequestDto { Username = "validUser", Password = "correctPwd" };
            var response = service.Login(request);

            Assert.Equal("validUser", response.Username);
            Assert.Contains("Student", response.Roles);
        }

        [Fact]
        public void UpdateUserRole_ThrowsException_IfUserNotFound()
        {
            var (_, mockRepo, service) = SetupService();
            mockRepo.Setup(x => x.GetByUsername("nonexistent")).Returns((UserDMO)null);

            Assert.Throws<UserNotFoundException>(() => service.UpdateUserRole("nonexistent", new List<string> { "Admin" }));
        }

        [Fact]
        public void UpdateUserRole_UpdatesRolesAndSaves_WhenValid()
        {
            var (context, mockRepo, service) = SetupService();
            
            var dummyUser = new UserDMO
            {
                Username = "target",
                Roles = new List<Role> { context.Roles.First(r => r.Name == "Student") }
            };
            mockRepo.Setup(x => x.GetByUsername("target")).Returns(dummyUser);

            service.UpdateUserRole("target", new List<string> { "Admin" });

            Assert.Contains(dummyUser.Roles, r => r.Name == "Admin");
            Assert.DoesNotContain(dummyUser.Roles, r => r.Name == "Student"); // Should be cleared
            mockRepo.Verify(x => x.SaveChanges(), Times.Once);
        }
    }
}
