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
        // Optional external mentor identifier (when mentor is not a user in the local DB)
        public string? ExternalMentorId { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}