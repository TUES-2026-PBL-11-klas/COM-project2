using Microsoft.AspNetCore.Identity;
using PM.Data.Context;
using PM.Data.Entities;

namespace PM.Data.Seed
{
    public static class MentorSeeder
    {
        public static void SeedTestMentors(AppDbContext context)
        {
            if (context.MentorProfiles.Any())
            {
                return;
            }

            var hasher = new PasswordHasher<UserDMO>();
            var mentorRole = context.Roles.FirstOrDefault(r => r.Name == "Mentor");
            var studentRole = context.Roles.FirstOrDefault(r => r.Name == "Student");

            void AddMentor(
                string username,
                string email,
                string subjects,
                string experience,
                bool available,
                int studentsHelped,
                params (string name, int rating, string comment, DateTime date)[] reviews)
            {
                var user = new UserDMO
                {
                    Username = username,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = hasher.HashPassword(user, "mentor123");
                if (mentorRole != null)
                {
                    user.Roles.Add(mentorRole);
                }

                context.Users.Add(user);

                var profile = new MentorProfile
                {
                    User = user,
                    UserId = user.Id,
                    Subjects = subjects,
                    Experience = experience,
                    Available = available,
                    StudentsHelped = studentsHelped,
                    CreatedAt = DateTime.UtcNow
                };
                context.MentorProfiles.Add(profile);

                foreach (var review in reviews)
                {
                    context.Reviews.Add(new Review
                    {
                        ReviewerId = Guid.NewGuid(),
                        ReviewerName = review.name,
                        ReviewedUserId = user.Id,
                        Rating = review.rating,
                        Content = review.comment,
                        CreatedAt = review.date
                    });
                }
            }

            AddMentor("ivan.petrov", "ivan@example.com", "Math", "10+ years", true, 245,
                ("Sophia", 5, "Amazing math tutor! Explained complex calculus concepts so clearly.", DateTime.UtcNow.AddDays(-7)),
                ("Liam", 5, "Helped me ace my algebra exam. Very patient and thorough.", DateTime.UtcNow.AddDays(-9)));

            AddMentor("maria.dimitrova", "maria@example.com", "English", "8 years", true, 189,
                ("Alex", 5, "Great English tutor! Improved my writing skills significantly.", DateTime.UtcNow.AddDays(-8)),
                ("Emma", 4, "Good explanations, but sometimes moves too fast through topics.", DateTime.UtcNow.AddDays(-12)));

            AddMentor("alex.johnson", "alex@example.com", "Physics", "7 years", false, 156,
                ("Nina", 5, "Physics finally makes sense! Best tutor I've had.", DateTime.UtcNow.AddDays(-10)));

            AddMentor("sofia.rodriguez", "sofia@example.com", "Chemistry", "9 years", true, 212,
                ("David", 5, "Chemistry expert! Made organic chemistry fun and understandable.", DateTime.UtcNow.AddDays(-11)),
                ("Sarah", 5, "Helped me understand complex chemical reactions. Highly recommend!", DateTime.UtcNow.AddDays(-14)));

            AddMentor("michael.chen", "michael@example.com", "Programming", "6 years", true, 324,
                ("James", 5, "Excellent programming tutor! Learned Python from scratch.", DateTime.UtcNow.AddDays(-9)),
                ("Mia", 4, "Good at explaining code concepts, but could use more examples.", DateTime.UtcNow.AddDays(-13)),
                ("John", 5, "Helped me build my first app. Amazing patience and knowledge.", DateTime.UtcNow.AddDays(-16)));

            AddMentor("emma.watson", "emma@example.com", "History", "5 years", false, 98,
                ("Oliver", 4, "Good history knowledge, but sessions could be more engaging.", DateTime.UtcNow.AddDays(-17)));

            AddMentor("david.kumar", "david@example.com", "Math", "12 years", true, 278,
                ("Isabella", 5, "Outstanding math tutor! Made trigonometry easy to understand.", DateTime.UtcNow.AddDays(-13)),
                ("Lucas", 5, "Helped me improve my math grades from C to A+. Very dedicated.", DateTime.UtcNow.AddDays(-16)));

            AddMentor("lisa.anderson", "lisa@example.com", "Biology", "8 years", true, 167,
                ("Ethan", 5, "Biology finally clicked! Great explanations of complex topics.", DateTime.UtcNow.AddDays(-15)));

            AddMentor("james.wilson", "james@example.com", "English", "11 years", false, 301,
                ("Ava", 5, "Exceptional English tutor! Improved my essay writing dramatically.", DateTime.UtcNow.AddDays(-12)),
                ("Noah", 5, "Helped me prepare for English literature exam. Very knowledgeable.", DateTime.UtcNow.AddDays(-17)));

            AddMentor("nina.patel", "nina@example.com", "Chemistry", "6 years", true, 134,
                ("Mason", 4, "Solid chemistry tutoring. Good at explaining lab concepts.", DateTime.UtcNow.AddDays(-18)));

            if (!context.Users.Any(u => u.Username == "demo"))
            {
                var demoUser = new UserDMO
                {
                    Username = "demo",
                    Email = "demo@example.com",
                    CreatedAt = DateTime.UtcNow
                };
                demoUser.PasswordHash = hasher.HashPassword(demoUser, "demo123");
                if (studentRole != null)
                {
                    demoUser.Roles.Add(studentRole);
                }

                context.Users.Add(demoUser);
            }

            context.SaveChanges();
        }
    }
}
