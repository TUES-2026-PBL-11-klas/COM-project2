using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;
using PM.Core.Services;
using Moq;

namespace PM.Tests
{
    public class TokenServiceTests
    {
        [Fact]
        public void GenerateToken_Throws_WhenKeyMissing()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

            var svc = new TokenService(configMock.Object);

            Assert.Throws<System.InvalidOperationException>(() => svc.GenerateToken("user", new List<string>{"Role"}));
        }

        [Fact]
        public void GenerateToken_ReturnsToken_WhenConfigPresent()
        {
            var dict = new Dictionary<string, string?>
            {
                { "Jwt:Key", new string('a', 64) },
                { "Jwt:Issuer", "issuer" },
                { "Jwt:Audience", "aud" }
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            var svc = new TokenService(configuration);

            var token = svc.GenerateToken("user", new[] { "Admin" });

            Assert.False(string.IsNullOrWhiteSpace(token));
        }
    }
}
