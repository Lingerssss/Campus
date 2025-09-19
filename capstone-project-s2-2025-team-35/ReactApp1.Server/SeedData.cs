using ReactApp1.Server.Data;
using ReactApp1.Server.Model;

namespace ReactApp1.Server
{
    public static class SeedData
    {
        public static void Initialize(EventDbContext context)
        {
            // If there is already data in any table, skip seeding
            if (context.Users.Any() || context.Events.Any() || context.EventRegistrations.Any())
                return;

            // Fixed User Ids
            const int ORG1 = 1;
            const int ORG2 = 2;
            const int ORG3 = 3;  // Gmail organizer
            const int STU1 = 4;
            const int STU2 = 5;
            const int STU3 = 6;

            var users = new[]
            {
                new User { Id = ORG1, GoogleId="google_org_1",  Email="bernies2018@gmail.com",  Username="Bernies Wu", Role=UserRole.Organizer },
                new User { Id = ORG2, GoogleId="google_org_2",  Email="organizer2@aucklanduni.ac.nz", Username="Prof. Mike Chen",   Role=UserRole.Organizer },
                new User { Id = ORG3, GoogleId="google_org_3",  Email="dove@gmail.com", Username="Dove", Role=UserRole.Organizer },
                new User { Id = STU1, GoogleId="google_student_1", Email="john.doe@aucklanduni.ac.nz",  Username="John Doe",   Role=UserRole.Student },
                new User { Id = STU2, GoogleId="google_student_2", Email="jane.smith@aucklanduni.ac.nz", Username="Jane Smith", Role=UserRole.Student },
                new User { Id = STU3, GoogleId="google_student_3", Email="bob.wilson@aucklanduni.ac.nz", Username="Bob Wilson", Role=UserRole.Student },
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // Fixed Event Ids
            const int EVT_REACT    = 1;
            const int EVT_DESIGN   = 2;
            const int EVT_RUN      = 3;
            const int EVT_AI       = 4;
            const int EVT_PHOTO    = 5;
            const int EVT_DS       = 6;

            var events = new[]
            {
                new Event { Id = EVT_REACT,  Title="React Workshop",
                    StartAt=DateTime.Now.AddDays(7),  EndAt=DateTime.Now.AddDays(7).AddHours(3),
                    Location="Lab 2", Description="Learn React fundamentals with hands-on coding",
                    Category="Tech", Capacity=30, OrganizerId=ORG1, Tags=new List<string>{"react","javascript","workshop"} },

                new Event { Id = EVT_DESIGN, Title="Design Thinking Workshop",
                    StartAt=DateTime.Now.AddDays(10), EndAt=DateTime.Now.AddDays(10).AddHours(4),
                    Location="Studio A", Description="Creative problem solving and innovation workshop",
                    Category="Creative", Capacity=20, OrganizerId=ORG1, Tags=new List<string>{"design","creativity","innovation"} },

                new Event { Id = EVT_RUN,    Title="Campus Run",
                    StartAt=DateTime.Now.AddDays(3),  EndAt=DateTime.Now.AddDays(3).AddHours(1),
                    Location="Main Lawn", Description="Weekly running group for all fitness levels",
                    Category="Sports", Capacity=50, OrganizerId=ORG1, Tags=new List<string>{"running","fitness","outdoor"} },

                new Event { Id = EVT_AI,     Title="AI Ethics Seminar",
                    StartAt=DateTime.Now.AddDays(14), EndAt=DateTime.Now.AddDays(14).AddHours(2),
                    Location="Lecture Hall B", Description="Exploring ethical implications of artificial intelligence",
                    Category="Tech", Capacity=100, OrganizerId=ORG2, Tags=new List<string>{"ai","ethics","seminar"} },

                new Event { Id = EVT_PHOTO,  Title="Photography Walk",
                    StartAt=DateTime.Now.AddDays(5),  EndAt=DateTime.Now.AddDays(5).AddHours(3),
                    Location="Campus Gardens", Description="Capture the beauty of campus with fellow photography enthusiasts",
                    Category="Creative", Capacity=15, OrganizerId=ORG2, Tags=new List<string>{"photography","outdoor","creative"} },

                new Event { Id = EVT_DS,     Title="Data Science Bootcamp",
                    StartAt=DateTime.Now.AddDays(21), EndAt=DateTime.Now.AddDays(21).AddHours(6),
                    Location="Computer Lab B", Description="Intensive introduction to data science and machine learning",
                    Category="Tech", Capacity=25, OrganizerId=ORG2, Tags=new List<string>{"data-science","python","ml"} },
            };
            context.Events.AddRange(events);
            context.SaveChanges();

            // Create registrations (students register for different events)
            var registrations = new[]
            {
                // Student 1 (John Doe) registrations
                new EventRegistration { EventId = EVT_REACT, UserId = STU1, RegisteredAt = DateTime.Now.AddDays(-2) },
                new EventRegistration { EventId = EVT_RUN,   UserId = STU1, RegisteredAt = DateTime.Now.AddDays(-1) },
                new EventRegistration { EventId = EVT_PHOTO, UserId = STU1, RegisteredAt = DateTime.Now.AddHours(-3) },

                // Student 2 (Jane Smith) registrations
                new EventRegistration { EventId = EVT_DESIGN, UserId = STU2, RegisteredAt = DateTime.Now.AddDays(-3) },
                new EventRegistration { EventId = EVT_AI,     UserId = STU2, RegisteredAt = DateTime.Now.AddHours(-12) },

                // Student 3 (Bob Wilson) registrations
                new EventRegistration { EventId = EVT_DS,     UserId = STU3, RegisteredAt = DateTime.Now.AddHours(-6) },

                // Extra registrations to make events look popular
                new EventRegistration { EventId = EVT_REACT,  UserId = STU2, RegisteredAt = DateTime.Now.AddDays(-1) },
                new EventRegistration { EventId = EVT_REACT,  UserId = STU3, RegisteredAt = DateTime.Now.AddHours(-8) },
            };
            context.EventRegistrations.AddRange(registrations);
            context.SaveChanges();
        }
    }
}