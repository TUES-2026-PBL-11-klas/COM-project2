using PM.Data.Context;
using PM.Data.Entities;
using System;
using System.Linq;

namespace PM.Data.Seed
{
    public static class MentorSeeder
    {
        public static void SeedTestMentors(AppDbContext context)
        {
            if (context.MentorProfiles.Any()) return;

            void AddMentor(string username, string email, string subjects, int studentsHelped, (string id, string name, int rating, string comment, DateTime date)[] reviews)
            {
                var user = new UserDMO { Username = username, Email = email, PasswordHash = "", CreatedAt = DateTime.UtcNow };
                context.Users.Add(user);

                var prof = new MentorProfile
                {
                    User = user,
                    UserId = user.Id,
                    Subjects = subjects,
                    StudentsHelped = studentsHelped,
                    CreatedAt = DateTime.UtcNow
                };
                context.MentorProfiles.Add(prof);

                foreach (var r in reviews)
                {
                    context.Reviews.Add(new Review
                    {
                        ReviewerId = Guid.NewGuid(),
                        ReviewerName = r.name,
                        ReviewedUserId = user.Id,
                        Rating = r.rating,
                        Content = r.comment,
                        CreatedAt = r.date
                    });
                }
            }

            AddMentor("ivan.petrov", "ivan@example.com", "Math", 245, new[] {
                ("r1","Sophia",5,"Amazing math tutor! Explained complex calculus concepts so clearly.", DateTime.UtcNow.AddDays(-7)),
                ("r2","Liam",5,"Helped me ace my algebra exam. Very patient and thorough.", DateTime.UtcNow.AddDays(-9))
            });

            AddMentor("maria.dimitrova", "maria@example.com", "English", 189, new[] {
                ("r3","Alex",5,"Great English tutor! Improved my writing skills significantly.", DateTime.UtcNow.AddDays(-8)),
                ("r4","Emma",4,"Good explanations, but sometimes moves too fast through topics.", DateTime.UtcNow.AddDays(-12))
            });

            AddMentor("alex.johnson", "alex@example.com", "Physics", 156, new[] {
                ("r5","Nina",5,"Physics finally makes sense! Best tutor I've had.", DateTime.UtcNow.AddDays(-10))
            });

            AddMentor("sofia.rodriguez", "sofia@example.com", "Chemistry", 212, new[] {
                ("r6","David",5,"Chemistry expert! Made organic chemistry fun and understandable.", DateTime.UtcNow.AddDays(-11)),
                ("r7","Sarah",5,"Helped me understand complex chemical reactions. Highly recommend!", DateTime.UtcNow.AddDays(-14))
            });

            AddMentor("michael.chen", "michael@example.com", "Programming", 324, new[] {
                ("r8","James",5,"Excellent programming tutor! Learned Python from scratch.", DateTime.UtcNow.AddDays(-9)),
                ("r9","Mia",4,"Good at explaining code concepts, but could use more examples.", DateTime.UtcNow.AddDays(-13)),
                ("r10","John",5,"Helped me build my first app. Amazing patience and knowledge.", DateTime.UtcNow.AddDays(-16))
            });

            context.SaveChanges();
        }
    }
}
