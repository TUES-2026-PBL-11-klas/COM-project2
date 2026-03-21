using PM.Core.Interfaces;
using PM.Core.DTOs;
using PM.Data.Entities;
using PM.Data.Repositories;
using Microsoft.AspNetCore.Identity;

namespace PM.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly PasswordHasher<UserDMO> _hasher;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
            _hasher = new PasswordHasher<UserDMO>();
        }

        public RegisterResponseDto Register(RegisterRequestDto request)
        {
            if (_repo.GetByUsername(request.Username) != null)
                throw new Exception("Username already exists");

            var user = new UserDMO
            {
                Username = request.Username,
                Email = request.Email
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);
            _repo.AddUser(user);
            _repo.SaveChanges();

            return new RegisterResponseDto
            {
                Username = user.Username,
                Email = user.Email
            };
        }

        public LoginResponseDto Login(LoginRequestDto request)
        {
            var user = _repo.GetByUsername(request.Username)
                ?? throw new Exception("User not found"); // trqq se adne custom exception

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
                throw new Exception("Invalid password");

            return new LoginResponseDto
            {
                Username = user.Username
            };
        }
    }
}