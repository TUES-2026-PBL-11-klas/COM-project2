using System;

namespace PM.Data.Entities
{
    public class MentorProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public UserDMO User { get; set; } = null!;
        public string Subjects { get; set; } = string.Empty;
        public int StudentsHelped { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
