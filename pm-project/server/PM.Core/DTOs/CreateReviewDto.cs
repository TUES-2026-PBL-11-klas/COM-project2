namespace PM.Core.DTOs
{
    public class CreateReviewDto
    {
        public string ReviewedId { get; set; } = null!;
        public Guid? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = null!;
    }
}
