namespace PM.Data.Entities
{
    public class Review
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = null!;

        public Guid? ReviewedUserId { get; set; }
        public string? ReviewedExternalId { get; set; }

        public int Rating { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
