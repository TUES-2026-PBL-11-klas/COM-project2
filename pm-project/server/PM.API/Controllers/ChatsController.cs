using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PM.Data.Context;
using Microsoft.EntityFrameworkCore;
using PM.Core.DTOs;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(AppDbContext db, ILogger<ChatsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("mine")]
        public IActionResult GetMyChats()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var myMentorProfileIds = _db.MentorProfiles.Where(m => m.UserId == user.Id).Select(m => m.Id.ToString()).ToList();
            var isMentor = _db.MentorProfiles.Any(m => m.UserId == user.Id);
            var myUserIdStr = user.Id.ToString();

            var chatEntities = _db.Chats.Where(c =>
                (c.User1Id == user.Id || c.User2Id == user.Id)
                && !(c.User1Id == user.Id && (c.User2Id == Guid.Empty || c.User2Id == user.Id))
                && !(c.User2Id == user.Id && (c.User1Id == Guid.Empty || c.User1Id == user.Id))
                && (string.IsNullOrEmpty(c.ExternalMentorId) || (!myMentorProfileIds.Contains(c.ExternalMentorId) && c.ExternalMentorId != myUserIdStr))
            ).Include(c => c.Messages).ToList();

            string FormatUsername(string? uname)
            {
                if (string.IsNullOrWhiteSpace(uname)) return string.Empty;
                var cleaned = System.Text.RegularExpressions.Regex.Replace(uname ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                return string.Join(' ', parts);
            }

            var chats = chatEntities.Select(c =>
            {
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
                    LastMessageContent = (c.Messages ?? Enumerable.Empty<Message>()).OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                    LastMessageAt = (c.Messages ?? Enumerable.Empty<Message>()).OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                    LastMessageSenderId = (c.Messages ?? Enumerable.Empty<Message>()).OrderByDescending(m => m.CreatedAt).Select(m => (Guid?)m.SenderId).FirstOrDefault()
                };
            }).ToList();

            var formattedUser = FormatUsername(user.Username);

            foreach (var c in chats)
            {
                var reasons = new List<string>();
                if (string.Equals(c.Name ?? string.Empty, formattedUser, StringComparison.OrdinalIgnoreCase)) reasons.Add("NameMatchesUser");
                if (!string.IsNullOrEmpty(c.ExternalMentorId) && (c.ExternalMentorId == myUserIdStr || myMentorProfileIds.Contains(c.ExternalMentorId))) reasons.Add("ExternalMentorIdMatches");
                if (c.User1Id == user.Id && c.User2Id == Guid.Empty) reasons.Add("PublicMentor_User1IsMentor");
                if (c.User2Id == user.Id && c.User1Id == Guid.Empty) reasons.Add("PublicMentor_User2IsMentor");
                if (c.User1Id == user.Id && c.User2Id == user.Id) reasons.Add("BothSlotsAreUser");
                if (reasons.Any())
                {
                    _logger.LogInformation("GetMyChats: excluding chat {ChatId} for reasons: {Reasons}", c.Id, string.Join(',', reasons));
                }
            }

            var filtered = chats.Where(c =>
                !string.Equals(c.Name ?? string.Empty, formattedUser, StringComparison.OrdinalIgnoreCase)
                && !( !string.IsNullOrEmpty(c.ExternalMentorId) && (c.ExternalMentorId == myUserIdStr || myMentorProfileIds.Contains(c.ExternalMentorId)) )
                && !(c.User1Id == user.Id && c.User2Id == Guid.Empty)
                && !(c.User2Id == user.Id && c.User1Id == Guid.Empty)
                && !(c.User1Id == user.Id && c.User2Id == user.Id)
            ).ToList();

            _logger.LogInformation("GetMyChats: returning {Count} chats (original {Original}) for user {UserId}", filtered.Count, chats.Count, user.Id);

            return Ok(filtered);
        }

        [Authorize]
        [HttpGet("mine/debug")] 
        public IActionResult GetMyChatsDebug()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var myMentorProfileIds = _db.MentorProfiles.Where(m => m.UserId == user.Id).Select(m => m.Id.ToString()).ToList();
            var myUserIdStr = user.Id.ToString();
            var chatEntities = _db.Chats.Include(c => c.Messages).ToList();

            var debug = chatEntities.Select(c => {
                var reasons = new List<string>();
                string FormatUsernameLocal(string? uname)
                {
                    if (string.IsNullOrWhiteSpace(uname)) return string.Empty;
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(uname ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                    var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                    return string.Join(' ', parts);
                }
                var formattedUser = FormatUsernameLocal(user.Username);
                if (string.Equals(c.Name ?? string.Empty, formattedUser, StringComparison.OrdinalIgnoreCase)) reasons.Add("NameMatchesUser");
                if (!string.IsNullOrEmpty(c.ExternalMentorId) && (c.ExternalMentorId == myUserIdStr || myMentorProfileIds.Contains(c.ExternalMentorId))) reasons.Add("ExternalMentorIdMatches");
                if (c.User1Id == user.Id && c.User2Id == Guid.Empty) reasons.Add("PublicMentor_User1IsMentor");
                if (c.User2Id == user.Id && c.User1Id == Guid.Empty) reasons.Add("PublicMentor_User2IsMentor");
                if (c.User1Id == user.Id && c.User2Id == user.Id) reasons.Add("BothSlotsAreUser");

                return new {
                    ChatId = c.Id,
                    Name = c.Name,
                    User1Id = c.User1Id,
                    User2Id = c.User2Id,
                    ExternalMentorId = c.ExternalMentorId,
                    ExclusionReasons = reasons,
                    LastMessageAt = (c.Messages ?? Enumerable.Empty<Message>()).OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault()
                };
            }).ToList();

            return Ok(debug);
        }

        [Authorize]
        [HttpPost("mine/cleanup")]
        public async Task<IActionResult> CleanupMySelfChats()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            string FormatUsernameLocal(string? uname)
            {
                if (string.IsNullOrWhiteSpace(uname)) return string.Empty;
                var cleaned = System.Text.RegularExpressions.Regex.Replace(uname ?? string.Empty, "[^a-zA-Z0-9]+", " ");
                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                return string.Join(' ', parts);
            }

            var formatted = FormatUsernameLocal(user.Username);

            var toDelete = _db.Chats.Where(c =>
                (c.User1Id == user.Id && c.User2Id == user.Id)
                || (!string.IsNullOrEmpty(c.Name) && c.Name == formatted)
            ).ToList();

            if (!toDelete.Any()) return Ok(new { deleted = 0 });

            _db.Chats.RemoveRange(toDelete);
            await _db.SaveChangesAsync();

            return Ok(new { deleted = toDelete.Count });
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
                (isGuid && ((c.User1Id == senderId && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == senderId)))
                || (isGuid && ((c.User1Id == mentorGuid && c.User2Id == Guid.Empty) || (c.User2Id == mentorGuid && c.User1Id == Guid.Empty)))
                || (!isGuid && c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId)));

            var created = false;

            Guid authUserId = Guid.Empty;
            if (!string.IsNullOrEmpty(username))
            {
                var authUser = _db.Users.FirstOrDefault(u => u.Username == username);
                if (authUser != null) authUserId = authUser.Id;
            }
            var effectiveSenderId = authUserId != Guid.Empty ? authUserId : senderId;

            Guid? resolvedMentorUser = null;
            if (isGuid)
            {
                resolvedMentorUser = mentorGuid;
            }
            else
            {
                var maybeUser = _db.Users.FirstOrDefault(u => u.Username == mentorId || u.Id.ToString() == mentorId);
                if (maybeUser != null) resolvedMentorUser = maybeUser.Id;
                else
                {
                    var maybeMp = _db.MentorProfiles.FirstOrDefault(m => m.Id.ToString() == mentorId || m.UserId.ToString() == mentorId);
                    if (maybeMp != null) resolvedMentorUser = maybeMp.UserId;
                }
            }

            if (resolvedMentorUser.HasValue && resolvedMentorUser.Value == effectiveSenderId)
            {
                return BadRequest("cannot create chat targeting yourself");
            }
            if (chat == null)
            {
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
                    User1Id = isGuid ? senderId : Guid.Empty,
                    User2Id = isGuid ? mentorGuid : Guid.Empty,
                    ExternalMentorId = isGuid ? null : mentorId,
                    Name = displayName ?? (isGuid ? $"chat_{effectiveSenderId}_{mentorGuid}" : $"chat_{effectiveSenderId}_ext_{mentorId}")
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
                created = true;
                _logger.LogInformation("CreateChat: created chat {ChatId} sender={Sender} mentor={MentorId} isGuid={IsGuid}", chat.Id, senderId, mentorId, isGuid);
            }

            var dtoOut = new ChatSummaryDto
            {
                Id = chat.Id,
                Name = chat.Name,
                User1Id = chat.User1Id,
                User2Id = chat.User2Id,
                ExternalMentorId = chat.ExternalMentorId
            };

            if (created && isGuid)
            {
                try
                {
                    var mp = _db.MentorProfiles.FirstOrDefault(m => m.UserId == mentorGuid);
                    if (mp != null)
                    {
                        mp.StudentsHelped += 1;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("CreateChat: incremented StudentsHelped for mentor {MentorGuid} -> {NewCount}", mentorGuid, mp.StudentsHelped);
                    }
                }
                catch { }
            }

            return CreatedAtAction(nameof(GetMyChats), dtoOut);
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessagesByChatId(Guid chatId)
        {
            var chat = _db.Chats.FirstOrDefault(c => c.Id == chatId);
            if (chat == null) return Ok(new List<Core.DTOs.OutMessageDto>());

            var msgs = await new PM.Core.Services.ChatService(_db).GetChatMessagesAsync(chatId) ?? Enumerable.Empty<PM.Data.Entities.Message>();
            var messages = msgs.Select(m => new Core.DTOs.OutMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                SenderId = m.SenderId,
                ChatId = m.ChatId
            }).ToList();

            return Ok(messages);
        }

        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> PostMessageToChat(Guid chatId, [FromBody] PM.Core.DTOs.SendMessageDto dto)
        {
            Guid senderId = dto?.SenderId ?? Guid.Empty;
            Guid authUserId = Guid.Empty;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var authUser = _db.Users.FirstOrDefault(u => u.Username == username);
                if (authUser != null)
                {
                    authUserId = authUser.Id;
                    if (senderId == Guid.Empty) senderId = authUser.Id;
                }
            }

            if (senderId == Guid.Empty) return BadRequest("senderId is required either in body or via authenticated token");

            var chat = _db.Chats.FirstOrDefault(c => c.Id == chatId);
            if (chat == null) return NotFound();

            var effectiveSenderId = authUserId != Guid.Empty ? authUserId : senderId;

            var content = dto?.Content ?? string.Empty;
            var message = await new PM.Core.Services.ChatService(_db).SendMessageAsync(chatId, effectiveSenderId, content);

            var outMsg = new Core.DTOs.OutMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId,
                ChatId = message.ChatId
            };

            return CreatedAtAction(nameof(GetMessagesByChatId), new { chatId = chatId }, outMsg);
        }
    }
}
