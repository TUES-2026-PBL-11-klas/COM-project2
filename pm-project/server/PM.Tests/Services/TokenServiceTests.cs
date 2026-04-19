using Microsoft.Extensions.Configuration;
using Moq;
using PM.Core.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace PM.Tests.Services
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateToken_ReturnsValidJwt_WhenConfigurationIsCorrect()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.SetupGet(x => x[It.Is<string>(s => s == "Jwt:Key")]).Returns("ThisIsAVerySecretKeyForTesting1234567890!");
            mockConfig.SetupGet(x => x[It.Is<string>(s => s == "Jwt:Issuer")]).Returns("TestIssuer");
            mockConfig.SetupGet(x => x[It.Is<string>(s => s == "Jwt:Audience")]).Returns("TestAudience");

            var tokenService = new TokenService(mockConfig.Object);
            var roles = new List<string> { "Student" };

            var tokenString = tokenService.GenerateToken("testuser", roles);

            Assert.False(string.IsNullOrEmpty(tokenString));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Equal("TestAudience", jwtToken.Audiences.First());
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Student");
        }

        [Fact]
        public void GenerateToken_ThrowsException_WhenKeyIsMissing()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.SetupGet(x => x[It.IsRegex("Jwt:Key")]).Returns((string?)null);

            var tokenService = new TokenService(mockConfig.Object);

            Assert.Throws<InvalidOperationException>(() => tokenService.GenerateToken("testuser", new List<string>()));
        }
    }
}
