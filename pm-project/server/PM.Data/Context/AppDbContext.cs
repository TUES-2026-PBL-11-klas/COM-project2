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
        // oshte tablichki
        // public DbSet<MentorDMO> Mentors { get; set; }
        // public DbSet<ReviewDMO> Reviews { get; set; }
        // public DbSet<SessionDMO> Sessions { get; set; }
    }
}