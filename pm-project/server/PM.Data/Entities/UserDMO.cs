using System;
using System.Collections.Generic;

namespace PM.Data.Entities
{
    public class UserDMO
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<ConversationDMO> Conversations { get; set; } = new List<ConversationDMO>();
    }
}