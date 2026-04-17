using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PM.API.Controllers;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using System.Collections.Generic;
using Xunit;

namespace PM.Tests.API
{
    public class AuthControllerTests
    {
        private (Mock<IUserService>, Mock<ITokenService>, Mock<ILogger<AuthController>>, AuthController) Setup()
        {
            var mockUserService = new Mock<IUserService>();
            var mockTokenService = new Mock<ITokenService>();
            var mockLogger = new Mock<ILogger<AuthController>>();

            var controller = new AuthController(mockUserService.Object, mockTokenService.Object, mockLogger.Object);
            return (mockUserService, mockTokenService, mockLogger, controller);
        }

        [Fact]
        public void Register_ReturnsOkResult_WithValidRequest()
        {
            // Arrange
            var (mockUserService, mockTokenService, _, controller) = Setup();

            var req = new RegisterRequestDto { Username = "testUser", Password = "pwd" };
            
            var regResponse = new RegisterResponseDto 
            { 
                Username = "testUser",
                Email = "email@test.com",
                Roles = new List<string> { "Student" }
            };
            
            mockUserService.Setup(x => x.Register(req)).Returns(regResponse);
            mockTokenService.Setup(x => x.GenerateToken("testUser", It.IsAny<IEnumerable<string>>(), 1)).Returns("tokenString");

            // Act
            var result = controller.Register(req) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var responseData = result.Value as RegisterResponseDto;
            Assert.NotNull(responseData);
            Assert.Equal("testUser", responseData.Username);
            Assert.Equal("tokenString", responseData.Token);
            Assert.Contains("Student", responseData.Roles);
        }

        [Fact]
        public void Login_ReturnsOkResult_WithValidCredentials()
        {
            // Arrange
            var (mockUserService, mockTokenService, _, controller) = Setup();

            var req = new LoginRequestDto { Username = "user", Password = "pwd" };
            
            var loginResp = new LoginResponseDto
            {
                Username = "user",
                Roles = new List<string> { "Admin" }
            };

            mockUserService.Setup(x => x.Login(req)).Returns(loginResp);
            mockTokenService.Setup(x => x.GenerateToken("user", It.IsAny<IEnumerable<string>>(), 1)).Returns("tokenString");

            // Act
            var result = controller.Login(req) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var responseData = result.Value as LoginResponseDto;
            Assert.NotNull(responseData);
            Assert.Equal("user", responseData.Username);
            Assert.Equal("tokenString", responseData.Token);
        }
    }
}
