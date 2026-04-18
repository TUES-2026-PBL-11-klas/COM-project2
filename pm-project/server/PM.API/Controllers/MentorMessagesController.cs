using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;

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
            var senderId = await ResolveSenderIdAsync(dto.SenderId);
            if (senderId == Guid.Empty)
            {
                return BadRequest("senderId is required either in request body or via authenticated token");
            }

            try
            {
                var chat = await _chatService.CreateOrGetChatAsync(senderId, mentorId);
                var message = await _chatService.SendMessageAsync(chat.Id, senderId, dto.Content);

                return CreatedAtAction(nameof(GetMessages), new { mentorId, senderId }, new OutMessageDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    SenderId = message.SenderId,
                    ChatId = message.ChatId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{mentorId}/start")]
        public async Task<IActionResult> StartChat(string mentorId, [FromBody] SendMessageDto dto)
        {
            var senderId = await ResolveSenderIdAsync(dto.SenderId);
            if (senderId == Guid.Empty)
            {
                return BadRequest("senderId is required either in request body or via authenticated token");
            }

            try
            {
                var chat = await _chatService.CreateOrGetChatAsync(senderId, mentorId);
                return Ok(new { chatId = chat.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{mentorId}/messages")]
        public async Task<IActionResult> GetMessages(string mentorId, [FromQuery] string? senderId)
        {
            var parsedSenderId = await ResolveSenderIdAsync(ParseSenderId(senderId));
            if (parsedSenderId == Guid.Empty)
            {
                return BadRequest("senderId query parameter is required or provide an authenticated token");
            }

            var chat = await FindExistingChatAsync(parsedSenderId, mentorId);
            if (chat == null)
            {
                return Ok(new List<OutMessageDto>());
            }

            var messages = await _chatService.GetChatMessagesAsync(chat.Id);
            return Ok(messages.Select(m => new OutMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                SenderId = m.SenderId,
                ChatId = m.ChatId
            }).ToList());
        }

        private static Guid ParseSenderId(string? senderId)
        {
            return Guid.TryParse(senderId, out var parsed) ? parsed : Guid.Empty;
        }

        private async Task<Guid> ResolveSenderIdAsync(Guid candidate)
        {
            if (candidate != Guid.Empty)
            {
                return candidate;
            }

            var username = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Guid.Empty;
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.Id ?? Guid.Empty;
        }

        private async Task<Chat?> FindExistingChatAsync(Guid senderId, string mentorId)
        {
            if (Guid.TryParse(mentorId, out var parsedGuid))
            {
                var mentorProfile = await _db.MentorProfiles.FirstOrDefaultAsync(mp => mp.Id == parsedGuid);
                var mentorUserId = mentorProfile?.UserId ?? parsedGuid;

                return await _db.Chats.FirstOrDefaultAsync(c =>
                    (c.User1Id == senderId && c.User2Id == mentorUserId) ||
                    (c.User1Id == mentorUserId && c.User2Id == senderId));
            }

            var mentorUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == mentorId);
            if (mentorUser != null)
            {
                return await _db.Chats.FirstOrDefaultAsync(c =>
                    (c.User1Id == senderId && c.User2Id == mentorUser.Id) ||
                    (c.User1Id == mentorUser.Id && c.User2Id == senderId));
            }

            return await _db.Chats.FirstOrDefaultAsync(c =>
                c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId));
        }
    }
}
