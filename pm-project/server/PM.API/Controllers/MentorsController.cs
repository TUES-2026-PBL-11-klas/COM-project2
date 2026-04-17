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

        [AllowAnonymous]
        [HttpGet("list")]
        public IActionResult List()
        {
                var mentors = _db.MentorProfiles.ToList().Select(mp =>
            {
                var user = _db.Users.FirstOrDefault(u => u.Id == mp.UserId);
                var reviews = _db.Reviews.Where(r => r.ReviewedUserId.HasValue && r.ReviewedUserId.Value == mp.UserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new {
                        id = r.Id,
                        name = r.ReviewerName,
                        rating = r.Rating,
                        comment = r.Content,
                        date = r.CreatedAt
                    }).ToList();

                double? avg = null;
                if (reviews.Count > 0)
                {
                    avg = reviews.Average(r => (double)r.rating);
                }

                // derive a friendly display name from username (e.g. ivan.petrov -> Ivan Petrov)
                string? displayName = null;
                if (user?.Username != null)
                {
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(user.Username, "[^a-zA-Z0-9]+", " ");
                    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Length > 0)
                            parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                    }
                    displayName = string.Join(' ', parts);
                }

                var avatarUrl = displayName != null ?
                    $"https://ui-avatars.com/api/?name={System.Uri.EscapeDataString(displayName)}&background=2563EB&color=fff" :
                    null;

                return new
                {
                    // expose the mentor's user id as the canonical id (used when starting chats)
                    id = mp.UserId.ToString(),
                    profileId = mp.Id.ToString(),
                    userId = mp.UserId.ToString(),
                    name = displayName,
                    subjects = mp.Subjects,
                    students = mp.StudentsHelped,
                    rating = avg,
                    avatar = avatarUrl,
                    reviews = reviews
                };
            }).ToList();

            return Ok(mentors);
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
                if (user != null)
                {
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(user.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                    return Ok(new { displayName = string.Join(' ', parts) });
                }
            }

            // try by username
            var u2 = _db.Users.FirstOrDefault(u => u.Username == id);
            if (u2 != null)
            {
                var cleaned = System.Text.RegularExpressions.Regex.Replace(u2.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                return Ok(new { displayName = string.Join(' ', parts) });
            }

            // try mentor profiles (match by MentorProfile.Id or MentorProfile.UserId)
            var mp = _db.MentorProfiles.FirstOrDefault(m => m.Id.ToString() == id || m.UserId.ToString() == id);
            if (mp != null)
            {
                var owner = _db.Users.FirstOrDefault(u => u.Id == mp.UserId);
                if (owner != null)
                {
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(owner.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                    return Ok(new { displayName = string.Join(' ', parts) });
                }
            }

            // fallback: nothing found
            return Ok(new { displayName = (string?)null });
        }
    }
}
