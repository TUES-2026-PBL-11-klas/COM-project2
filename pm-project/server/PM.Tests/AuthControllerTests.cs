using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PM.API.Controllers;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using Xunit;

namespace PM.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public void Register_ReturnsOk_WithToken()
        {
            var userSvc = new Mock<IUserService>();
            var tokenSvc = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();

            var returned = new RegisterResponseDto { Username = "u", Email = "e@e.com", Roles = new List<string> { "Student" } };
            userSvc.Setup(u => u.Register(It.IsAny<RegisterRequestDto>())).Returns(returned);
            tokenSvc.Setup(t => t.GenerateToken("u", It.IsAny<IEnumerable<string>>(), It.IsAny<int>())).Returns("tok");

            var ctrl = new AuthController(userSvc.Object, tokenSvc.Object, logger.Object);

            var req = new RegisterRequestDto { Username = "u", Email = "e@e.com", Password = "p" };
            var res = ctrl.Register(req) as OkObjectResult;

            Assert.NotNull(res);
            var value = res.Value as RegisterResponseDto;
            Assert.NotNull(value);
            Assert.Equal("u", value.Username);
            Assert.Equal("tok", value.Token);
        }

        [Fact]
        public void Login_ReturnsOk_WithToken()
        {
            var userSvc = new Mock<IUserService>();
            var tokenSvc = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();

            var loginResp = new LoginResponseDto { Username = "u", Roles = new List<string> { "Student" } };
            userSvc.Setup(u => u.Login(It.IsAny<LoginRequestDto>())).Returns(loginResp);
            tokenSvc.Setup(t => t.GenerateToken("u", It.IsAny<IEnumerable<string>>(), It.IsAny<int>())).Returns("tok");

            var ctrl = new AuthController(userSvc.Object, tokenSvc.Object, logger.Object);

            var req = new LoginRequestDto { Username = "u", Password = "p" };
            var res = ctrl.Login(req) as OkObjectResult;

            Assert.NotNull(res);
            var value = res.Value as LoginResponseDto;
            Assert.NotNull(value);
            Assert.Equal("u", value.Username);
        }
    }
}
