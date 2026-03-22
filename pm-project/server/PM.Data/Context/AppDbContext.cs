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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserDMO>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("UserRoles"));
        }
        // oshte tablichki
        // public DbSet<MentorDMO> Mentors { get; set; }
        // public DbSet<ReviewDMO> Reviews { get; set; }
        // public DbSet<SessionDMO> Sessions { get; set; }
    }
}