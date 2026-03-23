using System.Collections.Generic;

namespace PM.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string username, IEnumerable<string> roles, int expireHours = 1);
    }
}