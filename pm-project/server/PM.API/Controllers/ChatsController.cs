using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PM.Data.Context;
using PM.Core.DTOs;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ChatsController(AppDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpGet("mine")]
        public IActionResult GetMyChats()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var chatEntities = _db.Chats.Where(c => c.User1Id == user.Id || c.User2Id == user.Id).ToList();

            var chats = chatEntities.Select(c =>
            {
                // resolve a friendly display name using title-casing for usernames
                string displayName = c.Name ?? string.Empty;

                string FormatUsername(string? uname)
                {
                    if (string.IsNullOrWhiteSpace(uname)) return string.Empty;
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(uname ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                    return string.Join(' ', parts);
                }

                if (!string.IsNullOrWhiteSpace(c.ExternalMentorId))
                {
                    // try resolve external mentor id to a user
                    var owner = _db.Users.FirstOrDefault(u => u.Id.ToString() == c.ExternalMentorId || u.Username == c.ExternalMentorId);
                    if (owner != null) displayName = FormatUsername(owner.Username);
                    else
                    {
                        var mp = _db.MentorProfiles.FirstOrDefault(m => m.Id.ToString() == c.ExternalMentorId || m.UserId.ToString() == c.ExternalMentorId);
                        if (mp != null)
                        {
                            var owner2 = _db.Users.FirstOrDefault(u => u.Id == mp.UserId);
                            if (owner2 != null) displayName = FormatUsername(owner2.Username);
                        }
                    }
                }
                else
                {
                    // if no external id, prefer the other participant's username
                    var otherId = c.User1Id == user.Id ? c.User2Id : c.User1Id;
                    if (otherId != Guid.Empty)
                    {
                        var other = _db.Users.FirstOrDefault(u => u.Id == otherId);
                        if (other != null) displayName = FormatUsername(other.Username);
                    }
                }

                return new ChatSummaryDto
                {
                    Id = c.Id,
                    Name = displayName,
                    User1Id = c.User1Id,
                    User2Id = c.User2Id,
                    ExternalMentorId = c.ExternalMentorId,
                    LastMessageContent = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                    LastMessageAt = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                    LastMessageSenderId = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (Guid?)m.SenderId).FirstOrDefault()
                };
            }).ToList();

            return Ok(chats);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatDto dto)
        {
            var username = User?.Identity?.Name;
            if (username == null && dto.SenderId == null) return BadRequest("senderId or auth required");

            Guid senderId = Guid.Empty;
            if (dto.SenderId.HasValue)
                senderId = dto.SenderId.Value;

            if (senderId == Guid.Empty && username != null)
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Unauthorized();
                senderId = user.Id;
            }

            if (senderId == Guid.Empty) return BadRequest("senderId is required either in body or via auth");

            var mentorId = dto.MentorId;
            Guid mentorGuid = Guid.Empty;
            var isGuid = Guid.TryParse(mentorId, out mentorGuid);

            var chat = _db.Chats.FirstOrDefault(c =>
                // exact two-user chat (either order)
                (isGuid && ((c.User1Id == senderId && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == senderId)))
                // public mentor chat where mentor created a public entry (other slot empty)
                || (isGuid && ((c.User1Id == mentorGuid && c.User2Id == Guid.Empty) || (c.User2Id == mentorGuid && c.User1Id == Guid.Empty)))
                // external mentor id match for non-guid mentors
                || (!isGuid && c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId)));

            var created = false;
            if (chat == null)
            {
                // try to determine a human-friendly display name for the mentor
                string? displayName = null;
                if (isGuid)
                {
                    var owner = _db.Users.FirstOrDefault(u => u.Id == mentorGuid);
                    if (owner != null)
                    {
                        var cleaned = System.Text.RegularExpressions.Regex.Replace(owner.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                        displayName = string.Join(' ', parts);
                    }
                    else
                    {
                        var mp = _db.MentorProfiles.FirstOrDefault(m => m.UserId == mentorGuid || m.Id == mentorGuid);
                        if (mp != null)
                        {
                            var owner2 = _db.Users.FirstOrDefault(u => u.Id == mp.UserId);
                            if (owner2 != null)
                            {
                                var cleaned = System.Text.RegularExpressions.Regex.Replace(owner2.Username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                                displayName = string.Join(' ', parts);
                            }
                        }
                    }
                }

                chat = new Chat
                {
                    User1Id = senderId,
                    User2Id = isGuid ? mentorGuid : Guid.Empty,
                    ExternalMentorId = isGuid ? null : mentorId,
                    Name = displayName ?? (isGuid ? $"chat_{senderId}_{mentorGuid}" : $"chat_{senderId}_ext_{mentorId}")
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
                created = true;
            }

            var dtoOut = new ChatSummaryDto
            {
                Id = chat.Id,
                Name = chat.Name,
                User1Id = chat.User1Id,
                User2Id = chat.User2Id,
                ExternalMentorId = chat.ExternalMentorId
            };

            // when we created a new chat increment MentorProfile.StudentsHelped if this chat targets a local mentor
            if (created && isGuid)
            {
                try
                {
                    var mp = _db.MentorProfiles.FirstOrDefault(m => m.UserId == mentorGuid);
                    if (mp != null)
                    {
                        mp.StudentsHelped += 1;
                        await _db.SaveChangesAsync();
                    }
                }
                catch { }
            }

            return CreatedAtAction(nameof(GetMyChats), dtoOut);
        }
    }
}
