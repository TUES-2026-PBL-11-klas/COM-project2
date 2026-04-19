namespace PM.Data.Entities
{
    public class Chat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public Guid User1Id { get; set; }
        public UserDMO User1 { get; set; } = null!;
        public Guid User2Id { get; set; }
        public UserDMO User2 { get; set; } = null!;
        public string? ExternalMentorId { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public Guid? LastMessageSenderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
