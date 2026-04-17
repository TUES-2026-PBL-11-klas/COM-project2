using System;
using System.Collections.Generic;

namespace PM.Data.Entities
{
    public class ConversationDMO
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string? Picture { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public ICollection<UserDMO> Participants { get; set; } = new List<UserDMO>();
    }
}
