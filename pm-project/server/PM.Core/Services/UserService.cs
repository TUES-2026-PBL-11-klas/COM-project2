using PM.Core.Interfaces;
using PM.Core.DTOs;
using PM.Data.Entities;
using PM.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using PM.Core.Exceptions;
using PM.Data.Context;

namespace PM.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly PasswordHasher<UserDMO> _hasher;
        private readonly AppDbContext _context;

        public UserService(IUserRepository repo, AppDbContext context)
        {
            _repo = repo;
            _hasher = new PasswordHasher<UserDMO>();
            _context = context;
        }

        public RegisterResponseDto Register(RegisterRequestDto request)
        {
            if (_repo.GetByUsername(request.Username) != null)
                throw new UserAlreadyExistsException(request.Username);

            var user = new UserDMO
            {
                Username = request.Username
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            if (request.Roles != null && request.Roles.Count != 0)
            {
                var rolesFromDb = _context.Roles
                    .Where(r => request.Roles.Contains(r.Name))
                    .ToList();

                foreach (var role in rolesFromDb)
                {
                    if (!user.Roles.Any(r => r.Id == role.Id))
                        user.Roles.Add(role);
                }
            }
            else
            {
                var defaultRole = _context.Roles.FirstOrDefault(r => r.Name == "Student");
                if (defaultRole != null)
                    user.Roles.Add(defaultRole);
            }

            _repo.AddUser(user);
            _repo.SaveChanges();

            return new RegisterResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }

        public LoginResponseDto Login(LoginRequestDto request)
        {
            var user = _repo.GetByUsername(request.Username)
                ?? throw new InvalidCredentialsException();

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
                throw new InvalidCredentialsException();

            return new LoginResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles.Select(r => r.Name).ToList()
            };
        }

        public void UpdateUserRole(string username, List<string> roleNames)
        {
            var user = _repo.GetByUsername(username) ?? throw new UserNotFoundException(username);

            user.Roles.Clear();

            foreach (var roleName in roleNames)
            {
                var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role != null)
                    user.Roles.Add(role);
            }

            _repo.SaveChanges();
        }
    }
}