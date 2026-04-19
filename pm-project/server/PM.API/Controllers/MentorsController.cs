using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PM.API.Hubs;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/mentors")]
    public class MentorsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IChatService _chatService;
        private readonly ILogger<MentorsController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public MentorsController(AppDbContext db, IChatService chatService, ILogger<MentorsController> logger, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _chatService = chatService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var mentorProfiles = await _db.MentorProfiles
                .Include(mp => mp.User)
                .OrderByDescending(mp => mp.CreatedAt)
                .ToListAsync();

            var mentors = mentorProfiles.Select(mp =>
            {
                var reviews = _db.Reviews
                    .Where(r => r.ReviewedUserId == mp.UserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.ReviewerName,
                        rating = r.Rating,
                        comment = r.Content,
                        date = r.CreatedAt
                    })
                    .ToList();

                double? averageRating = reviews.Count == 0 ? null : reviews.Average(r => (double)r.rating);
                var displayName = FormatDisplayName(mp.User.Username);

                return new
                {
                    id = mp.UserId.ToString(),
                    profileId = mp.Id.ToString(),
                    userId = mp.UserId.ToString(),
                    name = displayName,
                    subjects = mp.Subjects,
                    students = mp.StudentsHelped,
                    rating = averageRating,
                    experience = mp.Experience,
                    available = mp.Available,
                    createdAt = mp.CreatedAt,
                    avatar = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(displayName)}&background=2563EB&color=fff",
                    reviews
                };
            }).ToList();

            return Ok(mentors);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateMentor([FromBody] CreateMentorDto dto)
        {
            var user = await ResolveUserAsync(dto?.UserId);
            if (user == null)
            {
                _logger.LogWarning("CreateMentor: could not resolve user. dto.UserId={UserId}", dto?.UserId);
                return Unauthorized();
            }

            var subjects = dto?.Subjects?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            var mentorProfile = await _db.MentorProfiles.FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (mentorProfile == null)
            {
                mentorProfile = new MentorProfile
                {
                    UserId = user.Id,
                    Subjects = string.Join(',', subjects),
                    Experience = "New mentor",
                    Available = true
                };
                _db.MentorProfiles.Add(mentorProfile);
            }
            else
            {
                mentorProfile.Subjects = string.Join(',', subjects);
                mentorProfile.Available = true;
            }

            var mentorRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Mentor");
            if (mentorRole == null)
            {
                mentorRole = new Role { Name = "Mentor" };
                _db.Roles.Add(mentorRole);
            }

            if (!user.Roles.Any(r => r.Name == "Mentor"))
            {
                user.Roles.Add(mentorRole);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                userId = user.Id,
                username = user.Username,
                profileId = mentorProfile.Id,
                isMentor = true
            });
        }

        [Authorize]
        [HttpPost("resign")]
        public async Task<IActionResult> ResignAsMentor()
        {
            var user = await ResolveUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var mentorRole = user.Roles.FirstOrDefault(r => r.Name == "Mentor");
            if (mentorRole != null)
            {
                user.Roles.Remove(mentorRole);
            }

            var existingProfile = await _db.MentorProfiles.FirstOrDefaultAsync(m => m.UserId == user.Id);
            IReadOnlyCollection<Guid> deletedChatIds = Array.Empty<Guid>();

            if (existingProfile != null)
            {
                deletedChatIds = await _chatService.DeleteMentorChatsAsync(user.Id, existingProfile.Id);
                _db.MentorProfiles.Remove(existingProfile);
            }

            await _db.SaveChangesAsync();

            if (deletedChatIds.Count > 0)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ChatsDeleted", deletedChatIds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast deleted chats for mentor {UserId}", user.Id);
                }
            }

            return Ok(new { userId = user.Id, username = user.Username });
        }

        [AllowAnonymous]
        [HttpGet("resolve/{id}")]
        public async Task<IActionResult> ResolveDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await ResolveUserByIdentifierAsync(id);
            return Ok(new { displayName = user != null ? FormatDisplayName(user.Username) : (string?)null });
        }

        private async Task<UserDMO?> ResolveUserAsync(string? explicitUserId = null)
        {
            var username = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(username))
            {
                var byName = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == username);
                if (byName != null)
                {
                    return byName;
                }
            }

            if (!string.IsNullOrWhiteSpace(explicitUserId) && Guid.TryParse(explicitUserId, out var parsedUserId))
            {
                return await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == parsedUserId);
            }

            return null;
        }

        private async Task<UserDMO?> ResolveUserByIdentifierAsync(string id)
        {
            if (Guid.TryParse(id, out var parsed))
            {
                var directUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == parsed);
                if (directUser != null)
                {
                    return directUser;
                }

                var mentorProfile = await _db.MentorProfiles.FirstOrDefaultAsync(m => m.Id == parsed);
                if (mentorProfile != null)
                {
                    return await _db.Users.FirstOrDefaultAsync(u => u.Id == mentorProfile.UserId);
                }
            }

            var byUsername = await _db.Users.FirstOrDefaultAsync(u => u.Username == id);
            if (byUsername != null)
            {
                return byUsername;
            }

            var byProfileString = await _db.MentorProfiles.FirstOrDefaultAsync(m => m.Id.ToString() == id || m.UserId.ToString() == id);
            if (byProfileString != null)
            {
                return await _db.Users.FirstOrDefaultAsync(u => u.Id == byProfileString.UserId);
            }

            return null;
        }

        private static string FormatDisplayName(string username)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..];
                }
            }

            return parts.Length == 0 ? "Mentor" : string.Join(' ', parts);
        }
    }
}
