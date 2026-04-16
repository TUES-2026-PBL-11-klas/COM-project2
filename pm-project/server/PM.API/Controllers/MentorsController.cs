using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PM.Core.DTOs;
using PM.Data.Context;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/mentors")]
    public class MentorsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<MentorsController> _logger;

        public MentorsController(AppDbContext db, ILogger<MentorsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMentor([FromBody] CreateMentorDto dto)
        {
            // Accept either an authenticated user or a provided userId in the payload.
            PM.Data.Entities.UserDMO? user = null;

            var username = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(username))
            {
                user = _db.Users.FirstOrDefault(u => u.Username == username);
            }

            if (user == null && !string.IsNullOrWhiteSpace(dto?.UserId))
            {
                // Only accept explicit GUID user ids from the client when not authenticated.
                // Reject arbitrary username-like strings to avoid accidentally resolving other users.
                if (Guid.TryParse(dto.UserId, out var parsed))
                {
                    user = _db.Users.FirstOrDefault(u => u.Id == parsed);
                }
            }

            if (user == null)
            {
                _logger.LogWarning("CreateMentor: could not resolve user. dto.UserId={UserId}", dto?.UserId);
                return Unauthorized();
            }

            var subjects = dto?.Subjects ?? new string[0];

            var existing = _db.MentorProfiles.FirstOrDefault(m => m.UserId == user!.Id);
            if (existing == null)
            {
                var prof = new MentorProfile
                {
                    UserId = user.Id,
                    Subjects = string.Join(',', subjects),
                };
                _db.MentorProfiles.Add(prof);
                _logger.LogDebug("CreateMentor: created MentorProfile for user {UserId}", user.Id);
            }
            else
            {
                existing.Subjects = string.Join(',', subjects);
                _logger.LogDebug("CreateMentor: updated MentorProfile for user {UserId}", user.Id);
            }

            // ensure Mentor role (create if missing)
            var mentorRole = _db.Roles.FirstOrDefault(r => r.Name == "Mentor");
            if (mentorRole == null)
            {
                mentorRole = new Role { Name = "Mentor" };
                _db.Roles.Add(mentorRole);
            }
            if (!user!.Roles.Any(r => r.Name == "Mentor"))
            {
                user.Roles.Add(mentorRole);
            }

            await _db.SaveChangesAsync();

            await _db.SaveChangesAsync();

            // Do not create a chat when a user becomes a mentor.
            // Chats will be created when other users initiate conversations.
            return Ok(new { userId = user.Id, username = user.Username });
        }

        [AllowAnonymous]
        [HttpGet("resolve/{id}")]
        public IActionResult ResolveDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            // try guid
            Guid g;
            if (Guid.TryParse(id, out g))
            {
                var user = _db.Users.FirstOrDefault(u => u.Id == g);
                if (user != null) return Ok(new { displayName = user.Username });
            }

            // try by username
            var u2 = _db.Users.FirstOrDefault(u => u.Username == id);
            if (u2 != null) return Ok(new { displayName = u2.Username });

            // try mentor profiles (match by MentorProfile.Id or MentorProfile.UserId)
            var mp = _db.MentorProfiles.FirstOrDefault(m => m.Id.ToString() == id || m.UserId.ToString() == id);
            if (mp != null)
            {
                var owner = _db.Users.FirstOrDefault(u => u.Id == mp.UserId);
                if (owner != null) return Ok(new { displayName = owner.Username });
            }

            // fallback: nothing found
            return Ok(new { displayName = (string?)null });
        }
    }
}
