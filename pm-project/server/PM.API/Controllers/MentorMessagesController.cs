using Microsoft.AspNetCore.Mvc;
using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Core.DTOs;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/mentors")]
    public class MentorMessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IChatService _chatService;
        private readonly ILogger<MentorMessagesController> _logger;

        public MentorMessagesController(AppDbContext db, IChatService chatService, ILogger<MentorMessagesController> logger)
        {
            _db = db;
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("{mentorId}/messages")]
        public async Task<IActionResult> SendToMentor(string mentorId, [FromBody] SendMessageDto dto)
        {
            var senderId = dto.SenderId;
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

            if (senderId == Guid.Empty)
                return BadRequest("senderId is required either in request body or via authenticated token");

            Guid mentorGuid = Guid.Empty;
            var isGuid = Guid.TryParse(mentorId, out mentorGuid);

            var chat = _db.Chats.FirstOrDefault(c =>
                (isGuid && ((c.User1Id == senderId && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == senderId)))
                || (!isGuid && c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId)));

            Guid effectiveSenderId = authUserId != Guid.Empty ? authUserId : senderId;
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
                chat = new Chat
                {
                    User1Id = isGuid ? senderId : Guid.Empty,
                    User2Id = isGuid ? mentorGuid : Guid.Empty,
                    ExternalMentorId = isGuid ? null : mentorId,
                    Name = isGuid ? $"chat_{effectiveSenderId}_{mentorGuid}" : $"chat_{effectiveSenderId}_ext_{mentorId}"
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();

                try
                {
                    if (isGuid)
                    {
                        var mp = _db.MentorProfiles.FirstOrDefault(m => m.UserId == mentorGuid);
                        if (mp != null)
                        {
                            mp.StudentsHelped += 1;
                            await _db.SaveChangesAsync();
                            _logger.LogInformation("SendToMentor: created chat {ChatId} sender={Sender} mentor={Mentor} incremented StudentsHelped={Count}", chat.Id, senderId, mentorGuid, mp.StudentsHelped);
                        }
                    }
                }
                catch { }
            }

            var message = await _chatService.SendMessageAsync(chat.Id, effectiveSenderId, dto.Content);

            var outMsg = new OutMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId,
                ChatId = message.ChatId
            };

            return CreatedAtAction(nameof(GetMessages), new { mentorId = mentorId, senderId = effectiveSenderId.ToString() }, outMsg);
        }

        [HttpPost("{mentorId}/start")]
        public async Task<IActionResult> StartChat(string mentorId, [FromBody] PM.Core.DTOs.SendMessageDto dto)
        {
            var senderId = dto?.SenderId ?? Guid.Empty;
            Guid authUserIdLocal = Guid.Empty;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var usernameLocal = User.Identity.Name;
                var authUserLocal = _db.Users.FirstOrDefault(u => u.Username == usernameLocal);
                if (authUserLocal != null)
                {
                    authUserIdLocal = authUserLocal.Id;
                    if (senderId == Guid.Empty) senderId = authUserLocal.Id;
                }
            }

            if (senderId == Guid.Empty)
                return BadRequest("senderId is required either in request body or via authenticated token");

            Guid mentorGuid = Guid.Empty;
            var isGuidLocal = Guid.TryParse(mentorId, out mentorGuid);

            var chat = _db.Chats.FirstOrDefault(c =>
                (isGuidLocal && ((c.User1Id == senderId && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == senderId)))
                || (!isGuidLocal && c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId)));

            var created = false;
            var effectiveSenderIdLocal = authUserIdLocal != Guid.Empty ? authUserIdLocal : senderId;
            Guid? resolvedMentorUserLocal = null;
            if (isGuidLocal)
            {
                resolvedMentorUserLocal = mentorGuid;
            }
            else
            {
                var maybeUserLocal = _db.Users.FirstOrDefault(u => u.Username == mentorId || u.Id.ToString() == mentorId);
                if (maybeUserLocal != null) resolvedMentorUserLocal = maybeUserLocal.Id;
                else
                {
                    var maybeMpLocal = _db.MentorProfiles.FirstOrDefault(m => m.Id.ToString() == mentorId || m.UserId.ToString() == mentorId);
                    if (maybeMpLocal != null) resolvedMentorUserLocal = maybeMpLocal.UserId;
                }
            }

            if (resolvedMentorUserLocal.HasValue && resolvedMentorUserLocal.Value == effectiveSenderIdLocal)
            {
                return BadRequest("cannot create chat targeting yourself");
            }
            if (chat == null)
            {
                chat = new Chat
                {
                    User1Id = isGuidLocal ? senderId : Guid.Empty,
                    User2Id = isGuidLocal ? mentorGuid : Guid.Empty,
                    ExternalMentorId = isGuidLocal ? null : mentorId,
                    Name = isGuidLocal ? $"chat_{effectiveSenderIdLocal}_{mentorGuid}" : $"chat_{effectiveSenderIdLocal}_ext_{mentorId}"
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
                created = true;
            }

            if (created)
            {
                try
                {
                    if (isGuidLocal)
                    {
                        var mp = _db.MentorProfiles.FirstOrDefault(m => m.UserId == mentorGuid);
                        if (mp != null)
                        {
                            mp.StudentsHelped += 1;
                            await _db.SaveChangesAsync();
                        }
                    }
                }
                catch { }
            }

            return Ok(new { chatId = chat.Id });
        }

        [HttpGet("{mentorId}/messages")]
        public async Task<IActionResult> GetMessages(string mentorId, [FromQuery] string? senderId)
        {
            Guid parsedSender = Guid.Empty;
            if (!string.IsNullOrEmpty(senderId))
            {
                Guid.TryParse(senderId, out parsedSender);
            }

            if (parsedSender == Guid.Empty && User?.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                    parsedSender = user.Id;
            }

            if (parsedSender == Guid.Empty)
                return BadRequest("senderId query parameter is required or provide an authenticated token");

            Guid mentorGuid = Guid.Empty;
            var isGuid = Guid.TryParse(mentorId, out mentorGuid);

            var chat = _db.Chats.FirstOrDefault(c =>
                (isGuid && ((c.User1Id == parsedSender && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == parsedSender)))
                || (!isGuid && c.ExternalMentorId == mentorId && (c.User1Id == parsedSender || c.User2Id == parsedSender)));

            if (chat == null)
                return Ok(new List<OutMessageDto>());

            var messages = (await _chatService.GetChatMessagesAsync(chat.Id)).Select(m => new OutMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                SenderId = m.SenderId,
                ChatId = m.ChatId
            }).ToList();

            return Ok(messages);
        }
    }
}
