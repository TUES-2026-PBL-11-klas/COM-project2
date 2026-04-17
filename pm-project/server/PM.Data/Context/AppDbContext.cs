using Microsoft.EntityFrameworkCore;
using PM.Data.Entities;

namespace PM.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserDMO> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ConversationDMO> Conversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDMO>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("USER_ROLES"));

            modelBuilder.Entity<UserDMO>()
                .HasMany(u => u.Conversations)
                .WithMany(c => c.Participants)
                .UsingEntity(j => j.ToTable("CONVERSATION_PARTICIPANTS"));
        }
    }
}