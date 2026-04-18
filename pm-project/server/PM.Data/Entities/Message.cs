namespace PM.Data.Entities
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public Guid SenderId { get; set; }
        public UserDMO Sender { get; set; } = null!;
        public Guid ChatId { get; set; }
        public Chat Chat { get; set; } = null!;
    }
}