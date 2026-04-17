using System;

namespace PM.Data.Entities
{
    public class MentorProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public UserDMO User { get; set; } = null!;
        // Comma-separated list of subjects
        public string Subjects { get; set; } = string.Empty;
        // Number of distinct students who started a chat with this mentor (visible to clients)
        public int StudentsHelped { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
