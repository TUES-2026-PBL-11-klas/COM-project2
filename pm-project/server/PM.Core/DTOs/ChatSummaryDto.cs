namespace PM.Core.DTOs
{
    public class ChatSummaryDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            public Guid User1Id { get; set; }
            public Guid User2Id { get; set; }
            public string? ExternalMentorId { get; set; }
            public Guid? LastMessageSenderId { get; set; }
            public string? LastMessageContent { get; set; }
            public DateTime? LastMessageAt { get; set; }
        }
}