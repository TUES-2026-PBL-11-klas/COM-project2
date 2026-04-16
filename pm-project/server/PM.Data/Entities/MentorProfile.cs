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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
