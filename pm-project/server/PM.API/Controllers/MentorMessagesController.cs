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

        public MentorMessagesController(AppDbContext db, IChatService chatService)
        {
            _db = db;
            _chatService = chatService;
        }

        [HttpPost("{mentorId}/messages")]
        public async Task<IActionResult> SendToMentor(string mentorId, [FromBody] SendMessageDto dto)
        {
            var senderId = dto.SenderId;
            if (senderId == Guid.Empty && User?.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                    senderId = user.Id;
            }

            if (senderId == Guid.Empty)
                return BadRequest("senderId is required either in request body or via authenticated token");

            Guid mentorGuid = Guid.Empty;
            var isGuid = Guid.TryParse(mentorId, out mentorGuid);

            var chat = _db.Chats.FirstOrDefault(c =>
                (isGuid && ((c.User1Id == senderId && c.User2Id == mentorGuid) || (c.User1Id == mentorGuid && c.User2Id == senderId)))
                || (!isGuid && c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId)));

            if (chat == null)
            {
                chat = new Chat
                {
                    User1Id = senderId,
                    User2Id = isGuid ? mentorGuid : Guid.Empty,
                    ExternalMentorId = isGuid ? null : mentorId,
                    Name = isGuid ? $"chat_{senderId}_{mentorGuid}" : $"chat_{senderId}_ext_{mentorId}"
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
            }

            var message = await _chatService.SendMessageAsync(chat.Id, senderId, dto.Content);

            var outMsg = new OutMessageDto
            {
                Id = message.Id,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                SenderId = message.SenderId,
                ChatId = message.ChatId
            };

            return CreatedAtAction(nameof(GetMessages), new { mentorId = mentorId, senderId = senderId.ToString() }, outMsg);
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
