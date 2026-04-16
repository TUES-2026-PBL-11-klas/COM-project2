namespace PM.Core.DTOs
{
    public class SendMessageDto
        {
            public Guid SenderId { get; set; }
            public string Content { get; set; } = null!;
        }
}