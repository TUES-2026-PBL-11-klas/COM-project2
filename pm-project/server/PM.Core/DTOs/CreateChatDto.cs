namespace PM.Core.DTOs
{
    public class CreateChatDto
    {
        public string MentorId { get; set; } = null!;
        public Guid? SenderId { get; set; }
    }
}
