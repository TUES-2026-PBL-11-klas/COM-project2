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
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<MentorProfile> MentorProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDMO>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("USER_ROLES"));

            modelBuilder.Entity<UserDMO>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<UserDMO>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<MentorProfile>()
                .HasIndex(mp => mp.UserId)
                .IsUnique();

            modelBuilder.Entity<MentorProfile>()
                .HasOne(mp => mp.User)
                .WithOne(u => u.MentorProfile)
                .HasForeignKey<MentorProfile>(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
