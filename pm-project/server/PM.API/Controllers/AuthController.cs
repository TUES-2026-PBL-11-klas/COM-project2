using Microsoft.AspNetCore.Mvc;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public AuthController(IUserService userService, ITokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDto request)
        {
            var user = _userService.Register(request);
            var roles = user.Roles?.ToList() ?? new List<string>();
            var token = _tokenService.GenerateToken(user.Username, roles);

            return Ok(new RegisterResponseDto
            {
                Username = user.Username,
                Email = user.Email,
                Roles = roles,
                Token = token
            });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            var user = _userService.Login(request);
            var roles = user.Roles?.ToList() ?? new List<string>();

            var token = _tokenService.GenerateToken(user.Username, roles);

            return Ok(new LoginResponseDto
            {
                Username = user.Username,
                Roles = roles,
                Token = token
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("promote/{username}")]
        public IActionResult PromoteUser(string username, [FromBody] List<string> roleNames)
        {
            _userService.UpdateUserRole(username, roleNames);
            return Ok($"User '{username}' roles promoted to: {string.Join(", ", roleNames)}");        
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            return Ok(new { Username = User?.Identity?.Name });
        }
        // tva za test prosto
    }
}