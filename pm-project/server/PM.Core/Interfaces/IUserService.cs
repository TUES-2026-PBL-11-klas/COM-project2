using PM.Core.DTOs;

namespace PM.Core.Interfaces
{
    public interface IUserService
    {
        LoginResponseDto Login(LoginRequestDto request);
        RegisterResponseDto Register(RegisterRequestDto request);
        void UpdateUserRole(string username, List<string> roleName);
    }
}