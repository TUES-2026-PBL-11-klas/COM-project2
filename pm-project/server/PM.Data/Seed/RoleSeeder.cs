using PM.Data.Context;
using PM.Data.Entities;

namespace PM.Data.Seed
{
    public static class RoleSeeder
    {
        public static void SeedRoles(AppDbContext context)
        {
            var roles = new[] { "Student", "Mentor", "Admin" };

            foreach (var roleName in roles)
            {
                if (!context.Roles.Any(r => r.Name == roleName))
                {
                    context.Roles.Add(new Role { Name = roleName });
                }
            }

            context.SaveChanges();
        }
    }
}