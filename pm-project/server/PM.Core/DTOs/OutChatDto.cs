namespace PM.Core.DTOs
{
    public class OutChatDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = null!;
            public Guid User1Id { get; set; }
            public Guid User2Id { get; set; }
            public string? ExternalMentorId { get; set; }
        }
}