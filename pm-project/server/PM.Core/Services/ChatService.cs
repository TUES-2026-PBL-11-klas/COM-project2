using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PM.Core.Interfaces;
using PM.Data.Context;
using PM.Data.Entities;
using PM.Data.Repositories;

namespace PM.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IMessageRepository _messageRepository;

        public ChatService(AppDbContext context, IMessageRepository messageRepository)
        {
            _context = context;
            _messageRepository = messageRepository;
        }

        public async Task<Chat> CreateOrGetChatAsync(Guid senderId, string mentorId)
        {
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            if (sender == null)
            {
                throw new InvalidOperationException("Sender not found.");
            }

            var resolvedMentor = await ResolveMentorUserAsync(mentorId);
            if (resolvedMentor != null && resolvedMentor.Id == senderId)
            {
                throw new InvalidOperationException("cannot create chat targeting yourself");
            }

            var existing = resolvedMentor != null
                ? await _context.Chats.FirstOrDefaultAsync(c =>
                    (c.User1Id == senderId && c.User2Id == resolvedMentor.Id) ||
                    (c.User1Id == resolvedMentor.Id && c.User2Id == senderId))
                : await _context.Chats.FirstOrDefaultAsync(c =>
                    c.ExternalMentorId == mentorId && (c.User1Id == senderId || c.User2Id == senderId));

            if (existing != null)
            {
                return existing;
            }

            var chat = new Chat
            {
                User1Id = senderId,
                User2Id = resolvedMentor?.Id,
                ExternalMentorId = await ResolveExternalMentorIdAsync(mentorId, resolvedMentor?.Id),
                Name = resolvedMentor != null ? FormatDisplayName(resolvedMentor.Username) : "Mentor"
            };

            _context.Chats.Add(chat);

            if (resolvedMentor != null)
            {
                var mentorProfile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.UserId == resolvedMentor.Id);
                if (mentorProfile != null)
                {
                    mentorProfile.StudentsHelped += 1;
                }
            }

            await _context.SaveChangesAsync();
            return chat;
        }

        public async Task<IReadOnlyCollection<Chat>> GetChatsForUserAsync(Guid userId)
        {
            return await _context.Chats
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Chat?> GetChatByIdAsync(Guid chatId)
        {
            return await _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
        }

        public async Task<MessageDMO> SendMessageAsync(Guid chatId, Guid senderId, string content)
        {
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
            if (chat == null)
            {
                throw new InvalidOperationException("Chat not found.");
            }

            var message = new MessageDMO
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Attachments = new List<string>()
            };

            await _messageRepository.AddMessageAsync(message);

            chat.LastMessageContent = message.Content;
            chat.LastMessageAt = message.CreatedAt;
            chat.LastMessageSenderId = senderId;

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<IReadOnlyCollection<MessageDMO>> GetChatMessagesAsync(Guid chatId)
        {
            var messages = await _messageRepository.GetMessagesForChatAsync(chatId);
            return messages.OrderBy(m => m.CreatedAt).ToList();
        }

        public async Task<IReadOnlyCollection<Guid>> DeleteMentorChatsAsync(Guid mentorUserId, Guid? mentorProfileId = null)
        {
            var mentorUserIdString = mentorUserId.ToString();
            var mentorProfileIdString = mentorProfileId?.ToString();

            var chats = await _context.Chats
                .Where(c =>
                    c.User1Id == mentorUserId ||
                    c.User2Id == mentorUserId ||
                    (!string.IsNullOrEmpty(c.ExternalMentorId) &&
                     (c.ExternalMentorId == mentorUserIdString ||
                      (mentorProfileIdString != null && c.ExternalMentorId == mentorProfileIdString))))
                .ToListAsync();

            if (chats.Count == 0)
            {
                return Array.Empty<Guid>();
            }

            var deletedIds = chats.Select(c => c.Id).ToList();
            _context.Chats.RemoveRange(chats);
            await _context.SaveChangesAsync();
            return deletedIds;
        }

        private async Task<UserDMO?> ResolveMentorUserAsync(string mentorId)
        {
            if (Guid.TryParse(mentorId, out var parsedGuid))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedGuid);
                if (user != null)
                {
                    return user;
                }

                var mentorProfile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.Id == parsedGuid);
                if (mentorProfile != null)
                {
                    return await _context.Users.FirstOrDefaultAsync(u => u.Id == mentorProfile.UserId);
                }
            }

            var byUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == mentorId);
            if (byUsername != null)
            {
                return byUsername;
            }

            var byProfile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.Id.ToString() == mentorId || mp.UserId.ToString() == mentorId);
            if (byProfile != null)
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == byProfile.UserId);
            }

            return null;
        }

        private async Task<string?> ResolveExternalMentorIdAsync(string mentorId, Guid? resolvedMentorUserId)
        {
            if (Guid.TryParse(mentorId, out var parsedGuid))
            {
                var mentorProfile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.Id == parsedGuid);
                if (mentorProfile != null)
                {
                    return mentorProfile.Id.ToString();
                }

                if (resolvedMentorUserId.HasValue)
                {
                    var profile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.UserId == resolvedMentorUserId.Value);
                    return profile?.Id.ToString();
                }

                return null;
            }

            return mentorId;
        }

        private static string FormatDisplayName(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Mentor";
            }

            var cleaned = System.Text.RegularExpressions.Regex.Replace(username, "[^a-zA-Z0-9]+", " ");
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
