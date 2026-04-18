using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PM.Core.DTOs;
using PM.Data.Context;
using PM.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PM.API.Hubs;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/mentors")]
    public class MentorsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<MentorsController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public MentorsController(AppDbContext db, ILogger<MentorsController> logger, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _logger = logger;
            _hubContext = hubContext;
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
                    id = mp.UserId.ToString(),
                    profileId = mp.Id.ToString(),
                    userId = mp.UserId.ToString(),
                    name = displayName,
                    subjects = mp.Subjects,
                    students = mp.StudentsHelped,
                    createdAt = mp.CreatedAt,
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

            return Ok(new { userId = user.Id, username = user.Username, isMentor = true });
        }

        [Authorize]
        [HttpPost("resign")]
        public async Task<IActionResult> ResignAsMentor()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = _db.Users.Include(u => u.Roles).FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound();

            var roles = user.Roles;
            if (roles != null)
            {
                var toRemove = roles.FirstOrDefault(r => r.Name == "Mentor");
                if (toRemove != null)
                {
                    roles.Remove(toRemove);
                }
            }

            var existingProfile = _db.MentorProfiles.FirstOrDefault(m => m.UserId == user.Id);
            var deletedChatIds = new List<Guid>();
            if (existingProfile != null)
            {
                var profileIdStr = existingProfile.Id.ToString();
                var userIdStr = user.Id.ToString();
                var mentorGuid = user.Id;

                var chats = _db.Chats
                    .Include(c => c.Messages)
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.ExternalMentorId) && (c.ExternalMentorId == profileIdStr || c.ExternalMentorId == userIdStr))
                        || c.User1Id == mentorGuid
                        || c.User2Id == mentorGuid
                    ).ToList();

                foreach (var c in chats)
                {
                    if (c.Messages != null && c.Messages.Any())
                    {
                        _db.Messages.RemoveRange(c.Messages);
                    }
                    deletedChatIds.Add(c.Id);
                    _db.Chats.Remove(c);
                }

                var remainingMessages = _db.Messages.Where(m => m.SenderId == user.Id && !deletedChatIds.Contains(m.ChatId)).ToList();
                if (remainingMessages.Any())
                {
                    _db.Messages.RemoveRange(remainingMessages);
                }

                _db.MentorProfiles.Remove(existingProfile);
            }

            await _db.SaveChangesAsync();

            try
            {
                if (deletedChatIds != null && deletedChatIds.Any())
                {
                    await _hubContext.Clients.All.SendAsync("ChatsDeleted", deletedChatIds);
                }
            }
            catch { }

            return Ok(new { userId = user.Id, username = user.Username });
        }

        [AllowAnonymous]
        [HttpGet("resolve/{id}")]
        public IActionResult ResolveDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

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

            var u2 = _db.Users.FirstOrDefault(u => u.Username == id);
            if (u2 != null)
            {
                var cleaned = System.Text.RegularExpressions.Regex.Replace(u2.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                return Ok(new { displayName = string.Join(' ', parts) });
            }

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

            return Ok(new { displayName = (string?)null });
        }
    }
}
