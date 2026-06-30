using System;
using System.Linq;
using LeaveScheduler.Domain.Entities;

namespace LeaveScheduler.Infrastructure.Persistence;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Seed Public Holidays
        if (!context.PublicHolidays.Any())
        {
            context.PublicHolidays.AddRange(
                new PublicHoliday { Date = new DateTime(2026, 7, 3), Name = "Independence Day (Observed)" },
                new PublicHoliday { Date = new DateTime(2026, 7, 6), Name = "Summer Bank Holiday" },
                new PublicHoliday { Date = new DateTime(2026, 7, 20), Name = "Midsummer Day" },
                new PublicHoliday { Date = new DateTime(2026, 9, 7), Name = "Labor Day" }
            );
            context.SaveChanges();
        }

        // Seed Teams and Employees
        if (!context.Teams.Any())
        {
            var smallTeam = new Team { Name = "Small Team (Size 3)" };
            var mediumTeam = new Team { Name = "Medium Team (Size 6)" };
            var largeTeam = new Team { Name = "Large Team (Size 10)" };

            context.Teams.AddRange(smallTeam, mediumTeam, largeTeam);
            context.SaveChanges(); // Persist teams to get IDs

            // Seed Employees for Small Team (Limit = 1)
            context.Employees.AddRange(
                new Employee { Name = "Alice Smith", TeamId = smallTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Bob Jones", TeamId = smallTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Charlie Brown", TeamId = smallTeam.Id, LeaveBalance = 15 }
            );

            // Seed Employees for Medium Team (Limit = 1)
            context.Employees.AddRange(
                new Employee { Name = "David Miller", TeamId = mediumTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Emma Davis", TeamId = mediumTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Frank Wilson", TeamId = mediumTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Grace Taylor", TeamId = mediumTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Henry Anderson", TeamId = mediumTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Ivy Thomas", TeamId = mediumTeam.Id, LeaveBalance = 30 }
            );

            // Seed Employees for Large Team (Limit = 3)
            context.Employees.AddRange(
                new Employee { Name = "Jack Jackson", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Karen White", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Leo Harris", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Mia Martin", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Nathan Thompson", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Olivia Garcia", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Paul Robinson", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Quinn Clark", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Ryan Rodriguez", TeamId = largeTeam.Id, LeaveBalance = 30 },
                new Employee { Name = "Sophia Lewis", TeamId = largeTeam.Id, LeaveBalance = 30 }
            );

            context.SaveChanges();
        }
    }
}
