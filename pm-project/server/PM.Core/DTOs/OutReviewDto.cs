namespace PM.Core.DTOs
{
    public class OutReviewDto
    {
        public Guid Id { get; set; }
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = null!;
        public Guid? ReviewedUserId { get; set; }
        public string? ReviewedExternalId { get; set; }
        public string? ReviewedUserName { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
