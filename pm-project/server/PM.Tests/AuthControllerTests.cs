using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PM.API.Controllers;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using PM.Data.Context;

namespace PM.Tests
{
    public class AuthControllerTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void Register_ReturnsOk_WithToken()
        {
            using var context = CreateContext(Guid.NewGuid().ToString());
            var userSvc = new Mock<IUserService>();
            var tokenSvc = new Mock<ITokenService>();

            var returned = new RegisterResponseDto
            {
                Id = Guid.NewGuid(),
                Username = "u",
                Email = "e@e.com",
                Roles = new List<string> { "Student" }
            };

            userSvc.Setup(u => u.Register(It.IsAny<RegisterRequestDto>())).Returns(returned);
            tokenSvc.Setup(t => t.GenerateToken("u", It.IsAny<IEnumerable<string>>(), It.IsAny<int>())).Returns("tok");

            var ctrl = new AuthController(userSvc.Object, tokenSvc.Object, NullLogger<AuthController>.Instance, context);

            var req = new RegisterRequestDto { Username = "u", Email = "e@e.com", Password = "p" };
            var res = ctrl.Register(req) as OkObjectResult;

            Assert.NotNull(res);
            var value = Assert.IsType<RegisterResponseDto>(res.Value);
            Assert.Equal("u", value.Username);
            Assert.Equal("tok", value.Token);
        }

        [Fact]
        public void Login_ReturnsOk_WithToken()
        {
            using var context = CreateContext(Guid.NewGuid().ToString());
            var userSvc = new Mock<IUserService>();
            var tokenSvc = new Mock<ITokenService>();

            var loginResp = new LoginResponseDto
            {
                Id = Guid.NewGuid(),
                Username = "u",
                Roles = new List<string> { "Student" }
            };

            userSvc.Setup(u => u.Login(It.IsAny<LoginRequestDto>())).Returns(loginResp);
            tokenSvc.Setup(t => t.GenerateToken("u", It.IsAny<IEnumerable<string>>(), It.IsAny<int>())).Returns("tok");

            var ctrl = new AuthController(userSvc.Object, tokenSvc.Object, NullLogger<AuthController>.Instance, context);

            var req = new LoginRequestDto { Username = "u", Password = "p" };
            var res = ctrl.Login(req) as OkObjectResult;

            Assert.NotNull(res);
            var value = Assert.IsType<LoginResponseDto>(res.Value);
            Assert.Equal("u", value.Username);
            Assert.Equal("tok", value.Token);
        }
    }
}
