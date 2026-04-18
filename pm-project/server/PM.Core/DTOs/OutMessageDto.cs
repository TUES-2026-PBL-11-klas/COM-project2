namespace PM.Core.DTOs
{
    public class OutMessageDto
        {
            public Guid Id { get; set; }
            public string Content { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public Guid SenderId { get; set; }
            public Guid ChatId { get; set; }
        }
}