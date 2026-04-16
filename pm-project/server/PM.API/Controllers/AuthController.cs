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
        private readonly PM.Data.Context.AppDbContext _context;

        public AuthController(IUserService userService, ITokenService tokenService, ILogger<AuthController> logger, PM.Data.Context.AppDbContext context)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequestDto request)
        {
            _logger.LogInformation("Register attempt for " + request.Username);
            var resp = _userService.Register(request);
            var roles = resp.Roles?.ToList() ?? new List<string>();
            var token = _tokenService.GenerateToken(resp.Username, roles);

            _logger.LogInformation("User registered: " + resp.Username);

            resp.Token = token;
            return Ok(resp);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Login attempt for " + request.Username);

            var resp = _userService.Login(request);
            var roles = resp.Roles?.ToList() ?? new List<string>();

            var token = _tokenService.GenerateToken(resp.Username, roles);

            _logger.LogInformation("User logged in: " + resp.Username);

            resp.Token = token;
            return Ok(resp);
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

            var user = _context.Users.FirstOrDefault(u => u.Username == name);
            if (user == null)
                return NotFound();

            var roles = user.Roles?.Select(r => r.Name).ToList() ?? new List<string>();

            return Ok(new { Username = name, Id = user.Id, Roles = roles });
        }
        // tva za test prosto
    }
}