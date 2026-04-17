using System;
using System.Collections.Generic;

namespace PM.Data.Entities
{
    public class MessageDMO
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // _id in Cassandra/NoSQL
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = null!;
        public List<string> Attachments { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }
}
