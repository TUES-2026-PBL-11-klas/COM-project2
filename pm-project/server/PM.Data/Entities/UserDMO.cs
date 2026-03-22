using System;

namespace PM.Data.Entities
{
    public class UserDMO
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}