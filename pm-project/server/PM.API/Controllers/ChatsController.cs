using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PM.Core.DTOs;
using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(AppDbContext db, IChatService chatService, ILogger<ChatsController> logger)
        {
            _db = db;
            _chatService = chatService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyChats()
        {
            var user = await ResolveCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var chats = await _chatService.GetChatsForUserAsync(user.Id);
            var summaries = new List<ChatSummaryDto>();

            foreach (var chat in chats)
            {
                if (chat.User1Id == user.Id && (chat.User2Id == Guid.Empty || chat.User2Id == user.Id) &&
                    string.IsNullOrWhiteSpace(chat.ExternalMentorId))
                {
                    continue;
                }

                summaries.Add(new ChatSummaryDto
                {
                    Id = chat.Id,
                    Name = await ResolveChatNameAsync(chat, user.Id),
                    User1Id = chat.User1Id,
                    User2Id = chat.User2Id,
                    ExternalMentorId = chat.ExternalMentorId,
                    LastMessageContent = chat.LastMessageContent,
                    LastMessageAt = chat.LastMessageAt,
                    LastMessageSenderId = chat.LastMessageSenderId
                });
            }

            return Ok(summaries);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatDto dto)
        {
            var senderId = dto.SenderId ?? Guid.Empty;
            if (senderId == Guid.Empty)
            {
                var authUser = await ResolveCurrentUserAsync();
                if (authUser == null)
                {
                    return BadRequest("senderId or auth required");
                }

                senderId = authUser.Id;
            }

            if (string.IsNullOrWhiteSpace(dto.MentorId))
            {
                return BadRequest("mentorId is required");
            }

            try
            {
                var chat = await _chatService.CreateOrGetChatAsync(senderId, dto.MentorId);
                var response = new ChatSummaryDto
                {
                    Id = chat.Id,
                    Name = chat.Name,
                    User1Id = chat.User1Id,
                    User2Id = chat.User2Id,
                    ExternalMentorId = chat.ExternalMentorId,
                    LastMessageContent = chat.LastMessageContent,
                    LastMessageAt = chat.LastMessageAt,
                    LastMessageSenderId = chat.LastMessageSenderId
                };

                return CreatedAtAction(nameof(GetMessagesByChatId), new { chatId = chat.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "CreateChat rejected for sender {SenderId} and mentor {MentorId}", senderId, dto.MentorId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessagesByChatId(Guid chatId)
        {
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
            {
                return Ok(new List<OutMessageDto>());
            }

            var messages = await _chatService.GetChatMessagesAsync(chatId);
            var output = messages.Select(m => new OutMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                SenderId = m.SenderId,
                ChatId = m.ChatId
            }).ToList();

            return Ok(output);
        }

        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> PostMessageToChat(Guid chatId, [FromBody] SendMessageDto dto)
        {
            var senderId = dto.SenderId;
            if (senderId == Guid.Empty)
            {
                var authUser = await ResolveCurrentUserAsync();
                if (authUser == null)
                {
                    return BadRequest("senderId is required either in body or via authenticated token");
                }

                senderId = authUser.Id;
            }

            try
            {
                var message = await _chatService.SendMessageAsync(chatId, senderId, dto.Content);
                return CreatedAtAction(nameof(GetMessagesByChatId), new { chatId }, new OutMessageDto
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
                return NotFound(ex.Message);
            }
        }

        private async Task<UserDMO?> ResolveCurrentUserAsync()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<string> ResolveChatNameAsync(Chat chat, Guid currentUserId)
        {
            if (chat.User1Id != Guid.Empty && chat.User2Id != Guid.Empty)
            {
                var otherId = chat.User1Id == currentUserId ? chat.User2Id : chat.User1Id;
                var otherUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == otherId);
                if (otherUser != null)
                {
                    return FormatDisplayName(otherUser.Username);
                }
            }

            if (!string.IsNullOrWhiteSpace(chat.ExternalMentorId))
            {
                var mentorProfile = await _db.MentorProfiles
                    .Include(mp => mp.User)
                    .FirstOrDefaultAsync(mp =>
                        mp.Id.ToString() == chat.ExternalMentorId ||
                        mp.UserId.ToString() == chat.ExternalMentorId);

                if (mentorProfile?.User != null)
                {
                    return FormatDisplayName(mentorProfile.User.Username);
                }

                var user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.Id.ToString() == chat.ExternalMentorId || u.Username == chat.ExternalMentorId);
                if (user != null)
                {
                    return FormatDisplayName(user.Username);
                }
            }

            return string.IsNullOrWhiteSpace(chat.Name) ? "Mentor" : chat.Name;
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
