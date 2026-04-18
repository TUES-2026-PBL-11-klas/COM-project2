using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PM.Data.Context;
using PM.Data.Repositories;
using PM.Core.Services;
using PM.Core.DTOs;
using PM.Data.Entities;
using Xunit;

namespace PM.Tests
{
    public class UserServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void Register_Throws_WhenUserExists()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            var repo = new UserRepository(ctx);

            var existing = new UserDMO { Username = "joe", Email = "e@e.com", PasswordHash = "x" };
            repo.AddUser(existing);
            repo.SaveChanges();

            var svc = new UserService(repo, ctx);

            var req = new RegisterRequestDto { Username = "joe", Email = "e@e.com", Password = "pw" };

            Assert.Throws<PM.Core.Exceptions.UserAlreadyExistsException>(() => svc.Register(req));
        }

        [Fact]
        public void Register_AddsUser_WithSpecifiedRole()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            ctx.Roles.Add(new Role { Name = "Student" });
            ctx.SaveChanges();

            var repo = new UserRepository(ctx);
            var svc = new UserService(repo, ctx);

            var req = new RegisterRequestDto { Username = "alice", Email = "a@a.com", Password = "pw", Roles = new List<string> { "Student" } };

            var res = svc.Register(req);

            Assert.Equal("alice", res.Username);
            Assert.Contains("Student", res.Roles);
            Assert.NotNull(repo.GetByUsername("alice"));
        }

        [Fact]
        public void Login_Succeeds_WithValidCredentials()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            var repo = new UserRepository(ctx);

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserDMO>();
            var user = new UserDMO { Username = "bob", Email = "b@b.com" };
            user.PasswordHash = hasher.HashPassword(user, "secret");

            repo.AddUser(user);
            repo.SaveChanges();

            var svc = new UserService(repo, ctx);

            var login = svc.Login(new LoginRequestDto { Username = "bob", Password = "secret" });

            Assert.Equal("bob", login.Username);
        }

        [Fact]
        public void UpdateUserRole_ReplacesRoles()
        {
            var ctx = CreateContext(Guid.NewGuid().ToString());
            ctx.Roles.Add(new Role { Name = "Admin" });
            ctx.Roles.Add(new Role { Name = "Student" });
            ctx.SaveChanges();

            var repo = new UserRepository(ctx);
            var user = new UserDMO { Username = "carol", Email = "c@c.com", PasswordHash = "x" };
            user.Roles.Add(ctx.Roles.First(r => r.Name == "Student"));
            repo.AddUser(user);
            repo.SaveChanges();

            var svc = new UserService(repo, ctx);

            svc.UpdateUserRole("carol", new List<string> { "Admin" });

            var updated = repo.GetByUsername("carol");
            Assert.NotNull(updated);
            Assert.Contains("Admin", updated.Roles.Select(r => r.Name));
            Assert.DoesNotContain("Student", updated.Roles.Select(r => r.Name));
        }
    }
}
