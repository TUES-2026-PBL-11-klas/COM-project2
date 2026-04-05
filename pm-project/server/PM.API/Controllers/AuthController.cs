using Microsoft.AspNetCore.Mvc;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDto request)
        {
            _logger.LogInformation("Register attempt for " + request.Username);
            var user = _userService.Register(request);
            var roles = user.Roles?.ToList() ?? new List<string>();
            var token = _tokenService.GenerateToken(user.Username, roles);

            _logger.LogInformation("User registered: " + user.Username);

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
            _logger.LogInformation("Login attempt for " + request.Username);

            var user = _userService.Login(request);
            var roles = user.Roles?.ToList() ?? new List<string>();

            var token = _tokenService.GenerateToken(user.Username, roles);

            _logger.LogInformation("User logged in: " + user.Username);

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
            _logger.LogInformation("Promote user " + username + " to roles: " + string.Join(", ", roleNames));
            _userService.UpdateUserRole(username, roleNames);
            _logger.LogInformation("User " + username + " promoted");
            return Ok("User '" + username + "' roles promoted to: " + string.Join(", ", roleNames));        
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var name = User?.Identity?.Name;
            _logger.LogInformation("GetMe called for " + name);
            return Ok(new { Username = name });
        }
        // tva za test prosto
    }
}